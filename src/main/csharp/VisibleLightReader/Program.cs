using System;
using System.Collections.Generic;
using System.Reflection;
using Basler.Pylon;
using Emgu.CV;
using Emgu.CV.Structure;
using log4net;
using SebastianHaeni.ThermoBox.Common.Util;

namespace SebastianHaeni.ThermoBox.VisibleLightReader
{
    internal static class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static void OnImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {
            // The grab result is automatically disposed when the event call back returns.
            // The grab result can be cloned using IGrabResult.Clone if you want to keep a copy of it (not shown in this sample).
            var grabResult = e.GrabResult;
            // Image grabbed successfully?
            if (grabResult.GrabSucceeded)
            {
                // Access the image data.
                Log.Info($"SizeX: {grabResult.Width}");
                Log.Info($"SizeY: {grabResult.Height}");
                byte[] buffer = grabResult.PixelData as byte[];
                var image = new Image<Rgb, byte>(grabResult.Width, grabResult.Height) {Bytes = buffer};
                DebugUtil.PreviewImages(new[] {image});
            }
            else
            {
                Log.Error($"Error: {grabResult.ErrorCode} {grabResult.ErrorDescription}");
            }
        }

        private static void Main()
        {
            var filter =
                new Dictionary<string, string> {[CameraInfoKey.FriendlyName] = "Basler acA1920-25uc (22450918)"};
            using (var camera = new Camera(filter, CameraSelectionStrategy.Unambiguous))
            {
                camera.CameraOpened += Configuration.AcquireContinuous;
                camera.Open();

                // Set a handler for processing the images.
                camera.StreamGrabber.ImageGrabbed += OnImageGrabbed;

                camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);

                // Wait for user input to trigger the camera or exit the loop.
                // Software triggering is used to trigger the camera device.
                char key;
                do
                {
                    Console.WriteLine("Press 't' to trigger the camera or 'e' to exit.");

                    key = Console.ReadKey(true).KeyChar;
                    if ((key == 't' || key == 'T'))
                    {
                        // Execute the software trigger. Wait up to 100 ms until the camera is ready for trigger.
                        if (camera.WaitForFrameTriggerReady(100, TimeoutHandling.ThrowException))
                        {
                            camera.ExecuteSoftwareTrigger();
                        }
                    }
                }
                while (key != 'e' && key != 'E');

                // Stop grabbing.
                camera.StreamGrabber.Stop();
            }
        }
    }
}
