package ch.sebastianhaeni.thermotrains;

import ch.sebastianhaeni.thermotrains.internals.CalibrateCamera;
import ch.sebastianhaeni.thermotrains.internals.ExtractFrames;
import ch.sebastianhaeni.thermotrains.internals.Undistort;

public class PipelineRunner {

  public static void main(String[] args){
    // Step 1
    ExtractFrames.extractFrames("../samples/gopro-checkerboard.mp4", 20);

    // Step 2
    CalibrateCamera.performCheckerboardCalibration("../samples/checkerboard-frames", 29);

    // Step 3
    Undistort.undistortImages("../samples/calibration.json", "../samples/distorted");
  }

}
