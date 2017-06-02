package ch.sebastianhaeni.thermotrains.internals;


import org.opencv.core.Mat;
import org.opencv.videoio.VideoCapture;
import org.opencv.videoio.Videoio;

import static ch.sebastianhaeni.thermotrains.util.FileUtil.saveMat;

public final class ExtractFrames {
  private ExtractFrames() {
  }

  public static void extractFrames(int framesToExtract, String inputVideoFilename, String outputFolder) {
    VideoCapture capture = new VideoCapture();

    if (!capture.open(inputVideoFilename)) {
      System.out.println("Cannot open the video file");
      return;
    }

    double frameCount = capture.get(Videoio.CAP_PROP_FRAME_COUNT);
    int frameCounter = 0;

    for (int i = 0; i < frameCount; i++) {
      Mat frame = new Mat();
      boolean success = capture.read(frame);
      if (!success) {
        System.out.println("Cannot read frame " + i);
        continue;
      }

      if (i == 0 || i % (int) (frameCount / framesToExtract) != 0) {
        // do not extract every frame, but once in a while so we have a fixed number of frames
        // not correlated to the frame count
        continue;
      }

      saveMat(outputFolder, frame, ++frameCounter);
    }
  }
}
