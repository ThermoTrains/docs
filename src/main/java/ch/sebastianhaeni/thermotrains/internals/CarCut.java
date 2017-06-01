package ch.sebastianhaeni.thermotrains.internals;

import org.opencv.core.*;

import java.nio.file.Path;
import java.util.Arrays;
import java.util.List;

import static ch.sebastianhaeni.thermotrains.util.FileUtil.getFiles;
import static ch.sebastianhaeni.thermotrains.util.FileUtil.saveMat;
import static org.opencv.core.Core.inRange;
import static org.opencv.imgcodecs.Imgcodecs.imread;
import static org.opencv.imgproc.Imgproc.*;

public class CarCut {
  public static void cut(String inputFolder, String outputFolder) {
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

    Mat hist = new Mat(new Size(dst.cols(), 1), CvType.CV_8UC1);
    double[] histArray = new double[dst.cols()];

    for (int i = 0; i < dst.cols(); i++) {

      int withinRange = 0;

      for (int j = 0; j < dst.rows(); j++) {
        if (dst.get(j, i)[0] > 0) {
          withinRange++;
        }
      }

      hist.put(0, i, withinRange);
      histArray[i] = withinRange;
    }

    double median = median(histArray);

    // find peaks
    for (int i = 0; i < histArray.length; i++) {
      if (histArray[i] < median * 3.0) {
        histArray[i] = 0.0;
      } else {
        histArray[i] = 1.0;
      }

      // remove plateau
      plateau(histArray, i);
    }

    int prev = 0;
    int i = 0;
    for (int x = 0; x < histArray.length; x++) {
      if (histArray[x] == 0.0) {
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

  private static void plateau(double[] histArray, int i) {
    int minDist = 500;
    if (histArray[i] != 1.0) {
      return;
    }

    for (int j = Math.max(i - minDist, 0); j < i; j++) {
      if (histArray[j] == 1.0) {
        histArray[j] = 0.0;
        return;
      }
    }
  }

  private static double median(double[] numArray) {
    double[] clone = numArray.clone();
    Arrays.sort(clone);

    if (clone.length % 2 == 0) {
      return (clone[clone.length / 2] + clone[clone.length / 2 - 1]) / 2;
    }

    return clone[clone.length / 2];
  }
}
