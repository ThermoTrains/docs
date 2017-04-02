package ch.sebastianhaeni.thermotrains.cli;

import ch.sebastianhaeni.thermotrains.internals.Undistort;

public class Step03Undistort {

  public static void main(String[] args) {
    if (args.length != 2) {
      System.out.println("Usage: java -jar Step03Undistort.jar <calibration.json> <path to distorted images>");
      return;
    }

    String calibrationJsonFile = args[0];
    String pathToDistoredImages = args[1];

    Undistort.undistortImages(calibrationJsonFile, pathToDistoredImages);
  }

}
