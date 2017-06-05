package ch.sebastianhaeni.thermotrains.internals;

import ch.sebastianhaeni.thermotrains.internals.geometry.MarginBox;
import ch.sebastianhaeni.thermotrains.util.Direction;
import ch.sebastianhaeni.thermotrains.util.FileUtil;
import ch.sebastianhaeni.thermotrains.util.MatUtil;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.opencv.core.Mat;

import javax.annotation.Nonnull;
import java.nio.file.Path;
import java.util.List;
import java.util.Optional;

import static ch.sebastianhaeni.thermotrains.internals.MotionCrop.findBoundingBox;
import static ch.sebastianhaeni.thermotrains.util.Direction.FORWARD;
import static org.opencv.imgcodecs.Imgcodecs.imread;

public final class PrepareTrainFrames {

  private static final Logger LOG = LogManager.getLogger(PrepareTrainFrames.class);
  private static final int NUMBER_OF_FRAMES = 150;
  private static final int DIRECTION_COUNT_THRESHOLD = 3;

  private PrepareTrainFrames() {
    // nop
  }

  public static void prepare(@Nonnull String inputFile, @Nonnull String outputFolder) {

    // Goal: Figure out the direction the train is travelling

    // Our approach to this is a bit rudimentary, time consuming and very slow. We basically extract some frames from
    // the video, look for big motion regions and then decide if it's moving left or right. And we can only do this if
    // we grab a sufficient amount of frames of the video. If we grab too little, there might be a frame with only
    // background and on the next the train is fully on it.
    // This is bad in a way because we have to check a lot of empty frames where the train isn't even in the frame yet.

    // extract half the frames from half of the video
    ExtractFrames.extractFrames(inputFile, outputFolder, FORWARD, NUMBER_OF_FRAMES / 2, .5);

    LOG.info("Analyzing frames to find direction");
    Direction direction = getDirection(outputFolder);
    LOG.info("The train's direction is {}", direction);

    // extract all frames in the direction we just found
    ExtractFrames.extractFrames(inputFile, outputFolder, direction, NUMBER_OF_FRAMES);
  }

  @Nonnull
  private static Direction getDirection(@Nonnull String outputFolder) {
    List<Path> inputFiles = FileUtil.getFiles(outputFolder, "**.jpg");

    Mat background = MatUtil.background(inputFiles.get(0).toString());

    int rightCount = 0;
    int leftCount = 0;

    MarginBox last = null;

    for (Path file : inputFiles) {
      Mat img = imread(file.toString());
      Optional<MarginBox> boundingBox = findBoundingBox(img, background, .1);

      if (!boundingBox.isPresent()) {
        // no motion
        continue;
      }

      if (last == null) {
        last = boundingBox.get();
        continue;
      }

      if (boundingBox.get().getLeft() - last.getLeft() < 0) {
        rightCount++;
      } else if (boundingBox.get().getRight() - last.getRight() > 0) {
        leftCount++;
      }

      last = boundingBox.get();

      if (rightCount > DIRECTION_COUNT_THRESHOLD) {
        return FORWARD;
      }
      if (leftCount > DIRECTION_COUNT_THRESHOLD) {
        return Direction.REVERSE;
      }
    }

    throw new IllegalStateException("Could not find any motion");
  }
}
