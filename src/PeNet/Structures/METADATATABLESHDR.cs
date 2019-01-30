﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PeNet.Structures.MetaDataTables;
using PeNet.Test.Structures;
using PeNet.Utilities;

namespace PeNet.Structures
{
    public interface IMETADATATABLESHDR
    {
        /// <summary>
        /// The size the indexes into the streams have. 
        /// Bit 0 (0x01) set: Indexes into #String are 4 bytes wide.
        /// Bit 1 (0x02) set: Indexes into #GUID heap are 4 bytes wide.
        /// Bit 2 (0x04) set: Indexes into #Blob heap are 4 bytes wide.
        /// If bit not set: indexes into heap is 2 bytes wide.
        /// </summary>
        byte HeapSizes { get; set; }

        /// <summary>
        /// Access a list of defined tables in the Meta Data Tables Header
        /// with the name and number of rows of the table.
        /// </summary>
        List<METADATATABLESHDR.MetaDataTableInfo> TableDefinitions { get; }
    }

    /// <summary>
    /// The Meta Data Tables Header contains information about all present
    /// data tables in the .Net assembly.
    /// </summary>
    public class METADATATABLESHDR : AbstractStructure, IMETADATATABLESHDR
    {
        private List<MetaDataTableInfo> _tableDefinitions;

        /// <summary>
        /// Represents an table definition entry from the list
        /// of available tables in the Meta Data Tables Header 
        /// in the .Net header of an assembly.
        /// </summary>
        public struct MetaDataTableInfo
        {
            /// <summary>
            /// Number of rows of the table.
            /// </summary>
            public uint RowCount { get; set; }

            /// <summary>
            /// Name of the table.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Offset where the table starts.
            /// </summary>
            public uint Offset { get; set; }

            /// <summary>
            /// Byter per row in the table.
            /// </summary>
            public uint BytesPerRow { get; set;}

            /// <summary>
            ///     Create a string representation of the objects
            ///     properties.
            /// </summary>
            /// <returns>The TableDefinition properties as a string.</returns>
            public override string ToString()
            {
                var sb = new StringBuilder("Table Definition\n");
                sb.Append(this.PropertiesToString("{0,-10}:\t{1,10:X}\n"));

                return sb.ToString();
            }
        }

        /// <summary>
        /// Create a new Meta Data Tables Header instance from a byte array.
        /// </summary>
        /// <param name="buff">Buffer which contains a METADATATABLESHDR structure.</param>
        /// <param name="offset">Offset in the buffer, where the header starts.</param>
        public METADATATABLESHDR(byte[] buff, uint offset) 
            : base(buff, offset)
        {
        }

        /// <summary>
        /// Reserved1, always 0.
        /// </summary>
        public uint Reserved1
        {
            get { return Buff.BytesToUInt32(Offset); }
            set { Buff.SetUInt32(Offset, value); }
        }

        /// <summary>
        /// Major Version.
        /// </summary>
        public byte MajorVersion
        {
            get { return Buff[Offset + 0x4]; }
            set { Buff[Offset + 0x4] = value; }
        }

        /// <summary>
        /// Minor Version.
        /// </summary>
        public byte MinorVersion
        {
            get { return Buff[Offset + 0x5]; }
            set { Buff[Offset + 0x5] = value; }
        }

        /// <summary>
        /// The size the indexes into the streams have. 
        /// Bit 0 (0x01) set: Indexes into #String are 4 bytes wide.
        /// Bit 1 (0x02) set: Indexes into #GUID heap are 4 bytes wide.
        /// Bit 2 (0x04) set: Indexes into #Blob heap are 4 bytes wide.
        /// If bit not set: indexes into heap is 2 bytes wide.
        /// </summary>
        public byte HeapSizes
        {
            get { return Buff[Offset + 0x6]; }
            set { Buff[Offset + 0x6] = value; }
        }

        /// <summary>
        /// Reserved2, always 1.
        /// </summary>
        public byte Reserved2
        {
            get { return Buff[Offset + 0x7]; }
            set { Buff[Offset + 0x7] = value; }
        }

        /// <summary>
        /// Bit mask which shows, which tables are present in the .Net assembly. 
        /// Maximal 64 tables can be present, but most tables are not defined such that
        /// the high bits of the mask are always 0.
        /// </summary>
        public ulong Valid
        {
            get { return Buff.BytesToUInt64(Offset + 0x8); }
            set { Buff.SetUInt64(Offset + 0x8, value); }
        }

        /// <summary>
        /// Bit mask which shows, which tables are sorted. 
        /// </summary>
        public ulong MaskSorted
        {
            get { return Buff.BytesToUInt64(Offset + 0x10); }
            set { Buff.SetUInt64(Offset + 0x10, value); }
        }

        /// <summary>
        /// Access a list of defined tables in the Meta Data Tables Header
        /// with the name and number of rows of the table.
        /// </summary>
        public List<MetaDataTableInfo> TableDefinitions
        {
            get
            {
                if (_tableDefinitions != null)
                    return _tableDefinitions;

                _tableDefinitions = ParseTableDefinitions();
                return _tableDefinitions;
            }
        }

