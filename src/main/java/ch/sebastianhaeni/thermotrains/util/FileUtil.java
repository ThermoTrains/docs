package ch.sebastianhaeni.thermotrains.util;

import org.opencv.core.Mat;

import java.io.File;
import java.nio.file.FileSystems;
import java.nio.file.Path;
import java.nio.file.PathMatcher;
import java.util.ArrayList;
import java.util.List;

import static org.opencv.imgcodecs.Imgcodecs.imwrite;

/**
 * MathUtil functions to handle files.
 */
public final class FileUtil {
  private FileUtil() {
    // nop
  }

  /**
   * Get a list of files in the folder matching the pattern.
   */
  public static List<Path> getFiles(String inputFolder, String globPattern) {
    List<Path> inputFiles = new ArrayList<>();
    PathMatcher matcher = FileSystems.getDefault().getPathMatcher("glob:" + globPattern);
    File folder = new File(inputFolder);

    File[] files = folder.listFiles();
    if (files == null) {
      throw new IllegalStateException("Cannot read files from folder: " + inputFolder);
    }

    for (File file : files) {
      if (matcher.matches(file.toPath())) {
        inputFiles.add(file.toPath());
      }
    }

    return inputFiles;
  }

  /**
   * Saves the {@link Mat} to the folder with the formatted integer index.
   */
  public static void saveMat(String outputFolder, Mat mat, int index) {
    String filename = String.format("%04d.jpg", index);
    File file = getFile(outputFolder, filename);
    imwrite(file.getAbsolutePath(), mat);
    System.out.printf("saved %s\n", file.getAbsolutePath());
  }

  /**
   * Saves the {@link Mat} to the folder with the given filename. The extension .jpg is automatically added.
   */
  public static void saveMat(String outputFolder, Mat mat, String filename) {
    File file = getFile(outputFolder, filename + ".jpg");
    imwrite(file.getAbsolutePath(), mat);
    System.out.printf("saved %s\n", file.getAbsolutePath());
  }

  /**
   * Gets the file reference to the given file. If the folder it should be in, doesn't exist yet, it will be created.
   */
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

  /**
   * Clears the given folder of any content.
   */
  public static void emptyFolder(String folder) {
    File dir = new File(folder);

    File[] files = dir.listFiles();
    if (files == null) {
      throw new IllegalStateException("Cannot read files from folder: " + folder);
    }

    for (File file : files) {
      if (!file.delete()) {
        throw new IllegalStateException("Could not delete file: " + file.toString());
      }
    }
  }
}
