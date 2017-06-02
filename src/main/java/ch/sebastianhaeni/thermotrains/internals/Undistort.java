package ch.sebastianhaeni.thermotrains.internals;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.List;

import org.opencv.core.Mat;
import org.opencv.core.Rect;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;

import ch.sebastianhaeni.thermotrains.serialization.Calibration;
import ch.sebastianhaeni.thermotrains.serialization.MatSerialization;
import ch.sebastianhaeni.thermotrains.util.FileUtil;

import static ch.sebastianhaeni.thermotrains.util.FileUtil.saveMat;
import static org.opencv.calib3d.Calib3d.getOptimalNewCameraMatrix;
import static org.opencv.imgcodecs.Imgcodecs.imread;
import static org.opencv.imgproc.Imgproc.undistort;

public final class Undistort {

  private Undistort() {
  }

  public static void undistortImages(String calibrationJsonFilename, String inputFolder, String outputFolder)
    throws IOException {

    Gson gson = new GsonBuilder()
      .registerTypeAdapter(Mat.class, new MatSerialization())
      .create();

    String fileString = new String(Files.readAllBytes(Paths.get(calibrationJsonFilename)), StandardCharsets.UTF_8);
    Calibration calibration = gson.fromJson(fileString, Calibration.class);

    Rect roi = new Rect();

    Mat optimalNewCameraMatrix = getOptimalNewCameraMatrix(
      calibration.getCameraMatrix(),
      calibration.getDistCoeffs(),
      calibration.getImageSize(),
      1,
      calibration.getImageSize(),
      roi,
      false);

    int i = 0;
    List<Path> inputFiles = FileUtil.getFiles(inputFolder, "**.jpg");

    for (Path inputFile : inputFiles) {
      Mat img = imread(inputFile.toString());
      Mat dst = new Mat();
      undistort(img, dst, calibration.getCameraMatrix(), calibration.getDistCoeffs(), optimalNewCameraMatrix);

      // crop based on ROI
      int x = roi.x;
      int y = roi.y;
      int width = roi.width;
      int height = roi.height;

      Mat cropped = new Mat(dst, new Rect(x, y, width, height));

      // save to disk
      saveMat(outputFolder, cropped, ++i);
    }
  }
}