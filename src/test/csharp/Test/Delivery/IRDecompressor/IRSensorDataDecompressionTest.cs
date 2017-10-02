using Microsoft.VisualStudio.TestTools.UnitTesting;
using SebastianHaeni.ThermoBox.IRDecompressor;

namespace Test.Delivery.IRDecompressor
{
    [TestClass]
    internal class IRSensorDataDecompressionTest
    {
        [TestMethod]
        public void TestDecompress()
        {
            // TODO finish this test
            const string compressedVideo = @"Resources\book.mp4";
            const string snapshotFile = @"Resources\book-snapshot.jpg";
            const string decompressedVideo = @"book-decompressed.seq";

            IRSensorDataDecompression.Decompress(compressedVideo, snapshotFile, decompressedVideo);
            //Assert.IsTrue(File.Exists(DecompressedVideo));
        }
    }
}
