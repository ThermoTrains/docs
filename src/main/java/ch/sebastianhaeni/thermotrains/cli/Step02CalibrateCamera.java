package ch.sebastianhaeni.thermotrains.cli;

import ch.sebastianhaeni.thermotrains.internals.CalibrateCamera;
import org.opencv.core.Core;

import java.io.FileNotFoundException;

public class Step02CalibrateCamera {
  static {
    System.loadLibrary(Core.NATIVE_LIBRARY_NAME);
  }

  public static void main(String[] args) {
    if (args.length != 2) {
      System.out.println("Usage: java -jar Step02CalibrateCamera.jar <frame folder> <output folder>");
      return;
    }

    String inputCheckerboardFrameFolder = args[0];
    String outputFolder = args[1];

    try {
      CalibrateCamera.performCheckerboardCalibration(inputCheckerboardFrameFolder, 29, outputFolder);
    } catch (FileNotFoundException e) {
      e.printStackTrace();
    }
  }
}
