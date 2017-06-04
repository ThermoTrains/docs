package ch.sebastianhaeni.thermotrains.internals;

import ch.sebastianhaeni.thermotrains.util.Direction;
import org.opencv.core.CvType;
import org.opencv.core.Mat;
import org.opencv.core.Size;

import java.nio.file.Path;
import java.util.List;

import static ch.sebastianhaeni.thermotrains.util.FileUtil.*;
import static org.opencv.core.Core.flip;
import static org.opencv.imgcodecs.Imgcodecs.imread;
import static org.opencv.imgproc.Imgproc.getPerspectiveTransform;
import static org.opencv.imgproc.Imgproc.warpPerspective;

public final class Rectify {

  private Rectify() {
    // nop
  }

  public static void transform(Direction direction, String inputFolder, String outputFolder) {
    emptyFolder(outputFolder);

    // TODO figure out the corner points of the train and write them to src
    Mat src = new Mat(new Size(4, 1), CvType.CV_32FC2);
    // TODO transform them into a rectangle and write them to dst
    Mat dst = new Mat(new Size(4, 1), CvType.CV_32FC2);

    double srcTopLeftX = 613;
    double srcTopLeftY = 74;
    double srcTopRightX = 1732;
    double srcTopRightY = 39;
    double srcBottomRightX = 1740;
    double srcBottomRightY = 532;
    double srcBottomLeftX = 605;
    double srcBottomLeftY = 536;

    src.put(0, 0,
      srcTopLeftX, srcTopLeftY,
      srcTopRightX, srcTopRightY,
      srcBottomRightX, srcBottomRightY,
      srcBottomLeftX, srcBottomLeftY);

    double dstTopLeftX = 640;
    double dstTopLeftY = 46;
    double dstTopRightX = 1732;
    double dstTopRightY = 39;
    double dstBottomRightX = 1740;
    double dstBottomRightY = 532;
    double dstBottomLeftX = 645;
    double dstBottomLeftY = 536;

    dst.put(0, 0,
      dstTopLeftX, dstTopLeftY,
      dstTopRightX, dstTopRightY,
      dstBottomRightX, dstBottomRightY,
      dstBottomLeftX, dstBottomLeftY);

    Mat perspectiveTransform = getPerspectiveTransform(src, dst);

    List<Path> files = getFiles(inputFolder, "**.jpg");

    for (int i = 0; i < files.size(); i++) {
      Path path = files.get(i);

      Mat img = imread(path.toString());

      if (direction == Direction.REVERSE) {
        // flip hard coded matrix
        // TODO remove when automatic
        flip(img, img, 1);
      }

      Mat out = new Mat();
      warpPerspective(img, out, perspectiveTransform, new Size(img.width(), img.height()));

      if (direction == Direction.REVERSE) {
        // flip back hard coded matrix
        // TODO remove when automatic
        flip(out, out, 1);
      }

      saveMat(outputFolder, out, i);
    }
  }
}
