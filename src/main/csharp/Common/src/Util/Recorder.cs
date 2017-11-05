using System;
using System.Drawing;
using Emgu.CV;

namespace SebastianHaeni.ThermoBox.Common.Util
{
    public class Recorder : IDisposable
    {
        private static readonly int Compression = VideoWriter.Fourcc('H', '2', '6', '4');
        private readonly int _fps;
        private readonly Size _size;
        private VideoWriter _videoWriter;
        private bool _isColor;
        public int FrameCounter { get; private set; }

        public Recorder(int fps, Size size, bool isColor)
        {
            _fps = fps;
            _size = size;
            _isColor = isColor;
        }

        public Recorder StartRecording(string filepath)
        {
            if (_videoWriter != null)
            {
                StopRecording();
            }

            FrameCounter = 0;
            _videoWriter = new VideoWriter(filepath, Compression, _fps, _size, _isColor);

            return this;
        }

        public Recorder Write<TColor>(Image<TColor, byte> image)
            where TColor : struct, IColor
        {
            if (_videoWriter == null)
            {
                return this;
            }

            FrameCounter++;
            _videoWriter.Write(image.Mat);

            return this;
        }

        public void StopRecording()
        {
            _videoWriter?.Dispose();
            _videoWriter = null;
        }

        public void Dispose()
        {
            _videoWriter?.Dispose();
        }
    }
}
