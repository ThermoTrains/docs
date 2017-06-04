package ch.sebastianhaeni.thermotrains;

import ch.sebastianhaeni.thermotrains.internals.*;
import ch.sebastianhaeni.thermotrains.util.Procedure;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.opencv.core.Core;

import static ch.sebastianhaeni.thermotrains.internals.ExtractFrames.Direction.FORWARD;

public final class PipelineRunner {
  static {
    // load OpenCV native library
    System.loadLibrary(Core.NATIVE_LIBRARY_NAME);
  }
  private static final Logger LOG = LogManager.getLogger(PipelineRunner.class);

  private static final int START_STEP = 3;
  private static final int STOP_STEP = 9;

  private PipelineRunner() {
    // nop
  }

  public static void main(String[] args) {
    runStep(1, () -> ExtractFrames.extractFrames(
      50,
      FORWARD,
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
      FORWARD,
      "samples/distorted/gopro-moving-train-1.mp4",
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
    runStep(7, () -> Rectify.transform(
      "target/6-cropped",
      "target/7-rectified"
    ));
    runStep(8, () -> TrainStitcher.stitchTrain(
      "target/7-rectified",
      "target/8-stitched"
    ));
    runStep(9, () -> SplitTrain.cut(
      "target/8-stitched",
      "target/9-final"
    ));
  }

  private static void runStep(int step, Procedure<?> procedure) {
    if (START_STEP > step || STOP_STEP < step) {
      return;
    }

    LOG.info("Running step {}", step);

    try {
      procedure.run();
    } catch (Exception e) {
      e.printStackTrace();
      System.exit(1);
    }
  }
}
