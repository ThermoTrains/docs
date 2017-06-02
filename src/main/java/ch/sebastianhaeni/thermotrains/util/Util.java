package ch.sebastianhaeni.thermotrains.util;
import java.util.Arrays;

public final class Util {

  private Util() {
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
