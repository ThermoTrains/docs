package ch.sebastianhaeni.thermotrains;

import ch.sebastianhaeni.thermotrains.internals.*;
import ch.sebastianhaeni.thermotrains.util.Procedure;
import org.opencv.core.Core;

public class PipelineRunner {
  static {
    System.loadLibrary(Core.NATIVE_LIBRARY_NAME);
  }

  private static final int START_STEP = 7;
  private static final int STOP_STEP = 7;

  public static void main(String[] args) {
    runStep(1, () -> ExtractFrames.extractFrames(
      50,
      "samples/calibration/gopro-checkerboard.mp4",
      "target/calibration"
    ));
    runStep(2, () -> CalibrateCamera.performCheckerboardCalibration(
      29,
      "target/calibration",
      "target/calibration-found"
    ));
    runStep(3, () -> ExtractFrames.extractFrames(
      150,
      "samples/distorted/gopro-moving-train.mp4",
      "target/distorted"
    ));
    runStep(4, () -> Undistort.undistortImages(
      "target/calibration-found/calibration.json",
      "target/distorted",
      "target/undistorted"
    ));
    runStep(5, () -> Straighten.straighten(
      "target/undistorted",
      "target/straightened"
    ));
    runStep(6, () -> MotionCrop.cropToMotion(
      "target/straightened",
      "target/cropped"
    ));
    runStep(7, () -> TrainStitcher.stitchTrain(
      "target/cropped",
      "target/stitched"
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
