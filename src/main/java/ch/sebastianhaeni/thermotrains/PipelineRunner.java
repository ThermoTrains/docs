package ch.sebastianhaeni.thermotrains;

import ch.sebastianhaeni.thermotrains.internals.*;
import ch.sebastianhaeni.thermotrains.util.Procedure;
import org.opencv.core.Core;

public final class PipelineRunner {
  static {
    System.loadLibrary(Core.NATIVE_LIBRARY_NAME);
  }

  private static final int START_STEP = 4;
  private static final int STOP_STEP = 9;

  private PipelineRunner() {
  }

  public static void main(String[] args) {
    runStep(1, () -> ExtractFrames.extractFrames(
      50,
      "samples/calibration/gopro-checkerboard.mp4",
      "target/1-calibration"
    ));
    runStep(2, () -> CalibrateCamera.performCheckerboardCalibration(
      29,
      "target/1-calibration",
      "target/2-calibration-found"
    ));
    runStep(3, () -> ExtractFrames.extractFrames(
      150,
      "samples/distorted/gopro-moving-train.mp4",
      "target/3-distorted"
    ));
    runStep(4, () -> Undistort.undistortImages(
      "target/2-calibration-found/calibration.json",
      "target/3-distorted",
      "target/4-undistorted"
    ));
    runStep(5, () -> Straighten.straighten(
      "target/4-undistorted",
      "target/5-straightened"
    ));
    runStep(6, () -> MotionCrop.cropToMotion(
      "target/5-straightened",
      "target/6-cropped"
    ));
    runStep(7, () -> PerspectiveTransformer.transform(
      "target/6-cropped",
      "target/7-transformed"
    ));
    runStep(8, () -> TrainStitcher.stitchTrain(
      "target/7-transformed",
      "target/8-stitched"
    ));
    runStep(9, () -> CarCut.cut(
      "target/8-stitched",
      "target/9-car-cut"
    ));
  }

  private static void runStep(int step, Procedure<?> procedure) {
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
