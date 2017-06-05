package ch.sebastianhaeni.thermotrains.internals;

import ch.sebastianhaeni.thermotrains.serialization.Calibration;
import ch.sebastianhaeni.thermotrains.serialization.MatSerialization;
import ch.sebastianhaeni.thermotrains.util.FileUtil;
import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import org.opencv.core.Mat;
import org.opencv.core.Rect;

import javax.annotation.Nonnull;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.List;

import static ch.sebastianhaeni.thermotrains.util.FileUtil.emptyFolder;
import static ch.sebastianhaeni.thermotrains.util.FileUtil.saveMat;
import static org.opencv.calib3d.Calib3d.getOptimalNewCameraMatrix;
import static org.opencv.imgcodecs.Imgcodecs.imread;
import static org.opencv.imgproc.Imgproc.undistort;

public final class Undistort {

  private Undistort() {
    // nop
  }

  public static void undistortImages(
    @Nonnull String calibrationJsonFilename,
    @Nonnull String inputFolder,
    @Nonnull String outputFolder)
    throws IOException {

    emptyFolder(outputFolder);

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

    List<Path> inputFiles = FileUtil.getFiles(inputFolder, "**.jpg");

    for (int i = 0; i < inputFiles.size(); i++) {
      Mat img = imread(inputFiles.get(i).toString());
      Mat dst = new Mat();
      undistort(img, dst, calibration.getCameraMatrix(), calibration.getDistCoeffs(), optimalNewCameraMatrix);

      // crop based on ROI
      int x = roi.x;
      int y = roi.y;
      int width = roi.width;
      int height = roi.height;

      Mat cropped = new Mat(dst, new Rect(x, y, width, height));

      // save to disk
      saveMat(outputFolder, cropped, i);
    }
  }
}
