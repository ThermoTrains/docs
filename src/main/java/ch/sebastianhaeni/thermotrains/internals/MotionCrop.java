package ch.sebastianhaeni.thermotrains.internals;

import ch.sebastianhaeni.thermotrains.util.FileUtil;
import org.opencv.core.*;
import org.opencv.imgproc.Imgproc;

import java.io.File;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.Collection;
import java.util.List;
import java.util.stream.IntStream;

import static ch.sebastianhaeni.thermotrains.util.FileUtil.saveMat;
import static org.opencv.core.Core.absdiff;
import static org.opencv.imgcodecs.Imgcodecs.imread;
import static org.opencv.imgproc.Imgproc.*;

public class MotionCrop {
  public static void cropToMotion(String inputFolder, String outputFolder) {
    File fBackground = new File(inputFolder, "0001.jpg");

    Mat background = imread(fBackground.toPath().toString());
    cvtColor(background, background, Imgproc.COLOR_BGR2GRAY);
    int kernelSize = 3 * 2;
    blur(background, background, new Size(kernelSize, kernelSize));

    int i = 0;
    Collection<Path> inputFiles = FileUtil.getFiles(inputFolder, "**.jpg");

    for (Path inputFile : inputFiles) {
      Mat img = imread(inputFile.toString());
      Mat dst = crop(img, background);

      if (dst == null) {
        continue;
      }

      // save to disk
      saveMat(outputFolder, dst, ++i);
    }
  }

  private static Mat crop(Mat source, Mat background) {
    Mat dst = new Mat();
    source.copyTo(dst);
    Mat gray = new Mat();
    cvtColor(dst, gray, Imgproc.COLOR_BGR2GRAY);

    Mat diff = new Mat();
    Mat t = new Mat();

    // compute absolute diff between current frame and first frame
    absdiff(background, gray, diff);
    threshold(diff, t, 40.0, 255.0, Imgproc.THRESH_BINARY);

    // dilate the threshold image to fill in holes
    int dilationSize = 10;
    int erodeSize = 50;
    Mat dilationElement = getStructuringElement(MORPH_ELLIPSE,
      new Size(2 * dilationSize + 1, 2 * dilationSize + 1),
      new Point(dilationSize, dilationSize));

    // erode to get uniformly rounded contours
    Mat erodeElement = getStructuringElement(MORPH_ELLIPSE,
      new Size(2 * erodeSize + 1, 2 * erodeSize + 1),
      new Point(erodeSize, erodeSize));
    erode(t, t, dilationElement);
    dilate(t, t, erodeElement);

    // find contours
    List<MatOfPoint> contours = new ArrayList<>();
    Mat hierarchy = new Mat();
    findContours(t, contours, hierarchy, RETR_EXTERNAL, CHAIN_APPROX_SIMPLE);

    if (contours.isEmpty()) {
      // no contours, so we purge
      return null;
    }

    MatOfPoint largestContour = contours.get(0);

    // find bounding box of contour
    int top = streamCoordinates(largestContour, 1).min().orElse(0);
    int bottom = streamCoordinates(largestContour, 1).max().orElse(dst.height());
    int left = streamCoordinates(largestContour, 0).min().orElse(0);
    int right = streamCoordinates(largestContour, 0).max().orElse(dst.width());

    if (right - left < (dst.width() * .9)) {
      // => the motion area covers not almost the whole width
      // this can be one of the following reasons
      // - it's the start of the train
      // - it's the end of the train
      // - a bird flew through the picture
      return null;
    }

    return new Mat(dst, new Rect(left, top, right - left, bottom - top));
  }

  private static IntStream streamCoordinates(MatOfPoint contour, int index) {
    return IntStream.rangeClosed(0, contour.rows() - 1)
      .map(i -> (int) contour.get(i, 0)[index]);
  }
}
