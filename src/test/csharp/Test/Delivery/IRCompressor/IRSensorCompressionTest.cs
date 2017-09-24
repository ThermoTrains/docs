using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SebastianHaeni.ThermoBox.IRCompressor;

namespace SebastianHaeni.ThermoBox.Delivery.IRCompression
{
    [TestClass]
    public class IRSensorCompressionTest
    {
        [TestMethod]
        public void TestCompress()
        {
            IRSensorDataCompression.Compress(
                @"..\..\..\..\..\..\samples\thermal\book.seq",
                $@"..\..\..\..\..\..\samples\thermal\book.mp4");
        }
    }
}
