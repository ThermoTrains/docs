using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SebastianHaeni.ThermoBox.IRCompressor;

namespace Test.Delivery.IRCompressor
{
    [TestClass]
    public class IRSensorDataCompressionTest
    {
        [TestMethod]
        public void TestCompressBook()
        {
            const string sourceFile = @"Resources\book.seq";
            const string compressedVideo = @"book.mp4";

            IRSensorDataCompression.Compress(sourceFile, compressedVideo);
            Assert.IsTrue(File.Exists(compressedVideo), "file is created");
            Assert.IsTrue(new FileInfo(compressedVideo).Length > 0, "file size is greater than 0");
        }


        [TestMethod]
        public void TestCompressFastTrain()
        {
            const string sourceFile = @"Resources\fast-train.seq";
            const string compressedVideo = @"fast-train.mp4";

            IRSensorDataCompression.Compress(sourceFile, compressedVideo);
            Assert.IsTrue(File.Exists(compressedVideo), "file is created");
            Assert.IsTrue(new FileInfo(compressedVideo).Length > 0, "file size is greater than 0");
        }
    }
}
