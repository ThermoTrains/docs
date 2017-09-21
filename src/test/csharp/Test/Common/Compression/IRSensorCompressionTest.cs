using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SebastianHaeni.ThermoBox.Common.Compression;

namespace SebastianHaeni.ThermoBox.IRReader.Compression
{
    [TestClass]
    public class IRSensorCompressionTest
    {
        [TestMethod]
        public void TestCompress()
        {
            IRSensorDataCompression.Compress();
        }
    }
}
