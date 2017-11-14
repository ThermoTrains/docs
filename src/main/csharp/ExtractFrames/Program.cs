using System;
using System.Reflection;
using Emgu.CV;
using Emgu.CV.CvEnum;
using log4net;

namespace ExtractFrames
{
    internal static class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static void Main(string[] args)
        {
            foreach (var filename in args)
            {
                ExtractFrames(filename);
            }
        }

        private static void ExtractFrames(string filename)
        {
            using (var capture = new VideoCapture(filename))
            {
                var frameCount = capture.GetCaptureProperty(CapProp.FrameCount);

                Log.Info($"Extracting {frameCount} frames from {filename}");

                var m = new Mat();

                for (var i = 0; i < frameCount; i++)
                {
                    capture.Read(m);

                    if (i % 20 == 0)
                    {
                        Log.Info($"{i} frames");
                    }

                    if (m.Bitmap == null)
                    {
                        Console.WriteLine($"Could not read frame {i}");
                        continue;
                    }

                    m.Save($@"{filename}-{i}.jpg");
                }
            }
        }
    }
}
