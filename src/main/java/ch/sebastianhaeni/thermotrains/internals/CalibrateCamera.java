package ch.sebastianhaeni.thermotrains.internals;

import java.io.File;
import java.io.FileNotFoundException;
import java.io.PrintWriter;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;

import org.opencv.core.Mat;
import org.opencv.core.MatOfPoint2f;
import org.opencv.core.MatOfPoint3f;
import org.opencv.core.Point3;
import org.opencv.core.Size;
import org.opencv.core.TermCriteria;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;

import ch.sebastianhaeni.thermotrains.serialization.Calibration;
import ch.sebastianhaeni.thermotrains.serialization.MatSerialization;
import ch.sebastianhaeni.thermotrains.util.FileUtil;

import static ch.sebastianhaeni.thermotrains.util.FileUtil.getFile;
import static ch.sebastianhaeni.thermotrains.util.FileUtil.saveMat;
import static org.opencv.calib3d.Calib3d.CALIB_CB_ADAPTIVE_THRESH;
import static org.opencv.calib3d.Calib3d.CALIB_CB_NORMALIZE_IMAGE;
import static org.opencv.calib3d.Calib3d.calibrateCamera;
import static org.opencv.calib3d.Calib3d.drawChessboardCorners;
import static org.opencv.calib3d.Calib3d.findChessboardCorners;
import static org.opencv.core.CvType.CV_64F;
import static org.opencv.imgcodecs.Imgcodecs.imread;
import static org.opencv.imgproc.Imgproc.COLOR_BGR2GRAY;
import static org.opencv.imgproc.Imgproc.cornerSubPix;
import static org.opencv.imgproc.Imgproc.cvtColor;

public final class CalibrateCamera {

  private static final double RAD2DEG = 180.0f / Math.PI;

  private CalibrateCamera() {
  }

  public static void performCheckerboardCalibration(double squareSize, String inputFolder, String outputFolder)
    throws FileNotFoundException {

    List<Path> inputFiles = FileUtil.getFiles(inputFolder, "**.jpg");

    // interior number of corners
    Size patternSize = new Size(8, 5);

    List<Point3> objectPointList = new ArrayList<>();

    for (double y = patternSize.height - 1; y >= 0; --y) {
      for (double x = 0; x < patternSize.width; ++x) {
        Point3 point3 = new Point3(x * squareSize, y * squareSize, 0);
        objectPointList.add(point3);
      }
    }

    MatOfPoint3f objectPoint = new MatOfPoint3f();
    objectPoint.fromList(objectPointList);

    List<Mat> imagePoints = new ArrayList<>();
    List<Mat> objectPoints = new ArrayList<>();

    if (inputFiles.isEmpty()) {
      System.out.println("Could not find any input files");
      return;
    }

    Size imageSize = null;

    for (int i = 0; i < inputFiles.size(); i++) {
      Mat img = imread(inputFiles.get(i).toString());
      Mat gray = new Mat();
      cvtColor(img, gray, COLOR_BGR2GRAY);

      if (imageSize == null) {
        imageSize = img.size();
      }

      // this will be filled by the detected corners
      MatOfPoint2f corners = new MatOfPoint2f();
      int flags = CALIB_CB_ADAPTIVE_THRESH + CALIB_CB_NORMALIZE_IMAGE;
      boolean patternFound = findChessboardCorners(gray, patternSize, corners, flags);

      if (!patternFound) {
        System.out.println("Could not find checkerboard pattern on image " + i);
        continue;
      }

      int type = TermCriteria.EPS + TermCriteria.MAX_ITER;
      TermCriteria criteria = new TermCriteria(type, 30, 0.1);
      Size winSize = new Size(11, 11);
      Size zeroZone = new Size(-1, -1);
      cornerSubPix(gray, corners, winSize, zeroZone, criteria);

      drawChessboardCorners(img, patternSize, corners, true);
      saveMat(outputFolder, img, i);

      imagePoints.add(corners);
      objectPoints.add(objectPoint);
    }

    Mat cameraMatrix = new Mat(3, 3, CV_64F);
    Mat distCoeffs = new Mat(8, 1, CV_64F);
    List<Mat> rvecs = new ArrayList<>();
    List<Mat> tvecs = new ArrayList<>();
    double rms = calibrateCamera(objectPoints, imagePoints, imageSize, cameraMatrix, distCoeffs, rvecs, tvecs);

    System.out.println("Calibration RMS: " + rms);
    System.out.println("Vertical FOV: " + calcFov(cameraMatrix));

    Calibration calibration = new Calibration(cameraMatrix, distCoeffs, imageSize, rvecs, tvecs);

    Gson gson = new GsonBuilder()
      .registerTypeAdapter(Mat.class, new MatSerialization())
      .setPrettyPrinting()
      .create();

    String serializedJson = gson.toJson(calibration);

    File file = getFile(outputFolder, "calibration.json");
    PrintWriter out = new PrintWriter(file.getAbsoluteFile());
    out.print(serializedJson);
    out.close();
  }

  private static double calcFov(Mat cameraMatrix) {
    double fy = cameraMatrix.get(1, 1)[0];
    double cy = cameraMatrix.get(1, 2)[0];
    double fovRad = 2 * Math.atan2(cy, fy);

    return fovRad * RAD2DEG;
  }
}