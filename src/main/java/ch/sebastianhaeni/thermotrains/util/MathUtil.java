package ch.sebastianhaeni.thermotrains.util;

import java.util.Arrays;

/**
 * Util functions for common math problems that are not in a library.
 */
public final class MathUtil {

  public static final double RAD2DEG = 180.0f / Math.PI;

  private MathUtil() {
    // nop
  }

  public static int median(int[] numArray) {
    int[] clone = numArray.clone();
    Arrays.sort(clone);

    if (clone.length % 2 == 0) {
      return (clone[clone.length / 2] + clone[clone.length / 2 - 1]) / 2;
    }

    return clone[clone.length / 2];
  }
}
