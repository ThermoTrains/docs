package ch.sebastianhaeni.thermotrains.util;

import org.opencv.core.Mat;
import org.opencv.core.Rect;

import javax.annotation.Nonnull;

/**
 * Util functions to handle {@link Mat} objects.
 */
public final class MatUtil {
  private MatUtil() {
    // nop
  }

  /**
   * Crops the given {@link Mat} by the given margins.
   */
  @Nonnull
  public static Mat crop(@Nonnull Mat mat, int topMargin, int rightMargin, int bottomMargin, int leftMargin) {
    Rect roi = new Rect(
      leftMargin,
      topMargin,
      mat.width() - rightMargin - leftMargin,
      mat.height() - bottomMargin - topMargin);

    return new Mat(mat, roi);
  }
}
