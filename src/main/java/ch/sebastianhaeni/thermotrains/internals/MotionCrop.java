package ch.sebastianhaeni.thermotrains.internals;

import java.nio.file.Path;
import java.util.ArrayList;
import java.util.Collection;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.function.ToIntFunction;
import java.util.stream.IntStream;

import javax.annotation.Nonnull;

import ch.sebastianhaeni.thermotrains.internals.geometry.MarginBox;
import ch.sebastianhaeni.thermotrains.util.FileUtil;
import ch.sebastianhaeni.thermotrains.util.MatUtil;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.opencv.core.Mat;
import org.opencv.core.MatOfPoint;
import org.opencv.core.Point;
import org.opencv.core.Rect;
import org.opencv.core.Size;
import org.opencv.imgproc.Imgproc;

import static ch.sebastianhaeni.thermotrains.util.FileUtil.emptyFolder;
import static ch.sebastianhaeni.thermotrains.util.FileUtil.saveMat;
import static ch.sebastianhaeni.thermotrains.util.MathUtil.median;
import static org.opencv.core.Core.absdiff;
import static org.opencv.imgcodecs.Imgcodecs.imread;
import static org.opencv.imgproc.Imgproc.CHAIN_APPROX_SIMPLE;
import static org.opencv.imgproc.Imgproc.MORPH_ELLIPSE;
import static org.opencv.imgproc.Imgproc.RETR_EXTERNAL;
import static org.opencv.imgproc.Imgproc.cvtColor;
import static org.opencv.imgproc.Imgproc.dilate;
import static org.opencv.imgproc.Imgproc.erode;
import static org.opencv.imgproc.Imgproc.findContours;
import static org.opencv.imgproc.Imgproc.getStructuringElement;
import static org.opencv.imgproc.Imgproc.threshold;

public final class MotionCrop {

  private static final Logger LOG = LogManager.getLogger(MotionCrop.class);

  private MotionCrop() {
    // nop
  }

  public static void cropToMotion(@Nonnull String inputFolder, @Nonnull String outputFolder) {
    emptyFolder(outputFolder);

    List<Path> inputFiles = FileUtil.getFiles(inputFolder, "**.jpg");
    Mat background = MatUtil.background(inputFiles.get(0).toString());
    Map<Integer, MarginBox> bboxes = new HashMap<>();

    for (int i = 0; i < inputFiles.size(); i++) {
      Path inputFile = inputFiles.get(i);
      Mat img = imread(inputFile.toString());
      Optional<MarginBox> boundingBox = findBoundingBox(img, background, .9);

      if (!boundingBox.isPresent()) {
        LOG.info("Found little to no motion on {}", inputFile);
        continue;
      }

      LOG.info("Found motion in {}", inputFile);

      // save to disk
      bboxes.put(i, boundingBox.get());
    }

    // get median bounding box
    MarginBox medianBox = new MarginBox();
    medianBox.setTop(getMedian(bboxes.values(), MarginBox::getTop));
    medianBox.setBottom(getMedian(bboxes.values(), MarginBox::getBottom));
    medianBox.setLeft(getMedian(bboxes.values(), MarginBox::getLeft));
    medianBox.setRight(getMedian(bboxes.values(), MarginBox::getRight));

    for (int i = 0; i < inputFiles.size(); i++) {

      if (!bboxes.containsKey(i)) {
        // if the key is not present, we found no motion in this file
        continue;
      }

      Path inputFile = inputFiles.get(i);
      Mat img = imread(inputFile.toString());
      img = crop(img, medianBox);

      saveMat(outputFolder, img, i);
    }
  }

  private static int getMedian(@Nonnull Collection<MarginBox> boxes, @Nonnull ToIntFunction<MarginBox> mapper) {
    Integer[] numArray = boxes.stream()
      .mapToInt(mapper)
      .boxed()
      .toArray(Integer[]::new);

    return median(numArray);
  }

  @Nonnull
  static Optional<MarginBox> findBoundingBox(@Nonnull Mat source, @Nonnull Mat background, double minWidthFactor) {
    Mat dst = source.clone();
    Mat gray = new Mat();
    cvtColor(dst, gray, Imgproc.COLOR_BGR2GRAY);

    Mat diff = new Mat();
    Mat t = new Mat();

    // compute absolute diff between current frame and first frame
    absdiff(background, gray, diff);
    threshold(diff, t, 40.0, 255.0, Imgproc.THRESH_BINARY);

    // erode to get rid of small dots
    int erodeSize = 10;
    Mat erodeElement = getStructuringElement(MORPH_ELLIPSE,
      new Size(2 * erodeSize + 1, 2 * erodeSize + 1),
      new Point(erodeSize, erodeSize));
    erode(t, t, erodeElement);

    // dilate the threshold image to fill in holes
    int dilateSize = 50;
    Mat dilateElement = getStructuringElement(MORPH_ELLIPSE,
      new Size(2 * dilateSize + 1, 2 * dilateSize + 1),
      new Point(dilateSize, dilateSize));
    dilate(t, t, dilateElement); // TODO this seems to be hogging the CPU hard, is there a way around this?

    // find contours
    List<MatOfPoint> contours = new ArrayList<>();
    Mat hierarchy = new Mat();
    findContours(t, contours, hierarchy, RETR_EXTERNAL, CHAIN_APPROX_SIMPLE);

    if (contours.isEmpty()) {
      // no contours, so we purge
      return Optional.empty();
    }

    MatOfPoint largestContour = contours.get(0);

    // find bounding box of contour
    MarginBox bbox = new MarginBox();
    bbox.setTop(streamCoordinates(largestContour, 1).min().orElse(0));
    bbox.setBottom(streamCoordinates(largestContour, 1).max().orElse(dst.height()));
    bbox.setLeft(streamCoordinates(largestContour, 0).min().orElse(0));
    bbox.setRight(streamCoordinates(largestContour, 0).max().orElse(dst.width()));

    if (bbox.getRight() - bbox.getLeft() < (dst.width() * minWidthFactor)) {
      // => the motion area covers not almost the whole width
      // this can be one of the following reasons
      // - it's the start of the train
      // - it's the end of the train
      // - a bird flew over the empty background
      return Optional.empty();
    }

    return Optional.of(bbox);
  }

  /**
   * Crops the {@link Mat} to the bounding box.
   */
  @Nonnull
  private static Mat crop(@Nonnull Mat mat, @Nonnull MarginBox bbox) {
    Rect roi = new Rect(bbox.getLeft(),
      bbox.getTop(),
      bbox.getRight() - bbox.getLeft(),
      bbox.getBottom() - bbox.getTop());

    return new Mat(mat, roi);
  }

  /**
   * Returns a stream of contour points with the given index.
   */
  @Nonnull
  private static IntStream streamCoordinates(@Nonnull MatOfPoint contour, int index) {
    return IntStream.rangeClosed(0, contour.rows() - 1)
      .map(i -> (int) contour.get(i, 0)[index]);
  }
}