        private List<MetaDataTableInfo> ParseTableDefinitions()
        {
            var heapSizes = new HeapSizes(HeapSizes);


            var tables = new MetaDataTableInfo[64];

            var startOfTableDefinitions = Offset + 24;
            var names = FlagResolver.ResolveMaskValidFlags(Valid);

            // Set number of rows per table
            var cnt = 0;
            for (var i = 0; i < tables.Length; ++i)
            {
                if ((Valid & (1UL << i)) != 0)
                {
                    tables[i].RowCount = Buff.BytesToUInt32(startOfTableDefinitions + (uint)cnt * 4);
                    tables[i].Name = names[cnt];
                    cnt++;
                }
            }

            var indexSizes = new IndexSize(tables);

            // Set row size of tables

            tables[(int)MetadataToken.Module].BytesPerRow = 2 + heapSizes.String + heapSizes.Guid * 3;
            tables[(int)MetadataToken.TypeReference].BytesPerRow = indexSizes[Index.ResolutionScope] + heapSizes.String * 2;
            tables[(int)MetadataToken.TypeDef].BytesPerRow = 4 + heapSizes.String * 2 + indexSizes[Index.TypeDefOrRef] + indexSizes[Index.Field] + indexSizes[Index.MethodDef];
            tables[(int)MetadataToken.Field].BytesPerRow = 2 + heapSizes.String + heapSizes.Blob;
            tables[(int)MetadataToken.MethodDef].BytesPerRow = 8 + heapSizes.String + heapSizes.Blob + GetIndexSize(MetadataToken.Parameter, tables);
            tables[(int)MetadataToken.Parameter].BytesPerRow = 4 + heapSizes.String;
            tables[(int)MetadataToken.InterfaceImplementation].BytesPerRow = GetIndexSize(MetadataToken.TypeDef, tables) + indexSizes[Index.TypeDefOrRef];
            tables[(int)MetadataToken.MemberReference].BytesPerRow = indexSizes[Index.MemberRefParent] + heapSizes.String + heapSizes.Blob;
            tables[(int)MetadataToken.Constant].BytesPerRow = 2 + indexSizes[Index.HasConstant] + heapSizes.Blob;
            tables[(int)MetadataToken.CustomAttribute].BytesPerRow = indexSizes[Index.HasCustomAttribute] + indexSizes[Index.CustomAttributeType] + heapSizes.Blob;
            tables[(int)MetadataToken.FieldMarshal].BytesPerRow = indexSizes[Index.HasFieldMarshal] + heapSizes.Blob;
            tables[(int)MetadataToken.DeclarativeSecurity].BytesPerRow = 2 + indexSizes[Index.HasDeclSecurity] + heapSizes.Blob;
            tables[(int)MetadataToken.ClassLayout].BytesPerRow = 6 + GetIndexSize(MetadataToken.TypeDef, tables);
            tables[(int)MetadataToken.FieldLayout].BytesPerRow = 4 + GetIndexSize(MetadataToken.Field, tables);
            tables[(int)MetadataToken.Signature].BytesPerRow = heapSizes.Blob;
            tables[(int)MetadataToken.EventMap].BytesPerRow = GetIndexSize(MetadataToken.TypeDef, tables) + GetIndexSize(MetadataToken.Event, tables);
            tables[(int)MetadataToken.Event].BytesPerRow = 2 + heapSizes.String + indexSizes[Index.TypeDefOrRef];
            tables[(int)MetadataToken.PropertyMap].BytesPerRow = GetIndexSize(MetadataToken.TypeDef, tables) + GetIndexSize(MetadataToken.Property, tables);
            tables[(int)MetadataToken.Property].BytesPerRow = 2 + heapSizes.String + heapSizes.Blob;
            tables[(int)MetadataToken.MethodSemantics].BytesPerRow = 2 + GetIndexSize(MetadataToken.MethodDef, tables) + indexSizes[Index.HasSemantics];
            tables[(int)MetadataToken.MethodImplementation].BytesPerRow = GetIndexSize(MetadataToken.TypeDef, tables) + indexSizes[Index.MethodDefOrRef] * 2;
            tables[(int)MetadataToken.ModuleReference].BytesPerRow = heapSizes.String;
            tables[(int)MetadataToken.TypeSpecification].BytesPerRow = heapSizes.Blob;
            tables[(int)MetadataToken.ImplementationMap].BytesPerRow = 2 + indexSizes[Index.MemberForwarded] + heapSizes.String + GetIndexSize(MetadataToken.ModuleReference, tables);
            tables[(int)MetadataToken.FieldRva].BytesPerRow = 4 + GetIndexSize(MetadataToken.Field, tables);
            tables[(int)MetadataToken.Assembly].BytesPerRow = 16 + heapSizes.Blob + heapSizes.String * 2;
            tables[(int)MetadataToken.AssemblyProcessor].BytesPerRow = 4;
            tables[(int)MetadataToken.AssemblyOS].BytesPerRow = 12;
            tables[(int)MetadataToken.AssemblyReference].BytesPerRow = 12 + heapSizes.Blob * 2 + heapSizes.String * 2;
            tables[(int)MetadataToken.AssemblyReferenceProcessor].BytesPerRow = 4 + GetIndexSize(MetadataToken.AssemblyReference, tables);
            tables[(int)MetadataToken.AssemblyReferenceOS].BytesPerRow = 12 + GetIndexSize(MetadataToken.AssemblyReference, tables);
            tables[(int)MetadataToken.File].BytesPerRow = 4 + heapSizes.String + heapSizes.Blob;
            tables[(int)MetadataToken.ExportedType].BytesPerRow = 8 + heapSizes.String * 2 + indexSizes[Index.Implementation];
            tables[(int)MetadataToken.ManifestResource].BytesPerRow = 8 + heapSizes.String + indexSizes[Index.Implementation];
            tables[(int)MetadataToken.NestedClass].BytesPerRow = GetIndexSize(MetadataToken.NestedClass, tables) * 2;
            tables[(int)MetadataToken.GenericParameter].BytesPerRow = 4 + indexSizes[Index.TypeOrMethodDef] + heapSizes.String;
            tables[(int)MetadataToken.MethodSpecification].BytesPerRow = indexSizes[Index.MethodDefOrRef] + heapSizes.Blob;
            tables[(int)MetadataToken.GenericParameterConstraint].BytesPerRow = GetIndexSize(MetadataToken.GenericParameter, tables) + indexSizes[Index.TypeDefOrRef];


            // Set offset of tables
            uint offset = 0;
            for (int i = 0; i < tables.Length; ++i)
            {
                tables[i].Offset = offset;
                offset += tables[i].BytesPerRow * tables[i].RowCount;
            }

            return tables.ToList();
        }

