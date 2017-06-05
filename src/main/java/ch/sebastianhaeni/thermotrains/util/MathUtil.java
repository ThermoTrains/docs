package ch.sebastianhaeni.thermotrains.util;

import javax.annotation.Nonnull;
import java.util.Arrays;
import java.util.function.BiFunction;

/**
 * Util functions for common math problems that are not in a library.
 */
public final class MathUtil {

  private MathUtil() {
    // nop
  }

  public interface Constants {
    double RAD2DEG = 180.0f / Math.PI;
  }

  /**
   * Gets the median of the numerical array.
   */
  public static int median(@Nonnull Integer[] numArray) {
    return median(numArray, (a, b) -> (a + b) / 2);
  }

  /**
   * Gets the median of the numerical array.
   */
  public static double median(@Nonnull Double[] numArray) {
    return median(numArray, (a, b) -> (a + b) / 2);
  }

  private static <T> T median(@Nonnull T[] numArray, @Nonnull BiFunction<T, T, T> mean) {
    T[] clone = numArray.clone();
    Arrays.sort(clone);

    if (clone.length % 2 == 0) {
      return mean.apply(clone[clone.length / 2], clone[clone.length / 2 - 1]);
    }

    return clone[clone.length / 2];
  }
}
