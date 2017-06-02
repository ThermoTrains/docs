package ch.sebastianhaeni.thermotrains.wrapper;

import org.opencv.core.Mat;

import java.util.Collection;

public final class Stitching {

  static {
    System.loadLibrary("stitching");
  }

  private Stitching() {
  }

  private static native int stitch(long[] images, long pano);

  public static Status stitch(Mat panorama, Collection<Mat> images) {

    long[] addresses = images.stream()
      .mapToLong(Mat::getNativeObjAddr)
      .toArray();

    int status = stitch(addresses, panorama.getNativeObjAddr());

    return Status.values()[status];
  }

  public enum Status {
    OK,
    ERR_NEED_MORE_IMGS,
    ERR_HOMOGRAPHY_EST_FAIL,
    ERR_CAMERA_PARAMS_ADJUST_FAIL
  }
}
