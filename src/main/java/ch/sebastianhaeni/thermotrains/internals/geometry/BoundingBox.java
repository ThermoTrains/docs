package ch.sebastianhaeni.thermotrains.internals.geometry;

import org.opencv.core.CvType;
import org.opencv.core.Mat;
import org.opencv.core.Point;
import org.opencv.core.Size;

import javax.annotation.Nonnull;

/**
 * Bounding box with top, bottom, left and right.
 */
public class BoundingBox {
  @Nonnull
  private Point topLeft;
  @Nonnull
  private Point topRight;
  @Nonnull
  private Point bottomRight;
  @Nonnull
  private Point bottomLeft;

  public BoundingBox(@Nonnull Point topLeft,
                     @Nonnull Point topRight,
                     @Nonnull Point bottomRight,
                     @Nonnull Point bottomLeft) {

    this.topLeft = topLeft;
    this.topRight = topRight;
    this.bottomRight = bottomRight;
    this.bottomLeft = bottomLeft;
  }

  public BoundingBox(@Nonnull Line line1, @Nonnull Line line2) {
    if (line1.getP1().y < line2.getP1().y) {
      // add line with p1 furthest to the right as top left
      this.topLeft = line1.getP1();
      this.topRight = line1.getP2();
      this.bottomLeft = line2.getP1();
      this.bottomRight = line2.getP2();

      return;
    }

    this.topLeft = line2.getP1();
    this.topRight = line2.getP2();
    this.bottomLeft = line1.getP1();
    this.bottomRight = line1.getP2();
  }

  @Nonnull
  public Mat getMat() {
    Mat mat = new Mat(new Size(4, 1), CvType.CV_32FC2);

    mat.put(0, 0,
      topLeft.x, topLeft.y,
      topRight.x, topRight.y,
      bottomRight.x, bottomRight.y,
      bottomLeft.x, bottomLeft.y);

    return mat;
  }

  @Nonnull
  public Point getTopLeft() {
    return topLeft;
  }

  public void setTopLeft(@Nonnull Point topLeft) {
    this.topLeft = topLeft;
  }

  @Nonnull
  public Point getTopRight() {
    return topRight;
  }

  public void setTopRight(@Nonnull Point topRight) {
    this.topRight = topRight;
  }

  @Nonnull
  public Point getBottomRight() {
    return bottomRight;
  }

  public void setBottomRight(@Nonnull Point bottomRight) {
    this.bottomRight = bottomRight;
  }

  @Nonnull
  public Point getBottomLeft() {
    return bottomLeft;
  }

  public void setBottomLeft(@Nonnull Point bottomLeft) {
    this.bottomLeft = bottomLeft;
  }
}
