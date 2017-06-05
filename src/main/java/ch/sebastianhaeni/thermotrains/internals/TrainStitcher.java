package ch.sebastianhaeni.thermotrains.internals;

import ch.sebastianhaeni.thermotrains.util.FileUtil;
import ch.sebastianhaeni.thermotrains.util.MatUtil;
import org.opencv.core.Core;
import org.opencv.core.Mat;
import org.opencv.core.Point;
import org.opencv.core.Scalar;

import javax.annotation.Nonnull;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

import static ch.sebastianhaeni.thermotrains.util.FileUtil.emptyFolder;
import static ch.sebastianhaeni.thermotrains.util.FileUtil.saveMat;
import static org.opencv.core.Core.*;
import static org.opencv.imgcodecs.Imgcodecs.imread;
import static org.opencv.imgproc.Imgproc.*;

public final class TrainStitcher {

  private TrainStitcher() {
    // nop
  }

  public static void stitchTrain(@Nonnull String inputFolder, @Nonnull String outputFolder) {
    emptyFolder(outputFolder);

    List<Path> inputFiles = FileUtil.getFiles(inputFolder, "**.jpg");
    List<Offset> offsets = new ArrayList<>();

    for (int i = 0; i < inputFiles.size() - 1; i++) {

      Mat imgScene = imread(inputFiles.get(i).toString());
      Mat imgObject = createTemplate(imread(inputFiles.get(i + 1).toString()));

      // Do the Matching and Normalize
      Mat result = new Mat();
      matchTemplate(imgScene, imgObject, result, TM_SQDIFF_NORMED);
      normalize(result, result, 0, 1, NORM_MINMAX, -1, new Mat());

      // Localizing the best match with minMaxLoc
      Core.MinMaxLocResult minMaxLocResult = minMaxLoc(result);

      // For SQDIFF and SQDIFF_NORMED, the best matches are lower values. For all the other methods, the higher the better
      Point matchLoc = minMaxLocResult.minLoc;

      // Show me what you got
      Point to = new Point(matchLoc.x + imgObject.cols(), matchLoc.y + imgObject.rows());
      rectangle(imgScene, matchLoc, to, Scalar.all(0), 2, 8, 0);

      Mat out = imgScene.adjustROI(0, 0, imgObject.width(), 0);

      offsets.add(new Offset((int) matchLoc.x, (int) matchLoc.y));
      saveMat(outputFolder, out, i);
    }

    Mat result = imread(inputFiles.get(0).toString());
    result = result.colRange(0, offsets.get(0).x);

    for (int i = 1; i <= offsets.size(); i++) {

      Mat right = imread(inputFiles.get(i).toString());

      // cut off at offset
      int start = getTemplateOffset(right);
      int end = i < offsets.size() ? offsets.get(i).x : right.width();

      if (start >= end) {
        continue;
      }

      right = right.colRange(start, end);

      // concatenate them side by side
      hconcat(Arrays.asList(result, right), result);
    }

    saveMat(outputFolder, result, "result");
  }

  private static final int verticalCrop = 100;

  /**
   * Creates the template used to match against the other picture.
   */
  @Nonnull
  private static Mat createTemplate(@Nonnull Mat mat) {
    int margin = getTemplateOffset(mat);
    return MatUtil.crop(mat, verticalCrop, margin, verticalCrop, margin);
  }

  private static int getTemplateOffset(@Nonnull Mat mat) {
    return (mat.width() / 2) - 100;
  }

  private static class Offset {
    int x;
    int y;

    Offset(int x, int y) {
      this.x = x;
      this.y = y;
    }
  }
}
