package ch.sebastianhaeni.thermotrains.internals.geometry;

/**
 * Bounding box with top, bottom, left and right.
 */
public class MarginBox {
  private int top;
  private int bottom;
  private int left;
  private int right;

  public int getTop() {
    return top;
  }

  public void setTop(int top) {
    this.top = top;
  }

  public int getBottom() {
    return bottom;
  }

  public void setBottom(int bottom) {
    this.bottom = bottom;
  }

  public int getLeft() {
    return left;
  }

  public void setLeft(int left) {
    this.left = left;
  }

  public int getRight() {
    return right;
  }

  public void setRight(int right) {
    this.right = right;
  }
}