        private Tables ParseMetaDataTables()
        {
            var heapSizes = new HeapSizes(HeapSizes);
            var indexSizes = new IndexSize(TableDefinitions.ToArray());

            var tables = new Tables();
            uint tablesOffset = (uint)(Offset + 0x18u + HammingWeight(Valid) * 4u);

            tables.Module = ParseTable<Module>(MetadataToken.Module, tablesOffset, heapSizes, indexSizes);
            tables.TypeRef = ParseTable<TypeRef>(MetadataToken.TypeReference, tablesOffset, heapSizes, indexSizes);
            tables.TypeDef = ParseTable<TypeDef>(MetadataToken.TypeDef, tablesOffset, heapSizes, indexSizes);
            tables.Field = ParseTable<Field>(MetadataToken.Field, tablesOffset, heapSizes, indexSizes);
            tables.MethodDef = ParseTable<MethodDef>(MetadataToken.MethodDef, tablesOffset, heapSizes, indexSizes);
            tables.Param = ParseTable<Param>(MetadataToken.Parameter, tablesOffset, heapSizes, indexSizes);
            tables.InterfaceImpl = ParseTable<InterfaceImpl>(MetadataToken.InterfaceImplementation, tablesOffset, heapSizes, indexSizes);
            tables.MemberRef = ParseTable<MemberRef>(MetadataToken.MemberReference, tablesOffset, heapSizes, indexSizes);
            tables.Constant = ParseTable<Constant>(MetadataToken.Constant, tablesOffset, heapSizes, indexSizes);
            tables.CustomAttribute = ParseTable<CustomAttribute>(MetadataToken.CustomAttribute, tablesOffset, heapSizes, indexSizes);
            tables.FieldMarshal = ParseTable<FieldMarshal>(MetadataToken.FieldMarshal, tablesOffset, heapSizes, indexSizes);

            return tables;
        }

        private List<T> ParseTable<T>(MetadataToken token, uint tablesOffset, HeapSizes heapSizes, IndexSize indexSizes)
            where T : AbstractTable
        {
            var tableInfo = TableDefinitions[(int)token];
            var rows = new List<T>();

            if(tableInfo.RowCount != 0)
            {
                for(var i = 0u; i < tableInfo.RowCount; i++)
                {
                    rows.Add((T) Activator.CreateInstance(typeof(T), new object[] {Buff, tablesOffset + tableInfo.Offset + tableInfo.BytesPerRow * i, heapSizes, indexSizes}));
                }
            }

            return rows.Count == 0 ? null : rows;
        }

        private int HammingWeight(ulong value)
        {
            var count = 0;
            while (value != 0)
            {
                ++count;
                value &= (value - 1);
            }
            return count;
        }


        private Tables _tables = null;
        public Tables Tables 
        {
            get
            {
                if(_tables is null)
                {
                    
                    _tables = ParseMetaDataTables();
                }

                return _tables;
            }
        }

        private uint GetIndexSize(MetadataToken table, MetaDataTableInfo[] tables)
		{
			return tables[(int)table].RowCount <= ushort.MaxValue ? 2U : 4U;
		}

        /// <summary>
        ///     Create a string representation of the objects
        ///     properties.
        /// </summary>
        /// <returns>The METADATATABLESHDR properties as a string.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("METADATATABLESHDR\n");
            sb.Append(this.PropertiesToString("{0,-10}:\t{1,10:X}\n"));

            return sb.ToString();
        }
    }
}