package ch.sebastianhaeni.thermotrains.util;

import org.opencv.core.Mat;
import org.opencv.core.Rect;

public class MatUtil {

  public static Mat crop(Mat mat, int topMargin, int rightMargin, int bottomMargin, int leftMargin) {
    Rect roi = new Rect(
      leftMargin,
      topMargin,
      mat.width() - rightMargin - leftMargin,
      mat.height() - bottomMargin - topMargin);

    return new Mat(mat, roi);
  }
}
