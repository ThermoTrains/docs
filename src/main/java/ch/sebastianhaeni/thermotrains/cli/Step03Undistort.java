package ch.sebastianhaeni.thermotrains.cli;

import ch.sebastianhaeni.thermotrains.internals.Undistort;
import org.opencv.core.Core;

import java.io.IOException;

public class Step03Undistort {
  static {
    System.loadLibrary(Core.NATIVE_LIBRARY_NAME);
  }

  public static void main(String[] args) {
    if (args.length != 3) {
      System.out.println("Usage: java -jar Step03Undistort.jar <calibration.json> <path to distorted images> <path to undistorted images>");
      return;
    }

    String calibrationJsonFile = args[0];
    String pathToDistortedImages = args[1];
    String pathToUndistortedImages = args[2];

    try {
      Undistort.undistortImages(calibrationJsonFile, pathToDistortedImages, pathToUndistortedImages);
    } catch (IOException e) {
      e.printStackTrace();
    }
  }
}
