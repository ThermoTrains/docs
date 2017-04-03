package ch.sebastianhaeni.thermotrains;

import ch.sebastianhaeni.thermotrains.internals.CalibrateCamera;
import ch.sebastianhaeni.thermotrains.internals.ExtractFrames;
import ch.sebastianhaeni.thermotrains.internals.Undistort;
import ch.sebastianhaeni.thermotrains.util.Procedure;
import org.opencv.core.Core;

public class PipelineRunner {
  static {
    System.loadLibrary(Core.NATIVE_LIBRARY_NAME);
  }

  private static final int START_STEP = 1;
  private static final int STOP_STEP = 3;

  public static void main(String[] args) {
    // Step 1
    runStep(1, () -> ExtractFrames.extractFrames(
      "samples/calibration/gopro-checkerboard.mp4",
      20,
      "samples/calibration"
    ));

    runStep(1, () -> ExtractFrames.extractFrames(
      "samples/distorted/gopro-moving-train.mp4",
      10,
      "samples/distorted"
    ));

    // Step 2
    runStep(2, () -> CalibrateCamera.performCheckerboardCalibration(
      "samples/calibration",
      29,
      "samples/calibration-found"
    ));

    // Step 3
    runStep(3, () -> Undistort.undistortImages(
      "samples/calibration-found/calibration.json",
      "samples/distorted",
      "samples/undistorted"
    ));
  }

  private static void runStep(int step, Procedure procedure) {
    if (START_STEP > step || STOP_STEP < step) {
      return;
    }

    System.out.println("Running step " + step);

    try {
      procedure.run();
    } catch (Exception e) {
      e.printStackTrace();
      System.exit(1);
    }
  }
}
