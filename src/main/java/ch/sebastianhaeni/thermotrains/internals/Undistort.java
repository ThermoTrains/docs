package ch.sebastianhaeni.thermotrains.internals;

import ch.sebastianhaeni.thermotrains.models.Calibration;
import com.google.gson.Gson;
import org.opencv.core.Mat;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Paths;

import static org.opencv.calib3d.Calib3d.getOptimalNewCameraMatrix;
import static org.opencv.imgcodecs.Imgcodecs.imread;
import static org.opencv.imgcodecs.Imgcodecs.imwrite;
import static org.opencv.imgproc.Imgproc.undistort;

public class Undistort {
  public static void undistortImages(String calibrationJsonFilename, String pathToDistortedImages) throws IOException {
    Gson gson = new Gson();
    String fileString = new String(Files.readAllBytes(Paths.get(calibrationJsonFilename)), StandardCharsets.UTF_8);
    Calibration calibration = gson.fromJson(fileString, Calibration.class);

    Mat optimalNewCameraMatrix = getOptimalNewCameraMatrix(calibration.getCameraMatrix(), calibration.getDistCoeffs(), calibration.getImageSize(), 1);

    Mat img = imread(pathToDistortedImages + "/Frame 1.jpg");
    Mat dst = new Mat();
    undistort(img, dst, calibration.getCameraMatrix(), calibration.getDistCoeffs(), optimalNewCameraMatrix);
    imwrite("undistorted.jpg", dst);
  }
}
