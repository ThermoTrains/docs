package ch.sebastianhaeni.thermotrains.util;

import org.opencv.core.Mat;
import org.opencv.core.Rect;
import org.opencv.core.Size;
import org.opencv.imgproc.Imgproc;

import javax.annotation.Nonnull;

import static org.opencv.imgcodecs.Imgcodecs.imread;
import static org.opencv.imgproc.Imgproc.blur;
import static org.opencv.imgproc.Imgproc.cvtColor;

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

  /**
   * Reads the file as an image and prepares it to be used as a background.
   */
  @Nonnull
  public static Mat background(@Nonnull String file){
    Mat background = imread(file);
    cvtColor(background, background, Imgproc.COLOR_BGR2GRAY);
    int kernelSize = 3 * 2;
    blur(background, background, new Size(kernelSize, kernelSize));

    return background;
  }
}
