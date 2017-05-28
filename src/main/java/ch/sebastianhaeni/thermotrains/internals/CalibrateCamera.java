package ch.sebastianhaeni.thermotrains.internals;

import ch.sebastianhaeni.thermotrains.serialization.Calibration;
import ch.sebastianhaeni.thermotrains.serialization.MatSerialization;
import ch.sebastianhaeni.thermotrains.util.FileUtil;
import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import org.opencv.core.*;

import java.io.File;
import java.io.FileNotFoundException;
import java.io.PrintWriter;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.Collection;
import java.util.List;

import static ch.sebastianhaeni.thermotrains.util.FileUtil.getFile;
import static org.opencv.calib3d.Calib3d.*;
import static org.opencv.core.CvType.CV_64F;
import static org.opencv.imgcodecs.Imgcodecs.imread;
import static org.opencv.imgcodecs.Imgcodecs.imwrite;
import static org.opencv.imgproc.Imgproc.*;

public class CalibrateCamera {

  public static void performCheckerboardCalibration(double squareSize, String inputFolder, String outputFolder)
    throws FileNotFoundException {

    Collection<Path> inputFiles = FileUtil.getFiles(inputFolder, "**.jpg");

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

    for (Path inputFile : inputFiles) {
      Mat img = imread(inputFile.toString());
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
        System.out.println("Could not find checkerboard pattern on " + inputFile.getFileName());
        continue;
      } else {
        System.out.println("Found checkerboard on " + inputFile.getFileName());
      }

      int type = TermCriteria.EPS + TermCriteria.MAX_ITER;
      TermCriteria criteria = new TermCriteria(type, 30, 0.1);
      Size winSize = new Size(11, 11);
      Size zeroZone = new Size(-1, -1);
      cornerSubPix(gray, corners, winSize, zeroZone, criteria);

      drawChessboardCorners(img, patternSize, corners, true);
      File file = new File(outputFolder, "checkerboard-" + inputFile.getFileName());
      imwrite(file.getAbsolutePath(), img);

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
    double RAD2DEG = 180.0f / Math.PI;
    return fovRad * RAD2DEG;
  }
}
