package ch.sebastianhaeni.thermotrains.util;

import java.io.File;
import java.nio.file.FileSystems;
import java.nio.file.Path;
import java.nio.file.PathMatcher;
import java.util.Collection;
import java.util.concurrent.LinkedBlockingDeque;

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
}
