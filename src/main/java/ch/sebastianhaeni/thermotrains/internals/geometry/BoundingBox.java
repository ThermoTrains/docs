package ch.sebastianhaeni.thermotrains.internals.geometry;

import org.opencv.core.CvType;
import org.opencv.core.Mat;
import org.opencv.core.Point;
import org.opencv.core.Size;

/**
 * Bounding box with top, bottom, left and right.
 */
public class BoundingBox {
  private Point topLeft;
  private Point topRight;
  private Point bottomRight;
  private Point bottomLeft;

  public BoundingBox(Point topLeft, Point topRight, Point bottomRight, Point bottomLeft) {
    this.topLeft = topLeft;
    this.topRight = topRight;
    this.bottomRight = bottomRight;
    this.bottomLeft = bottomLeft;
  }

  public BoundingBox(Line line1, Line line2) {
    if (line1.getP1().y < line2.getP1().y) {
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

  public Mat getMat() {
    Mat mat = new Mat(new Size(4, 1), CvType.CV_32FC2);

    mat.put(0, 0,
      topLeft.x, topLeft.y,
      topRight.x, topRight.y,
      bottomRight.x, bottomRight.y,
      bottomLeft.x, bottomLeft.y);

    return mat;
  }

  public Point getTopLeft() {
    return topLeft;
  }

  public void setTopLeft(Point topLeft) {
    this.topLeft = topLeft;
  }

  public Point getTopRight() {
    return topRight;
  }

  public void setTopRight(Point topRight) {
    this.topRight = topRight;
  }

  public Point getBottomRight() {
    return bottomRight;
  }

  public void setBottomRight(Point bottomRight) {
    this.bottomRight = bottomRight;
  }

  public Point getBottomLeft() {
    return bottomLeft;
  }

  public void setBottomLeft(Point bottomLeft) {
    this.bottomLeft = bottomLeft;
  }
}
