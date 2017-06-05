package ch.sebastianhaeni.thermotrains.internals;

import ch.sebastianhaeni.thermotrains.internals.geometry.BoundingBox;
import ch.sebastianhaeni.thermotrains.internals.geometry.Line;
import org.opencv.core.CvType;
import org.opencv.core.Mat;
import org.opencv.core.Point;
import org.opencv.core.Size;

import javax.annotation.Nonnull;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;

import static ch.sebastianhaeni.thermotrains.util.FileUtil.*;
import static ch.sebastianhaeni.thermotrains.util.MatUtil.crop;
import static com.google.common.base.Preconditions.checkState;
import static org.opencv.core.Core.*;
import static org.opencv.imgcodecs.Imgcodecs.imread;
import static org.opencv.imgproc.Imgproc.*;

public final class Rectify {

  private static final int FREQUENCY_RESOLUTION = 2;

  private Rectify() {
    // nop
  }

  public static void transform(@Nonnull String inputFolder, @Nonnull String outputFolder) {
    emptyFolder(outputFolder);

    List<Path> files = getFiles(inputFolder, "**.jpg");

    for (int i = 0; i < files.size(); i++) {
      Path path = files.get(i);

      Mat img = imread(path.toString());

      BoundingBox polygon = findBoundingBox(img);
      BoundingBox rectangle = rectifyBox(polygon);

      Mat perspectiveTransform = getPerspectiveTransform(polygon.getMat(), rectangle.getMat());

      Mat out = new Mat();
      warpPerspective(img, out, perspectiveTransform, new Size(img.width(), img.height()));

      saveMat(outputFolder, out, i);
    }
  }

  /**
   * Prepare the img in highlighting the upper edge and lower edge of the car. Then use hough to get the lines from that
   * and return the bounding box of the two lines.
   */
  @Nonnull
  private static BoundingBox findBoundingBox(@Nonnull Mat img) {
    Mat w = img.clone();

    // give it a good blur
    GaussianBlur(w, w, w.size(), 4);

    // get value from HSV
    cvtColor(w, w, COLOR_BGR2HSV);
    List<Mat> channels = new ArrayList<>();
    split(w, channels);
    Mat value = channels.get(2);

    int fullHeight = value.height();
    int height = fullHeight / 3;

    Mat upperPart = crop(value, 0, 0, fullHeight - height, 0);
    Mat lowerPart = crop(value, fullHeight - height, 0, 0, 0);

    Line line1 = getLine(upperPart)
      .expand(0, img.width(), 0, fullHeight);
    Line line2 = getLine(lowerPart)
      .translate(0, fullHeight - height)
      .expand(0, img.width(), 0, fullHeight);

    return new BoundingBox(line1, line2);
  }

  /**
   * Gets the strongest line based on y frequency changes.
   */
  @Nonnull
  private static Line getLine(@Nonnull Mat src) {
    Mat lines = findMaxYFrequency(src);

    HoughLinesP(lines, lines, 1.0, Math.PI / 180, 50, 50.0, 30.0);

    checkState(lines.rows() > 0, "Didn't find at least 1 line, cannot create bounding box");

    double[] val = lines.get(0, 0);
    Point p1 = new Point(val[0] * FREQUENCY_RESOLUTION, val[1] * FREQUENCY_RESOLUTION);
    Point p2 = new Point(val[2] * FREQUENCY_RESOLUTION, val[3] * FREQUENCY_RESOLUTION);

    return new Line(p1, p2);
  }

  /**
   * Searches for the maximum frequency in y direction in every column and marks it white.
   */
  @Nonnull
  private static Mat findMaxYFrequency(@Nonnull Mat src) {

    Mat w = new Mat();
    resize(src, w, new Size(src.width() / FREQUENCY_RESOLUTION, src.height() / FREQUENCY_RESOLUTION));

    Size diffSize = new Size(w.width(), w.height() - 1);

    Mat top = new Mat(diffSize, CvType.CV_8UC1);
    Mat bottom = new Mat(diffSize, CvType.CV_8UC1);

    w.rowRange(1, w.height() - 1).copyTo(top);
    w.rowRange(2, w.height()).copyTo(bottom);

    Mat diff = new Mat();
    absdiff(top, bottom, diff);

    Mat output = new Mat(diffSize, CvType.CV_8UC1);

    for (int x = 0; x < output.width(); x++) {
      MinMaxLocResult max = minMaxLoc(diff.col(x));
      output.put((int) max.maxLoc.y, x, 255);
    }

    return output;
  }

  /**
   * Creates a bounding rectangle from the polygon.
   */
  @Nonnull
  private static BoundingBox rectifyBox(@Nonnull BoundingBox polygon) {
    double top = Math.min(polygon.getTopLeft().y, polygon.getTopRight().y);
    double bottom = Math.max(polygon.getBottomLeft().y, polygon.getBottomRight().y);

    return new BoundingBox(
      new Point(polygon.getTopLeft().x, top),
      new Point(polygon.getTopRight().x, top),
      new Point(polygon.getBottomRight().x, bottom),
      new Point(polygon.getBottomLeft().x, bottom)
    );
  }
}
