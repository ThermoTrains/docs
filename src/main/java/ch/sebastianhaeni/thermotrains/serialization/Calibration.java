package ch.sebastianhaeni.thermotrains.serialization;

import org.opencv.core.Mat;
import org.opencv.core.Size;

import java.util.List;

/**
 * This class represents the camera calibration settings used to undistort images. To serialize it with GSON, you can
 * use {@link MatSerialization}.
 */
public class Calibration {
  private final Mat cameraMatrix;
  private final Mat distCoeffs;
  private final Size imageSize;
  private final List<Mat> rvecs;
  private final List<Mat> tvecs;

  public Calibration(Mat cameraMatrix, Mat distCoeffs, Size imageSize, List<Mat> rvecs, List<Mat> tvecs) {
    this.cameraMatrix = cameraMatrix;
    this.distCoeffs = distCoeffs;
    this.imageSize = imageSize;
    this.rvecs = rvecs;
    this.tvecs = tvecs;
  }

  public Mat getCameraMatrix() {
    return cameraMatrix;
  }

  public Mat getDistCoeffs() {
    return distCoeffs;
  }

  public Size getImageSize() {
    return imageSize;
  }

  public List<Mat> getRvecs() {
    return rvecs;
  }

  public List<Mat> getTvecs() {
    return tvecs;
  }
}
