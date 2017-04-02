package ch.sebastianhaeni.thermotrains.cli;

import ch.sebastianhaeni.thermotrains.internals.ExtractFrames;
import org.opencv.core.Core;

public class Step01ExtractFrames {
  static {
    System.loadLibrary(Core.NATIVE_LIBRARY_NAME);
  }

  public static void main(String[] args) {
    if (args.length != 2) {
      System.out.println("Usage: java -jar Step01ExtractFrames.jar <video> <frame count>");
      return;
    }

    String inputVideoFilename = args[0];
    int framesToExtract = Integer.parseInt(args[1]);

    ExtractFrames.extractFrames(inputVideoFilename, framesToExtract);
  }
}
