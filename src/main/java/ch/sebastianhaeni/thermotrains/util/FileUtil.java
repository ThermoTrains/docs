package ch.sebastianhaeni.thermotrains.util;

import org.opencv.core.Mat;

import java.io.File;
import java.nio.file.FileSystems;
import java.nio.file.Path;
import java.nio.file.PathMatcher;
import java.util.Collection;
import java.util.concurrent.LinkedBlockingDeque;

import static org.opencv.imgcodecs.Imgcodecs.imwrite;

public class FileUtil {
  public static Collection<Path> getFiles(String inputCheckerboardFrameFolder, String globPattern) {
    LinkedBlockingDeque<Path> inputFiles = new LinkedBlockingDeque<>();
    PathMatcher matcher = FileSystems.getDefault().getPathMatcher("glob:" + globPattern);
    File folder = new File(inputCheckerboardFrameFolder);

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
