package ch.sebastianhaeni.thermotrains.util;

import com.google.common.base.Charsets;
import com.google.gson.JsonObject;
import org.opencv.core.CvType;
import org.opencv.core.Mat;

import javax.annotation.Nonnull;
import java.nio.*;
import java.util.Base64;

/**
 * MathUtil functions for serialization.
 */
public final class SerializationUtil {

  private SerializationUtil() {
    // nop
  }

  /**
   * Serialize the given {@link Mat} to a JSON object.
   */
  @Nonnull
  public static JsonObject matToJson(@Nonnull Mat mat) {
    JsonObject obj = new JsonObject();

    if (!mat.isContinuous()) {
      throw new IllegalArgumentException("Mat is not continuous");
    }

    int cols = mat.cols();
    int rows = mat.rows();
    int elemSize = (int) mat.elemSize();
    int type = mat.type();

    obj.addProperty("rows", rows);
    obj.addProperty("cols", cols);
    obj.addProperty("type", type);

    // We cannot set binary data to a json object, so:
    // Encoding data byte array to Base64.
    String dataString;

    Base64.Encoder encoder = Base64.getEncoder();

    if (type == CvType.CV_32S || type == CvType.CV_32SC2 || type == CvType.CV_32SC3 || type == CvType.CV_16S) {
      int[] data = new int[cols * rows * elemSize];
      mat.get(0, 0, data);
      dataString = new String(encoder.encode(toByteArray(data)), Charsets.US_ASCII);
    } else if (type == CvType.CV_32F || type == CvType.CV_32FC2) {
      float[] data = new float[cols * rows * elemSize];
      mat.get(0, 0, data);
      dataString = new String(encoder.encode(toByteArray(data)), Charsets.US_ASCII);
    } else if (type == CvType.CV_64F || type == CvType.CV_64FC2) {
      double[] data = new double[cols * rows * elemSize];
      mat.get(0, 0, data);
      dataString = new String(encoder.encode(toByteArray(data)), Charsets.US_ASCII);
    } else if (type == CvType.CV_8U) {
      byte[] data = new byte[cols * rows * elemSize];
      mat.get(0, 0, data);
      dataString = new String(encoder.encode(data), Charsets.US_ASCII);
    } else {
      throw new UnsupportedOperationException("unknown type");
    }

    obj.addProperty("data", dataString);

    return obj;
  }

  /**
   * Deserialize the JSON object into a {@link Mat} object.
   */
  @Nonnull
  public static Mat matFromJson(@Nonnull JsonObject json) {
    int rows = json.get("rows").getAsInt();
    int cols = json.get("cols").getAsInt();
    int type = json.get("type").getAsInt();

    Mat mat = new Mat(rows, cols, type);

    String dataString = json.get("data").getAsString();
    Base64.Decoder decoder = Base64.getDecoder();

    if (type == CvType.CV_32S || type == CvType.CV_32SC2 || type == CvType.CV_32SC3 || type == CvType.CV_16S) {
      int[] data = toIntArray(decoder.decode(dataString.getBytes(Charsets.US_ASCII)));
      mat.put(0, 0, data);
    } else if (type == CvType.CV_32F || type == CvType.CV_32FC2) {
      float[] data = toFloatArray(decoder.decode(dataString.getBytes(Charsets.US_ASCII)));
      mat.put(0, 0, data);
    } else if (type == CvType.CV_64F || type == CvType.CV_64FC2) {
      double[] data = toDoubleArray(decoder.decode(dataString.getBytes(Charsets.US_ASCII)));
      mat.put(0, 0, data);
    } else if (type == CvType.CV_8U) {
      byte[] data = decoder.decode(dataString.getBytes(Charsets.US_ASCII));
      mat.put(0, 0, data);
    } else {
      throw new UnsupportedOperationException("unknown type");
    }

    return mat;
  }

  @Nonnull
  private static int[] toIntArray(@Nonnull byte[] data) {
    IntBuffer intBuf = ByteBuffer.wrap(data)
      .order(ByteOrder.BIG_ENDIAN)
      .asIntBuffer();
    int[] array = new int[intBuf.remaining()];
    intBuf.get(array);

    return array;
  }

  @Nonnull
  private static float[] toFloatArray(@Nonnull byte[] data) {
    FloatBuffer floatBuffer = ByteBuffer.wrap(data)
      .order(ByteOrder.BIG_ENDIAN)
      .asFloatBuffer();
    float[] array = new float[floatBuffer.remaining()];
    floatBuffer.get(array);

    return array;
  }

  @Nonnull
  private static double[] toDoubleArray(@Nonnull byte[] data) {
    DoubleBuffer doubleBuffer = ByteBuffer.wrap(data)
      .order(ByteOrder.BIG_ENDIAN)
      .asDoubleBuffer();
    double[] array = new double[doubleBuffer.remaining()];
    doubleBuffer.get(array);

    return array;
  }

  @Nonnull
  private static byte[] toByteArray(@Nonnull double[] data) {
    ByteBuffer byteBuffer = ByteBuffer.allocate(data.length * Double.BYTES);
    DoubleBuffer doubleBuffer = byteBuffer.asDoubleBuffer();
    doubleBuffer.put(data);

    return byteBuffer.array();
  }

  @Nonnull
  private static byte[] toByteArray(@Nonnull float[] data) {
    ByteBuffer byteBuffer = ByteBuffer.allocate(data.length * Float.BYTES);
    FloatBuffer floatBuffer = byteBuffer.asFloatBuffer();
    floatBuffer.put(data);

    return byteBuffer.array();
  }

  @Nonnull
  private static byte[] toByteArray(@Nonnull int[] data) {
    ByteBuffer byteBuffer = ByteBuffer.allocate(data.length * Integer.BYTES);
    IntBuffer intBuffer = byteBuffer.asIntBuffer();
    intBuffer.put(data);

    return byteBuffer.array();
  }
}
