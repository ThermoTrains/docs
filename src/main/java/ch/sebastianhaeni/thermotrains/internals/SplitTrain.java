package ch.sebastianhaeni.thermotrains.internals;

import ch.sebastianhaeni.thermotrains.util.MathUtil;
import org.opencv.core.Mat;
import org.opencv.core.Point;
import org.opencv.core.Scalar;
import org.opencv.core.Size;

import javax.annotation.Nonnull;
import java.nio.file.Path;
import java.util.List;

import static ch.sebastianhaeni.thermotrains.util.FileUtil.*;
import static org.opencv.core.Core.inRange;
import static org.opencv.imgcodecs.Imgcodecs.imread;
import static org.opencv.imgproc.Imgproc.*;

public final class SplitTrain {

  private static final int MIN_CAR_LENGTH_IN_PX = 2000;
  private static final double PEAK_THRESHOLD = 4.0;
  private static final int DILATION_SIZE = 10;

  private SplitTrain() {
    // nop
  }

  public static void cut(@Nonnull String inputFolder, @Nonnull String outputFolder) {
    emptyFolder(outputFolder);

    List<Path> files = getFiles(inputFolder, "**result.jpg");
    Mat img = imread(files.get(0).toString());

    Mat hsv = new Mat();
    cvtColor(img, hsv, COLOR_BGR2HSV);

    Scalar lower = new Scalar(0, 0, 140);
    Scalar upper = new Scalar(255, 255, 255);

    Mat dst = new Mat();
    inRange(img, lower, upper, dst);

    // dilate the threshold image to fill in holes
    Mat dilationElement = getStructuringElement(MORPH_ELLIPSE,
      new Size(2 * DILATION_SIZE + 1, 2 * DILATION_SIZE + 1),
      new Point(DILATION_SIZE, DILATION_SIZE));

    erode(dst, dst, dilationElement);

    Integer[] hist = new Integer[dst.cols()];

    for (int i = 0; i < dst.cols(); i++) {

      int withinRange = 0;

      for (int j = 0; j < dst.rows(); j++) {
        if (dst.get(j, i)[0] > 0) {
          withinRange++;
        }
      }

      hist[i] = withinRange;
    }

    double median = MathUtil.median(hist);

    // find peaks
    int lastPeak = -1;
    for (int i = 0; i < hist.length; i++) {
      if (hist[i] < median * PEAK_THRESHOLD) {
        hist[i] = 0;
      } else {
        hist[i] = 1;

        // Removes the plateau by flattening every element that is 1 before the current one in a fixed distance.
        if (lastPeak >= 0 && lastPeak + MIN_CAR_LENGTH_IN_PX > i) {
          hist[lastPeak] = 0;
        }
        lastPeak = i;
      }
    }

    int prev = 0;
    int i = 0;
    for (int x = 0; x < hist.length; x++) {
      if (hist[x] == 0.0) {
        continue;
      }

      if (x - prev < MIN_CAR_LENGTH_IN_PX) {
        prev = x;
        continue;
      }

      Mat car = img.colRange(prev, x);
      prev = x;
      saveMat(outputFolder, car, ++i);
    }
  }
}
