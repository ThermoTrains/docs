package ch.sebastianhaeni.thermotrains.internals;

import ch.sebastianhaeni.thermotrains.serialization.Calibration;
import com.google.gson.Gson;
import org.opencv.core.Mat;
import org.opencv.core.Rect;

import java.io.File;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Paths;

import static org.opencv.calib3d.Calib3d.getOptimalNewCameraMatrix;
import static org.opencv.imgcodecs.Imgcodecs.imread;
import static org.opencv.imgcodecs.Imgcodecs.imwrite;
import static org.opencv.imgproc.Imgproc.undistort;

public class Undistort {
  public static void undistortImages(String calibrationJsonFilename, String pathToDistortedImages, String pathToUndistortedImages) {
    Gson gson = new Gson();
    String fileString;

    try {
      fileString = new String(Files.readAllBytes(Paths.get(calibrationJsonFilename)), StandardCharsets.UTF_8);
    } catch (IOException e) {
      e.printStackTrace();
      return;
    }

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

    int i = 1;
    File file = new File(pathToDistortedImages, "Frame " + i + ".jpg");
    Mat img = imread(file.getAbsolutePath());
    Mat dst = new Mat();
    undistort(img, dst, calibration.getCameraMatrix(), calibration.getDistCoeffs(), optimalNewCameraMatrix);

    // crop based on ROI
    int x = roi.x;
    int y = roi.y;
    int width = roi.width;
    int height = roi.height;
    dst = dst.adjustROI(y, y + height, x, x + width);

    // save to disk
    file = new File(pathToUndistortedImages, "undistorted " + i + ".jpg");
    imwrite(file.getAbsolutePath(), dst);
    System.out.println("Written " + file);
  }
}
