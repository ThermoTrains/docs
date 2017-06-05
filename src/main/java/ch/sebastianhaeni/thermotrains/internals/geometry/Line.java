package ch.sebastianhaeni.thermotrains.internals.geometry;

import org.opencv.core.Point;

import javax.annotation.Nonnull;

public class Line {

  private Point p1;
  private Point p2;

  public Line(Point p1, Point p2) {
    // add the point that is most left on the x axis as p1
    if (p1.x < p2.x) {
      this.p1 = p1;
      this.p2 = p2;

      return;
    }

    this.p1 = p2;
    this.p2 = p1;
  }

  @Nonnull
  public Line translate(int x, int y) {
    p1.x += x;
    p1.y += y;
    p2.x += x;
    p2.y += y;

    return this;
  }

  /**
   * Expands the line within a boundary using the traditional equation.
   * {@code y = ax + b}
   */
  @Nonnull
  public Line expand(int xLow, int xHigh, int yLow, int yHigh) {
    double a = getSlope();

    // b = y - ax
    double b = p1.y - (a * p1.x);

    p1.x = xLow;
    p1.y = a * xLow + b;

    p2.x = xHigh;
    p2.y = a * xHigh + b;

    // keep inside y boundary
    if (p1.y < yLow) {
      p1.x = (yLow - b) / a;
      p1.y = yLow;
    }
    if (p1.y > yHigh) {
      p1.x = (yHigh - b) / a;
      p1.y = yHigh;
    }

    if (p2.y > yHigh) {
      p2.x = (yHigh - b) / a;
      p2.y = yHigh;
    }
    if (p2.y < yLow) {
      p2.x = (yLow - b) / a;
      p2.y = yLow;
    }

    return this;
  }

  private double getSlope() {
    double absoluteInclination = p2.y - p1.y;

    return absoluteInclination / (p2.x - p1.x);
  }

  public Point getP1() {
    return p1;
  }

  public void setP1(Point p1) {
    this.p1 = p1;
  }

  public Point getP2() {
    return p2;
  }

  public void setP2(Point p2) {
    this.p2 = p2;
  }
}
