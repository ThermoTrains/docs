package ch.sebastianhaeni.thermotrains.internals;

import ch.sebastianhaeni.thermotrains.util.FileUtil;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.opencv.core.*;
import org.opencv.imgproc.Imgproc;

import javax.annotation.Nonnull;
import java.io.File;
import java.nio.file.Path;
import java.util.*;
import java.util.stream.IntStream;

import static ch.sebastianhaeni.thermotrains.util.FileUtil.emptyFolder;
import static ch.sebastianhaeni.thermotrains.util.FileUtil.saveMat;
import static ch.sebastianhaeni.thermotrains.util.MathUtil.median;
import static org.opencv.core.Core.absdiff;
import static org.opencv.imgcodecs.Imgcodecs.imread;
import static org.opencv.imgproc.Imgproc.*;

public final class MotionCrop {

  private static final Logger LOG = LogManager.getLogger(MotionCrop.class);

  private MotionCrop() {
    // nop
  }

  public static void cropToMotion(String inputFolder, String outputFolder) {
    emptyFolder(outputFolder);

    File fBackground = new File(inputFolder, "0001.jpg");

    Mat background = imread(fBackground.toPath().toString());
    cvtColor(background, background, Imgproc.COLOR_BGR2GRAY);
    int kernelSize = 3 * 2;
    blur(background, background, new Size(kernelSize, kernelSize));

    List<Path> inputFiles = FileUtil.getFiles(inputFolder, "**.jpg");

    Map<Integer, BBox> bboxes = new HashMap<>();

    for (int i = 0; i < inputFiles.size(); i++) {
      Path inputFile = inputFiles.get(i);
      Mat img = imread(inputFile.toString());
      Optional<BBox> boundingBox = findBoundingBox(img, background);

      if (!boundingBox.isPresent()) {
        LOG.info("Found little to no motion on {}", inputFile);
        continue;
      }

      LOG.info("Found motion in {}", inputFile);

      // save to disk
      bboxes.put(i, boundingBox.get());
    }

    // get median bounding box
    BBox medianBox = new BBox();
    medianBox.top = median(bboxes.values().stream().mapToInt(bbox -> bbox.top).toArray());
    medianBox.bottom = median(bboxes.values().stream().mapToInt(bbox -> bbox.bottom).toArray());
    medianBox.left = median(bboxes.values().stream().mapToInt(bbox -> bbox.left).toArray());
    medianBox.right = median(bboxes.values().stream().mapToInt(bbox -> bbox.right).toArray());

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

  @Nonnull
  private static Optional<BBox> findBoundingBox(@Nonnull Mat source, @Nonnull Mat background) {
    Mat dst = new Mat();
    source.copyTo(dst);
    Mat gray = new Mat();
    cvtColor(dst, gray, Imgproc.COLOR_BGR2GRAY);

    Mat diff = new Mat();
    Mat t = new Mat();

    // compute absolute diff between current frame and first frame
    absdiff(background, gray, diff);
    threshold(diff, t, 40.0, 255.0, Imgproc.THRESH_BINARY);

    // erode to get rid of small dots
    int dilationSize = 10;
    Mat erodeElement = getStructuringElement(MORPH_ELLIPSE,
      new Size(2 * dilationSize + 1, 2 * dilationSize + 1),
      new Point(dilationSize, dilationSize));
    erode(t, t, erodeElement);

    // dilate the threshold image to fill in holes
    int erodeSize = 50;
    Mat dilateElement = getStructuringElement(MORPH_ELLIPSE,
      new Size(2 * erodeSize + 1, 2 * erodeSize + 1),
      new Point(erodeSize, erodeSize));
    dilate(t, t, dilateElement);

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
    BBox bbox = new BBox();
    bbox.top = streamCoordinates(largestContour, 1).min().orElse(0);
    bbox.bottom = streamCoordinates(largestContour, 1).max().orElse(dst.height());
    bbox.left = streamCoordinates(largestContour, 0).min().orElse(0);
    bbox.right = streamCoordinates(largestContour, 0).max().orElse(dst.width());

    if (bbox.right - bbox.left < (dst.width() * .9)) {
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
  private static Mat crop(Mat mat, BBox bbox) {
    return new Mat(mat, new Rect(bbox.left, bbox.top, bbox.right - bbox.left, bbox.bottom - bbox.top));
  }

  /**
   * Returns a stream of contour points with the given index.
   */
  private static IntStream streamCoordinates(MatOfPoint contour, int index) {
    return IntStream.rangeClosed(0, contour.rows() - 1)
      .map(i -> (int) contour.get(i, 0)[index]);
  }

  /**
   * Bounding box with top, bottom, left and right.
   */
  private static class BBox {
    int top, bottom, left, right;
  }
}
