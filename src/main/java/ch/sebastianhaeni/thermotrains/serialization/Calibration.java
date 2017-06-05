package ch.sebastianhaeni.thermotrains.serialization;

import org.opencv.core.Mat;
import org.opencv.core.Size;

import javax.annotation.Nonnull;
import java.util.List;

/**
 * This class represents the camera calibration settings used to undistort images. To serialize it with GSON, you can
 * use {@link MatSerialization}.
 */
public class Calibration {
  @Nonnull
  private final Mat cameraMatrix;
  @Nonnull
  private final Mat distCoeffs;
  @Nonnull
  private final Size imageSize;
  @Nonnull
  private final List<Mat> rvecs;
  @Nonnull
  private final List<Mat> tvecs;

  public Calibration(
    @Nonnull Mat cameraMatrix,
    @Nonnull Mat distCoeffs,
    @Nonnull Size imageSize,
    @Nonnull List<Mat> rvecs,
    @Nonnull List<Mat> tvecs) {

    this.cameraMatrix = cameraMatrix;
    this.distCoeffs = distCoeffs;
    this.imageSize = imageSize;
    this.rvecs = rvecs;
    this.tvecs = tvecs;
  }

  @Nonnull
  public Mat getCameraMatrix() {
    return cameraMatrix;
  }

  @Nonnull
  public Mat getDistCoeffs() {
    return distCoeffs;
  }

  @Nonnull
  public Size getImageSize() {
    return imageSize;
  }

  @Nonnull
  public List<Mat> getRvecs() {
    return rvecs;
  }

  @Nonnull
  public List<Mat> getTvecs() {
    return tvecs;
  }
}
