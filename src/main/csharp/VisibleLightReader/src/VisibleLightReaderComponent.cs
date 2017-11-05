using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using Basler.Pylon;
using Emgu.CV;
using Emgu.CV.Structure;
using log4net;
using SebastianHaeni.ThermoBox.Common.Component;
using SebastianHaeni.ThermoBox.Common.Motion;
using SebastianHaeni.ThermoBox.Common.Util;
using Configuration = Basler.Pylon.Configuration;
using System.IO;

namespace SebastianHaeni.ThermoBox.VisibleLightReader
{
    internal class VisibleLightReaderComponent : ThermoBoxComponent, IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly string CameraName = ConfigurationManager.AppSettings["VISIBLE_LIGHT_CAMERA_NAME"];

        private static readonly Dictionary<string, string> CameraFilter =
            new Dictionary<string, string> {[CameraInfoKey.FriendlyName] = CameraName};

        private static readonly string CaptureFolder = ConfigurationManager.AppSettings["CAPTURE_FOLDER"];

        private readonly Camera _camera;
        private readonly Recorder _recorder;
        private readonly Size _size;

        private readonly PixelDataConverter _converter = new PixelDataConverter()
        {
            OutputPixelFormat = PixelType.RGB8packed
        };

        private const int AnalyzeSequenceImages = 4;

        public VisibleLightReaderComponent()
        {
            // Setup camera
            _camera = new Camera(CameraFilter, CameraSelectionStrategy.Unambiguous);

            // Set the acquisition mode to free running continuous acquisition when the camera is opened.
            _camera.CameraOpened += Configuration.AcquireContinuous;

            // Open the connection to the camera device.
            _camera.Open();

            // Read device parameters
            var fps = (int) _camera.Parameters[PLCamera.AcquisitionFrameRate].GetValue();
            var width = (int) _camera.Parameters[PLCamera.Width].GetValue();
            var height = (int) _camera.Parameters[PLCamera.Height].GetValue();
            _size = new Size(width, height);

            // Setup recorder
            _recorder = new Recorder(fps, _size, true);

            // Start detecting asynchronously
            new Task(DetectIncomingTrains).Start();
        }

        private void DetectIncomingTrains()
        {
            _camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByUser);

            // Detection class
            var detector = new EntryDetector();

            // Current state of detection (entering, exiting, nothing)
            var state = DetectorState.Nothing;

            // Array to contain images that will be collected until it's full and we analyze them.
            var images = new Image<Gray, byte>[AnalyzeSequenceImages];

            // The background image used to find motion
            Image<Gray, byte> background = null;

            // Analyze image array counter
            var i = 0;

            // Buffer to put debayered RGB image into (3 channels)
            var convertedBuffer = new byte[_size.Width * _size.Height * 3];

            // Grab images.
            while (true)
            {
                // Wait for an image and then retrieve it. A timeout of 5000 ms is used.
                var grabResult = _camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);

                using (grabResult)
                {
                    // Image grabbed successfully?
                    if (!grabResult.GrabSucceeded)
                    {
                        Log.Error($"Error: {grabResult.ErrorCode} {grabResult.ErrorDescription}");
                        continue;
                    }

                    // Debayering RGB image
                    _converter.Convert(convertedBuffer, grabResult);

                    // Convert into EmguCV image type
                    var image = new Image<Rgb, byte>(grabResult.Width, grabResult.Height)
                    {
                        Bytes = convertedBuffer
                    };

                    // Write to recorder (if the recorder is not recording, it will discard it)
                    _recorder.Write(image);

                    // Convert to grayscale image for further analysis
                    var grayImage = image.Convert<Gray, byte>();

                    // Set background image if not set yet
                    if (background == null)
                    {
                        background = grayImage;
                    }

                    // Append to analyze array
                    images[i] = grayImage;
                    i++;

                    // Skip analysation step until we collected a full array of images
                    if (i != images.Length)
                    {
                        continue;
                    }

                    // Reset array counter
                    i = 0;

                    // Get the state of the image array (is a train entering? exiting?)
                    var newState = detector.Detect(background, images, state);

                    switch (newState)
                    {
                        case DetectorState.Entry when state != newState:
                            // TODO refactor into method
                            var savestamp = FileUtil.GenerateTimestampFilename();
                            Log.Info($"Detected train entering. Starting capture {savestamp}");
                            Publish(Commands.CaptureStart, savestamp);

                            // ensuring the recordings directory exists
                            var recordingDirectory = new DirectoryInfo(CaptureFolder);
                            if (!recordingDirectory.Exists)
                            {
                                recordingDirectory.Create();
                            }

                            var filename = $@"{CaptureFolder}\{savestamp}-visible.avi";
                            _recorder.StartRecording(filename);
                            break;
                        case DetectorState.Exit when state != newState:
                            // TODO refactor into method
                            Log.Info("Train exited. Stopping capture.");
                            Publish(Commands.CaptureStop, FileUtil.GenerateTimestampFilename());
                            _recorder.StopRecording();
                            break;
                        case DetectorState.Nothing:
                        default:
                            // nop
                            break;
                    }

                    state = newState;

                    // dispose of references to improve memory consumption
                    for (var k = 0; k < images.Length; k++)
                    {
                        images[k] = null;
                    }
                }
            }
        }

        public void Dispose()
        {
            _camera?.Dispose();
            _recorder?.Dispose();
        }
    }
}
