using System;
using System.Collections.Generic;
using System.Reflection;
using Basler.Pylon;
using Emgu.CV;
using Emgu.CV.Structure;
using log4net;
using SebastianHaeni.ThermoBox.Common.Motion;

namespace SebastianHaeni.ThermoBox.VisibleLightReader
{
    internal static class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const int ANALYZE_SEQUENCE_IMAGES = 4;

        private static void Main()
        {
            var filter =
                new Dictionary<string, string> { [CameraInfoKey.FriendlyName] = "Basler acA1920-25uc (22450918)" };
            using (var camera = new Camera(filter, CameraSelectionStrategy.Unambiguous))
            {
                // Set the acquisition mode to free running continuous acquisition when the camera is opened.
                camera.CameraOpened += Configuration.AcquireContinuous;

                // Open the connection to the camera device.
                camera.Open();

                camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByUser);

                var detector = new EntryDetector();
                var state = DetectorState.Nothing;
                var images = new Image<Gray, byte>[ANALYZE_SEQUENCE_IMAGES];
                Image<Gray, byte> background = null;
                var i = 0;

                // Grab images.
                while (true)
                {
                    // Wait for an image and then retrieve it. A timeout of 5000 ms is used.
                    IGrabResult grabResult = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
                    using (grabResult)
                    {
                        // Image grabbed successfully?
                        if (grabResult.GrabSucceeded)
                        {
                            // Access the image data.
                            byte[] buffer = grabResult.PixelData as byte[];
                            var image = new Image<Gray, byte>(grabResult.Width, grabResult.Height)
                            {
                                Bytes = buffer
                            };

                            if (background == null)
                            {
                                background = image;
                            }

                            images[i] = image;
                            i++;

                            if (i == images.Length)
                            {
                                i = 0;
                                var newState = detector.Detect(background, images, state);

                                if (state != newState && newState != DetectorState.Nothing)
                                {
                                    Log.Info($"Detected {newState}");
                                }
                                state = newState;

                                // dispose of references to improve memory consumption
                                for (var k = 0; k < images.Length; k++)
                                {
                                    images[k] = null;
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Error: {0} {1}", grabResult.ErrorCode, grabResult.ErrorDescription);
                            break;
                        }
                    }
                }

                // Stop grabbing.
                camera.StreamGrabber.Stop();
            }
        }
    }
}
