package ch.sebastianhaeni.thermotrains.util;

import org.opencv.core.Mat;

import java.io.File;
import java.nio.file.FileSystems;
import java.nio.file.Path;
import java.nio.file.PathMatcher;
import java.util.ArrayList;
import java.util.List;

import static org.opencv.imgcodecs.Imgcodecs.imwrite;

public class FileUtil {
  public static List<Path> getFiles(String inputFolder, String globPattern) {
    List<Path> inputFiles = new ArrayList<>();
    PathMatcher matcher = FileSystems.getDefault().getPathMatcher("glob:" + globPattern);
    File folder = new File(inputFolder);

    if (folder.listFiles() == null) {
      throw new IllegalStateException("Cannot read files from folder: " + inputFolder);
    }

    for (File file : folder.listFiles()) {
      if (matcher.matches(file.toPath())) {
        inputFiles.add(file.toPath());
      }
    }

    return inputFiles;
  }

  public static void saveMat(String outputFolder, Mat mat, int index) {
    String filename = String.format("%04d.jpg", index);
    File file = getFile(outputFolder, filename);
    imwrite(file.getAbsolutePath(), mat);
    System.out.printf("saved %s\n", file.getAbsolutePath());
  }

  public static void saveMat(String outputFolder, Mat mat, String filename) {
    File file = getFile(outputFolder, filename + ".jpg");
    imwrite(file.getAbsolutePath(), mat);
    System.out.printf("saved %s\n", file.getAbsolutePath());
  }

  public static File getFile(String outputFolder, String filename) {
    File folder = new File(outputFolder);

    if (!folder.exists()) {
      if (!folder.mkdir()) {
        System.out.printf("Created dir %s\n", outputFolder);
        throw new IllegalStateException("Could not create dir " + outputFolder);
      }
    }

    return new File(outputFolder, filename);
  }
}
