package ch.sebastianhaeni.thermotrains.internals;

import ch.sebastianhaeni.thermotrains.util.FileUtil;
import ch.sebastianhaeni.thermotrains.util.MatUtil;
import com.google.common.base.MoreObjects;
import org.opencv.core.*;

import java.nio.file.Path;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

import static ch.sebastianhaeni.thermotrains.util.FileUtil.saveMat;
import static org.opencv.core.Core.*;
import static org.opencv.imgcodecs.Imgcodecs.imread;
import static org.opencv.imgproc.Imgproc.*;

public class TrainStitcher {

  public static void stitchTrain(String inputFolder, String outputFolder) {

    List<Path> inputFiles = FileUtil.getFiles(inputFolder, "**.jpg");
    List<Offset> offsets = new ArrayList<>();

    for (int i = 0; i < inputFiles.size() - 1; i++) {

      Mat img_scene = imread(inputFiles.get(i).toString());
      Mat img_object = createTemplate(imread(inputFiles.get(i + 1).toString()));

      // Do the Matching and Normalize
      Mat result = new Mat();
      matchTemplate(img_scene, img_object, result, TM_SQDIFF_NORMED);
      normalize(result, result, 0, 1, NORM_MINMAX, -1, new Mat());

      // Localizing the best match with minMaxLoc
      Core.MinMaxLocResult minMaxLocResult = minMaxLoc(result);

      // For SQDIFF and SQDIFF_NORMED, the best matches are lower values. For all the other methods, the higher the better
      Point matchLoc = minMaxLocResult.minLoc;

      // Show me what you got
      Point to = new Point(matchLoc.x + img_object.cols(), matchLoc.y + img_object.rows());
      rectangle(img_scene, matchLoc, to, Scalar.all(0), 2, 8, 0);

      Mat out = img_scene.adjustROI(0, 0, img_object.width(), 0);

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

  private static Mat createTemplate(Mat mat) {
    int margin = getTemplateOffset(mat);
    return MatUtil.crop(mat, verticalCrop, margin, verticalCrop, margin);
  }

  private static int getTemplateOffset(Mat mat) {
    return (mat.width() / 2) - 100;
  }

  private static class Offset {
    int x;
    int y;

    Offset(int x, int y) {
      this.x = x;
      this.y = y;
    }

    @Override
    public String toString() {
      return MoreObjects.toStringHelper(this)
        .add("x", x)
        .add("y", y)
        .toString();
    }
  }
}
