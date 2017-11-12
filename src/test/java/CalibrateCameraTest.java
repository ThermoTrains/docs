import java.util.ArrayList;
import java.util.List;

import ch.sebastianhaeni.thermotrains.serialization.Calibration;
import ch.sebastianhaeni.thermotrains.serialization.MatSerialization;
import ch.sebastianhaeni.thermotrains.util.FileUtil;
import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import org.junit.Assert;
import org.junit.Test;
import org.opencv.core.Core;
import org.opencv.core.Mat;
import org.opencv.core.Rect;
import org.opencv.core.Size;

import static org.opencv.calib3d.Calib3d.getOptimalNewCameraMatrix;
import static org.opencv.core.CvType.CV_64F;
import static org.opencv.imgcodecs.Imgcodecs.imread;
import static org.opencv.imgproc.Imgproc.undistort;

public class CalibrateCameraTest {
  static {
    // load OpenCV native library
    System.loadLibrary(Core.NATIVE_LIBRARY_NAME);
  }

  @Test
  public void testFlir() {
    Mat cameraMatrix = new Mat(3, 3, CV_64F);
    Mat distCoeffs = new Mat(4, 1, CV_64F);
    Size imageSize = new Size(640, 512);

    List<Mat> rvecs = new ArrayList<>();
    List<Mat> tvecs = new ArrayList<>();

    // Data from Matlab - careful, matrix outputted from Matlab differs than the one from OpenCV
    cameraMatrix.put(0, 0, 805.748867816668);
    cameraMatrix.put(0, 1, 0);
    cameraMatrix.put(0, 2, 325.287947377588);

    cameraMatrix.put(1, 0, 0);
    cameraMatrix.put(1, 1, 810.561301720046);
    cameraMatrix.put(1, 2, 241.948199843830);

    cameraMatrix.put(2, 0, 0);
    cameraMatrix.put(2, 1, 0);
    cameraMatrix.put(2, 2, 1);

    // radial distortion coefficients
    distCoeffs.put(0, 0, -0.0240507365500920);
    distCoeffs.put(1, 0, 0.618695256531730);

    // tangential distortion
    distCoeffs.put(2, 0, -0.0145598660007376);
    distCoeffs.put(3, 0, -0.00291508175506785);

    Calibration calibration = new Calibration(cameraMatrix, distCoeffs, imageSize, rvecs, tvecs);

    Gson gson = new GsonBuilder()
      .registerTypeAdapter(Mat.class, new MatSerialization())
      .setPrettyPrinting()
      .create();

    String serializedJson = gson.toJson(calibration);

    Assert.assertTrue(!serializedJson.isEmpty());

    Rect roi = new Rect();

    Mat optimalNewCameraMatrix = getOptimalNewCameraMatrix(
      calibration.getCameraMatrix(),
      calibration.getDistCoeffs(),
      calibration.getImageSize(),
      1,
      calibration.getImageSize(),
      roi,
      false);

    Mat img = imread("target/1-calibration/0039.jpg");
    Mat dst = new Mat();

    undistort(img, dst, calibration.getCameraMatrix(), calibration.getDistCoeffs(), optimalNewCameraMatrix);

    int x = roi.x;
    int y = roi.y;
    int width = roi.width;
    int height = roi.height;

    Mat cropped = new Mat(dst, new Rect(x, y, width, height));

    FileUtil.saveMat("target", cropped, "undistorted");
  }

  @Test
  public void testBasler() {
    Mat cameraMatrix = new Mat(3, 3, CV_64F);
    Mat distCoeffs = new Mat(4, 1, CV_64F);
    Size imageSize = new Size(1920, 1080);

    List<Mat> rvecs = new ArrayList<>();
    List<Mat> tvecs = new ArrayList<>();

    // Matlab matrix:
    //   1882.71054682266 0 0
    //   0.768370522118902 1891.25445742008 0
    //   802.219900199790 493.292386064504 1

    // Data from Matlab - careful, matrix outputted from Matlab differs than the one from OpenCV
    // See https://ch.mathworks.com/help/vision/ug/camera-calibration.html
    // And https://docs.opencv.org/2.4/modules/calib3d/doc/camera_calibration_and_3d_reconstruction.html

    cameraMatrix.put(0, 0, 1882.71054682266);
    cameraMatrix.put(0, 1, 0);
    cameraMatrix.put(0, 2, 802.219900199790);

    cameraMatrix.put(1, 0, 0);
    cameraMatrix.put(1, 1, 1891.25445742008);
    cameraMatrix.put(1, 2, 493.292386064504);

    cameraMatrix.put(2, 0, 0);
    cameraMatrix.put(2, 1, 0);
    cameraMatrix.put(2, 2, 1);

    // radial distortion coefficients
    distCoeffs.put(0, 0, -0.234851299686286);
    distCoeffs.put(1, 0, 0.105351375562889);

    // tangential distortion
    distCoeffs.put(2, 0, -0.00298279560296429);
    distCoeffs.put(3, 0, -0.00494052497457844);

    Calibration calibration = new Calibration(cameraMatrix, distCoeffs, imageSize, rvecs, tvecs);

    Gson gson = new GsonBuilder()
      .registerTypeAdapter(Mat.class, new MatSerialization())
      .setPrettyPrinting()
      .create();

    String serializedJson = gson.toJson(calibration);

    Assert.assertTrue(!serializedJson.isEmpty());

    Rect roi = new Rect();

    Mat optimalNewCameraMatrix = getOptimalNewCameraMatrix(
      calibration.getCameraMatrix(),
      calibration.getDistCoeffs(),
      calibration.getImageSize(),
      1,
      calibration.getImageSize(),
      roi,
      false);

    Mat img = imread("target/1-calibration/53.jpg");
    Mat dst = new Mat();

    undistort(img, dst, calibration.getCameraMatrix(), calibration.getDistCoeffs(), optimalNewCameraMatrix);

    int x = roi.x;
    int y = roi.y;
    int width = roi.width;
    int height = roi.height;

    Mat cropped = new Mat(dst, new Rect(x, y, width, height));

    FileUtil.saveMat("target", cropped, "undistorted");
  }
}
