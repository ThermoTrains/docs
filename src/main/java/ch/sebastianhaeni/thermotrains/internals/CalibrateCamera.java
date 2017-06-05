package ch.sebastianhaeni.thermotrains.internals;

import ch.sebastianhaeni.thermotrains.serialization.Calibration;
import ch.sebastianhaeni.thermotrains.serialization.MatSerialization;
import ch.sebastianhaeni.thermotrains.util.FileUtil;
import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.opencv.core.*;

import javax.annotation.Nonnull;
import java.io.File;
import java.io.FileNotFoundException;
import java.io.PrintWriter;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;

import static ch.sebastianhaeni.thermotrains.util.FileUtil.*;
import static ch.sebastianhaeni.thermotrains.util.MathUtil.Constants.RAD2DEG;
import static org.opencv.calib3d.Calib3d.*;
import static org.opencv.core.CvType.CV_64F;
import static org.opencv.imgcodecs.Imgcodecs.imread;
import static org.opencv.imgproc.Imgproc.*;

public final class CalibrateCamera {

  private static final Logger LOG = LogManager.getLogger(CalibrateCamera.class);
  private static final Size PATTERN_SIZE = new Size(8, 5);

  private CalibrateCamera() {
    // nop
  }

  public static void performCheckerboardCalibration(
    double squareSize,
    @Nonnull String inputFolder,
    @Nonnull String outputFolder)
    throws FileNotFoundException {

    emptyFolder(outputFolder);

    List<Path> inputFiles = FileUtil.getFiles(inputFolder, "**.jpg");

    // interior number of corners
    List<Point3> objectPointList = new ArrayList<>();

    for (double y = PATTERN_SIZE.height - 1; y >= 0; --y) {
      for (double x = 0; x < PATTERN_SIZE.width; ++x) {
        Point3 point3 = new Point3(x * squareSize, y * squareSize, 0);
        objectPointList.add(point3);
      }
    }

    MatOfPoint3f objectPoint = new MatOfPoint3f();
    objectPoint.fromList(objectPointList);

    List<Mat> imagePoints = new ArrayList<>();
    List<Mat> objectPoints = new ArrayList<>();

    Size imageSize = imread(inputFiles.get(0).toString()).size();

    for (int i = 0; i < inputFiles.size(); i++) {
      Mat img = imread(inputFiles.get(i).toString());
      Mat gray = new Mat();
      cvtColor(img, gray, COLOR_BGR2GRAY);

      // this will be filled by the detected corners
      MatOfPoint2f corners = new MatOfPoint2f();
      int flags = CALIB_CB_ADAPTIVE_THRESH + CALIB_CB_NORMALIZE_IMAGE;
      boolean patternFound = findChessboardCorners(gray, PATTERN_SIZE, corners, flags);

      if (!patternFound) {
        LOG.warn("Could not find checkerboard pattern on image {}", i);
        continue;
      }

      int type = TermCriteria.EPS + TermCriteria.MAX_ITER;
      TermCriteria criteria = new TermCriteria(type, 30, 0.1);
      Size winSize = new Size(11, 11);
      Size zeroZone = new Size(-1, -1);
      cornerSubPix(gray, corners, winSize, zeroZone, criteria);

      drawChessboardCorners(img, PATTERN_SIZE, corners, true);
      saveMat(outputFolder, img, i);

      imagePoints.add(corners);
      objectPoints.add(objectPoint);
    }

    Mat cameraMatrix = new Mat(3, 3, CV_64F);
    Mat distCoeffs = new Mat(8, 1, CV_64F);
    List<Mat> rvecs = new ArrayList<>();
    List<Mat> tvecs = new ArrayList<>();
    double rms = calibrateCamera(objectPoints, imagePoints, imageSize, cameraMatrix, distCoeffs, rvecs, tvecs);

    LOG.info("Calibration RMS: {}", rms);
    LOG.info("Vertical FOV: {}", calcFov(cameraMatrix));

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

  private static double calcFov(@Nonnull Mat cameraMatrix) {
    double fy = cameraMatrix.get(1, 1)[0];
    double cy = cameraMatrix.get(1, 2)[0];
    double fovRad = 2 * Math.atan2(cy, fy);

    return fovRad * RAD2DEG;
  }
}
