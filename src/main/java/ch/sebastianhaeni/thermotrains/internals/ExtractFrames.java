package ch.sebastianhaeni.thermotrains.internals;

import java.util.function.Function;
import java.util.function.Predicate;

import javax.annotation.Nonnull;

import ch.sebastianhaeni.thermotrains.util.Direction;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.opencv.core.Mat;
import org.opencv.videoio.VideoCapture;
import org.opencv.videoio.Videoio;

import static ch.sebastianhaeni.thermotrains.util.FileUtil.emptyFolder;
import static ch.sebastianhaeni.thermotrains.util.FileUtil.saveMat;
import static org.opencv.core.Core.flip;

public final class ExtractFrames {

  private static final Logger LOG = LogManager.getLogger(ExtractFrames.class);

  private ExtractFrames() {
    // nop
  }

  public static void extractFrames(
    @Nonnull String inputVideoFilename,
    @Nonnull String outputFolder) {

    extractFrames(inputVideoFilename, outputFolder, Direction.FORWARD, 50);
  }

  static void extractFrames(
    @Nonnull String inputVideoFilename,
    @Nonnull String outputFolder,
    @Nonnull Direction direction,
    int framesToExtract) {

    extractFrames(inputVideoFilename, outputFolder, direction, framesToExtract, 1);
  }

  /**
   * Extract n frames in a direction from an input file. The lengthFactor gets multiplied with the video length and only
   * the frames from start this amount of frames will be considered.
   */
  static void extractFrames(
    @Nonnull String inputVideoFilename,
    @Nonnull String outputFolder,
    @Nonnull Direction direction,
    int framesToExtract,
    double lengthFactor) {

    emptyFolder(outputFolder);

    VideoCapture capture = new VideoCapture();

    if (!capture.open(inputVideoFilename)) {
      throw new IllegalStateException("Cannot open the video file");
    }

    boolean isForward = direction == Direction.FORWARD;
    int frameCount = (int) (capture.get(Videoio.CAP_PROP_FRAME_COUNT) * lengthFactor);
    int framesBetween = frameCount / framesToExtract;
    int frameCounter = 0;

    Predicate<Integer> termination = isForward ?
      i -> i < frameCount :
      i -> i > 0;
    Function<Integer, Integer> increment = isForward ?
      i -> i + 1 :
      i -> i - 1;

    int i = isForward ? 0 : frameCount;

    while (termination.test(i) && frameCounter < framesToExtract) {
      i = increment.apply(i);

      Mat frame = new Mat();
      boolean success = capture.read(frame);
      if (!success) {
        LOG.warn("Cannot read frame {}", i);
        continue;
      }

      if (i == 0 || i % framesBetween != 0) {
        // do not extract every frame, but once in a while so we have a fixed number of frames
        // not correlated to the frame count
        continue;
      }

      if (!isForward) {
        flip(frame, frame, 1);
      }

      saveMat(outputFolder, frame, ++frameCounter);
    }
  }
}
