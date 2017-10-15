using System.Collections.Generic;
using System.Reflection;
using Basler.Pylon;
using log4net;

namespace SebastianHaeni.ThermoBox.VisibleLightReader
{
    internal static class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static void Main()
        {
            var filter = new Dictionary<string, string> {[CameraInfoKey.FriendlyName] = "Basler acA1920-25uc"};
            using (var camera = new Camera(filter, CameraSelectionStrategy.Unambiguous))
            {
                camera.CameraOpened += Configuration.AcquireContinuous;
                camera.Open();
                camera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);

                // Grab a number of images.
                for (var i = 0; i < 10; ++i)
                {
                    // Wait for an image and then retrieve it. A timeout of 5000 ms is used.
                    var grabResult = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
                    using (grabResult)
                    {
                        // Image grabbed successfully?
                        if (grabResult.GrabSucceeded)
                        {
                            // Access the image data.
                            Log.Info($"SizeX: {grabResult.Width}");
                            Log.Info($"SizeY: {grabResult.Height}");
                            if (grabResult.PixelData is byte[] buffer)
                            {
                                Log.Info($"Gray value of first pixel: {buffer[0]}");
                            }
                        }
                        else
                        {
                            Log.Error($"{grabResult.ErrorCode} {grabResult.ErrorDescription}");
                        }
                    }
                }

                // Stop grabbing.
                camera.StreamGrabber.Stop();
            }
        }
    }
}
