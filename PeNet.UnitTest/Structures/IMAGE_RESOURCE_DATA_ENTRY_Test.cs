﻿/***********************************************************************
Copyright 2016 Stefan Hausotte

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

*************************************************************************/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PeNet.Structures;

namespace PeNet.UnitTest.Structures
{
    [TestClass]
    public class IMAGE_RESOURCE_DATA_ENTRY_Test
    {
        private readonly byte[] _rawResourceDataEntry =
        {
            0xff, // Junk
            0xff,

            0x00, // OffsetToData
            0x11,
            0x22,
            0x33,

            0x44, // Size1
            0x55,
            0x66,
            0x77,

            0x88, // CodePage
            0x99,
            0xaa,
            0xbb,

            0xcc, // Reserved
            0xdd,
            0xee,
            0xff
        };

        [TestMethod]
        public void ImageResourceDataEntryConstructorWorks_Test()
        {
            var resourceDataEntry = new IMAGE_RESOURCE_DATA_ENTRY(_rawResourceDataEntry, 2);

            Assert.AreEqual((uint) 0x33221100, resourceDataEntry.OffsetToData);
            Assert.AreEqual((uint) 0x77665544, resourceDataEntry.Size1);
            Assert.AreEqual((uint) 0xbbaa9988, resourceDataEntry.CodePage);
            Assert.AreEqual((uint) 0xffeeddcc, resourceDataEntry.Reserved);
        }
    }
}