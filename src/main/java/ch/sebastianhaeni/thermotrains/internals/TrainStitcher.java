package ch.sebastianhaeni.thermotrains.internals;

import ch.sebastianhaeni.thermotrains.util.FileUtil;
import ch.sebastianhaeni.thermotrains.util.MatUtil;
import ch.sebastianhaeni.thermotrains.wrapper.Stitching;
import org.opencv.core.Mat;

import java.nio.file.Path;
import java.util.ArrayList;
import java.util.Arrays;

import static ch.sebastianhaeni.thermotrains.util.FileUtil.saveMat;
import static org.opencv.imgcodecs.Imgcodecs.imread;

public class TrainStitcher {
  private static final int verticalCrop = 100;

  public static void stitchTrain(String inputFolder, String outputFolder) {

    ArrayList<Path> inputFiles = new ArrayList<>(FileUtil.getFiles(inputFolder, "**.jpg"));

    Mat pano;
    Mat image1;
    Mat image2 = imread(inputFiles.get(0).toString());

    for (int i = 1; i < inputFiles.size(); i++) {
      System.out.printf("Stitching image %d and %d together...\n", i - 1, i);
      image1 = crop(image2);
      image2 = imread(inputFiles.get(i).toString());

      pano = stitchTogether(image1, image2);
      saveMat(outputFolder, pano, i);
    }
  }

  private static Mat crop(Mat mat) {
    int quaterWidth = mat.width() / 4;
    return MatUtil.crop(mat, verticalCrop, 0, verticalCrop, quaterWidth);
  }

  private static Mat stitchTogether(Mat image1, Mat image2) {
    Mat panorama = new Mat();
    Stitching.Status status = Stitching.stitch(panorama, Arrays.asList(image1, image2));

    if (status != Stitching.Status.OK) {
      throw new IllegalStateException(String.format("Error during stitching: %s\n", status));
    }

    return panorama;
  }
}
