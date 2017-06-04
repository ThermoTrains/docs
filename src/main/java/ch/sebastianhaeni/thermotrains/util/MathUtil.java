package ch.sebastianhaeni.thermotrains.util;

import javax.annotation.Nonnull;
import java.util.Arrays;

/**
 * Util functions for common math problems that are not in a library.
 */
public final class MathUtil {

  private MathUtil() {
    // nop
  }

  public static int median(@Nonnull int[] numArray) {
    int[] clone = numArray.clone();
    Arrays.sort(clone);

    if (clone.length % 2 == 0) {
      return (clone[clone.length / 2] + clone[clone.length / 2 - 1]) / 2;
    }

    return clone[clone.length / 2];
  }

  public static final class Constants {
    public static final double RAD2DEG = 180.0f / Math.PI;
  }
}
