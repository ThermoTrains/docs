package ch.sebastianhaeni.thermotrains.internals;

import java.nio.file.Path;
import java.util.List;

import org.opencv.core.Mat;
import org.opencv.core.Point;
import org.opencv.core.Scalar;
import org.opencv.core.Size;

import ch.sebastianhaeni.thermotrains.util.Util;

import static ch.sebastianhaeni.thermotrains.util.FileUtil.emptyFolder;
import static ch.sebastianhaeni.thermotrains.util.FileUtil.getFiles;
import static ch.sebastianhaeni.thermotrains.util.FileUtil.saveMat;
import static org.opencv.core.Core.inRange;
import static org.opencv.imgcodecs.Imgcodecs.imread;
import static org.opencv.imgproc.Imgproc.COLOR_BGR2HSV;
import static org.opencv.imgproc.Imgproc.MORPH_ELLIPSE;
import static org.opencv.imgproc.Imgproc.cvtColor;
import static org.opencv.imgproc.Imgproc.erode;
import static org.opencv.imgproc.Imgproc.getStructuringElement;

public final class CarCut {
  private CarCut() {
  }

  public static void cut(String inputFolder, String outputFolder) {
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
    int dilationSize = 10;
    Mat dilationElement = getStructuringElement(MORPH_ELLIPSE,
      new Size(2 * dilationSize + 1, 2 * dilationSize + 1),
      new Point(dilationSize, dilationSize));

    erode(dst, dst, dilationElement);

    int[] hist = new int[dst.cols()];

    for (int i = 0; i < dst.cols(); i++) {

      int withinRange = 0;

      for (int j = 0; j < dst.rows(); j++) {
        if (dst.get(j, i)[0] > 0) {
          withinRange++;
        }
      }

      hist[i] = withinRange;
    }

    double median = Util.median(hist);

    // find peaks
    for (int i = 0; i < hist.length; i++) {
      if (hist[i] < median * 3.0) {
        hist[i] = 0;
      } else {
        hist[i] = 1;
      }

      // remove plateau
      plateau(hist, i);
    }

    int prev = 0;
    int i = 0;
    for (int x = 0; x < hist.length; x++) {
      if (hist[x] == 0.0) {
        continue;
      }

      if (x - prev < 400) {
        prev = x;
        continue;
      }

      Mat car = img.colRange(prev, x);
      prev = x;
      saveMat(outputFolder, car, ++i);
    }
  }

  private static void plateau(int[] arr, int i) {
    int minDist = 500;
    if (arr[i] != 1) {
      return;
    }

    for (int j = Math.max(i - minDist, 0); j < i; j++) {
      if (arr[j] == 1) {
        arr[j] = 0;
        return;
      }
    }
  }
}
