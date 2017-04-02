package ch.sebastianhaeni.thermotrains.cli;

import ch.sebastianhaeni.thermotrains.internals.CalibrateCamera;
import org.opencv.core.Core;

public class Step02CalibrateCamera {
  static {
    System.loadLibrary(Core.NATIVE_LIBRARY_NAME);
  }

  public static void main(String[] args) {
    if (args.length != 1) {
      System.out.println("Usage: java -jar Step02CalibrateCamera.jar <frame folder>");
      return;
    }

    String inputCheckerboardFrameFolder = args[0];
    CalibrateCamera.performCheckerboardCalibration(inputCheckerboardFrameFolder, 29);
  }
}
