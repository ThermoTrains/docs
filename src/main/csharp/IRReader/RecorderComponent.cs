using System;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using Flir.Atlas.Live.Device;
using Flir.Atlas.Live.Recorder;
using Flir.Atlas.Live.Remote;
using log4net;
using SebastianHaeni.ThermoBox.Common;

namespace SebastianHaeni.ThermoBox.IRReader
{
    class RecorderComponent : ThermoBoxComponent
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly string RECORDINGS_FOLDER = ConfigurationManager.AppSettings["IR_RECORDINGS_FOLDER"];

        private Camera camera;
        private static string currentRecordingDirectory;

        public RecorderComponent(Camera camera) : base()
        {
            this.camera = camera;
        }

        protected override void Configure()
        {
            Subscription(Commands.CaptureStart, (channel, message) => StartCapture(message));
            Subscription(Commands.CaptureStop, (channel, message) => StopCapture());
            Subscription(Commands.CaptureAbort, (channel, message) => AbortCapture());
        }

        private void StartCapture(string message)
        {
            if (camera.Recorder.Status != RecorderState.Stopped)
            {
                log.Warn($"Cannot start recording. Current state is {camera.Recorder.Status}");
                return;
            }

            // ensuring the recordings directory exists
            DirectoryInfo recordingDirectory = new DirectoryInfo(RECORDINGS_FOLDER);
            if (!recordingDirectory.Exists)
            {
                recordingDirectory.Create();
            }

            log.Info($"Starting capture with id {message}");
            currentRecordingDirectory = $@"{RECORDINGS_FOLDER}\{message}";

            Retry(() => camera.Recorder.Start($@"{currentRecordingDirectory}\FLIR Recording.seq"));
        }

        private void StopCapture()
        {
            var artifactDirectory = currentRecordingDirectory; // copy this as a new recording might start while we're finishing this one

            if (camera.Recorder.Status != RecorderState.Recording)
            {
                log.Warn($"Cannot stop recording. Current state is {camera.Recorder.Status}");
                return;
            }

            log.Info($"Stopping capture");
            Retry(() => camera.Recorder.Stop());
            log.Info($"Recorded {camera.Recorder.FrameCount} frames");

            // Zipping the file as it's a non compressed format and compression rates of 50 - 80% can be achieved
            var zipFilename = $@"{artifactDirectory}.zip";
            log.Info($"Zipping directory {artifactDirectory} to {zipFilename}");

            if (File.Exists(zipFilename))
            {
                log.Warn($"File {zipFilename} already exists, overwriting it");
                File.Delete(zipFilename);
            }

            ZipFile.CreateFromDirectory(artifactDirectory, zipFilename);

            // Deleting source artifact (that one that's uncompressed on the disk)
            new DirectoryInfo(artifactDirectory).Delete(true);

            // Send publish command with the zip file
            Publish(Commands.Upload, zipFilename);
        }

        private void AbortCapture()
        {
            if (camera.Recorder.Status == RecorderState.Stopped)
            {
                log.Warn($"Cannot stop recording. It is already stopped");
                return;
            }

            log.Info($"Aborting capture");
            Retry(() => camera.Recorder.Stop());

            // Deleting generated artifact
            new DirectoryInfo(currentRecordingDirectory).Delete(true);
        }

        /// <summary>
        /// Tries to execute an action. If an exception is thrown, it tries again until a threshold is reached.
        /// </summary>
        /// <param name="action">Action with potential exception thrown</param>
        private static void Retry(Action action)
        {
            int tries = 0;
            int MAX_TRIES = 5;

            while (tries < MAX_TRIES)
            {
                try
                {
                    action.Invoke();
                    return;
                }
                catch (CommandFailedException ex)
                {
                    tries++;
                    log.Warn($"Error executing camera command (try {tries} of {MAX_TRIES})", ex);
                    Thread.Sleep(500);
                }
            }

            log.Error($"Could not execute command after {tries} tries.");
        }
    }
}
