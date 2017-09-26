using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SebastianHaeni.ThermoBox.IRCompressor;
using SebastianHaeni.ThermoBox.IRDecompressor;
using System.IO;

namespace SebastianHaeni.ThermoBox.Delivery.IRCompression
{
    [TestClass]
    public class IRSensorCompressionTest
    {
        [TestMethod]
        public void TestCompress()
        {
            const string SourceFile = @"..\..\..\..\..\..\..\samples\thermal\book.seq";
            const string SnapshotFile = @"..\..\..\..\..\..\..\samples\thermal\book-snapshot.jpg";
            const string CompressedVideo = @"..\..\..\..\..\..\..\samples\thermal\book.mp4";
            const string DecompressedVideo = @"..\..\..\..\..\..\..\samples\thermal\book-decompressed.seq";

            IRSensorDataCompression.Compress(SourceFile, CompressedVideo);
            Assert.IsTrue(File.Exists(CompressedVideo));

            IRSensorDataDecompression.Decompress(CompressedVideo, SnapshotFile, DecompressedVideo);
            Assert.IsTrue(File.Exists(DecompressedVideo));
        }
    }
}
