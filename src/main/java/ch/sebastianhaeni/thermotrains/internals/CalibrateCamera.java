package ch.sebastianhaeni.thermotrains.internals;

import ch.sebastianhaeni.thermotrains.models.Calibration;
import com.google.gson.Gson;
import org.opencv.core.*;

import java.io.File;
import java.io.FileNotFoundException;
import java.io.PrintWriter;
import java.nio.file.FileSystems;
import java.nio.file.Path;
import java.nio.file.PathMatcher;
import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.LinkedBlockingDeque;

import static org.opencv.calib3d.Calib3d.*;
import static org.opencv.core.CvType.CV_64F;
import static org.opencv.imgcodecs.Imgcodecs.imread;
import static org.opencv.imgcodecs.Imgcodecs.imwrite;
import static org.opencv.imgproc.Imgproc.*;

public class CalibrateCamera {

  public static void performCheckerboardCalibration(String inputCheckerboardFrameFolder, double squareSize) {
    LinkedBlockingDeque<Path> inputFiles = new LinkedBlockingDeque<>();
    PathMatcher matcher = FileSystems.getDefault().getPathMatcher("glob:**.jpg");
    File folder = new File(inputCheckerboardFrameFolder);

    for (File file : folder.listFiles()) {
      if (matcher.matches(file.toPath())) {
        inputFiles.add(file.toPath());
      }
    }

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
      imwrite("checkerboard-" + inputFile.getFileName(), img);

      imagePoints.add(corners);
      objectPoints.add(objectPoint);
    }

    Mat cameraMatrix = new Mat(3, 3, CV_64F);
    Mat distCoeffs = new Mat(8, 1, CV_64F);
    List<Mat> rvecs = new ArrayList<>();
    List<Mat> tvecs = new ArrayList<>();
    double rms = calibrateCamera(objectPoints, imagePoints, imageSize, cameraMatrix, distCoeffs, rvecs, tvecs);

    Calibration calibration = new Calibration(cameraMatrix, distCoeffs, imageSize, rvecs, tvecs);

    Gson gson = new Gson();
    String serializedJson = gson.toJson(calibration);

    try {
      PrintWriter out = new PrintWriter("calibration.json");
      out.print(serializedJson);
      out.close();
    } catch (FileNotFoundException e) {
      e.printStackTrace();
    }
  }
}
