package ch.sebastianhaeni.thermotrains.util;

import com.google.gson.JsonObject;
import org.opencv.core.CvType;
import org.opencv.core.Mat;

import java.nio.*;
import java.util.Base64;

public final class SerializationUtil {

  public static JsonObject matToJson(Mat mat) {
    JsonObject obj = new JsonObject();

    if (!mat.isContinuous()) {
      System.out.println("Mat not continuous.");
      return null;
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
      dataString = new String(encoder.encode(toByteArray(data)));
    } else if (type == CvType.CV_32F || type == CvType.CV_32FC2) {
      float[] data = new float[cols * rows * elemSize];
      mat.get(0, 0, data);
      dataString = new String(encoder.encode(toByteArray(data)));
    } else if (type == CvType.CV_64F || type == CvType.CV_64FC2) {
      double[] data = new double[cols * rows * elemSize];
      mat.get(0, 0, data);
      dataString = new String(encoder.encode(toByteArray(data)));
    } else if (type == CvType.CV_8U) {
      byte[] data = new byte[cols * rows * elemSize];
      mat.get(0, 0, data);
      dataString = new String(encoder.encode(data));
    } else {
      throw new UnsupportedOperationException("unknown type");
    }
    obj.addProperty("data", dataString);

    return obj;
  }

  public static Mat matFromJson(JsonObject json) {
    int rows = json.get("rows").getAsInt();
    int cols = json.get("cols").getAsInt();
    int type = json.get("type").getAsInt();

    Mat mat = new Mat(rows, cols, type);

    String dataString = json.get("data").getAsString();
    Base64.Decoder decoder = Base64.getDecoder();

    if (type == CvType.CV_32S || type == CvType.CV_32SC2 || type == CvType.CV_32SC3 || type == CvType.CV_16S) {
      int[] data = toIntArray(decoder.decode(dataString.getBytes()));
      mat.put(0, 0, data);
    } else if (type == CvType.CV_32F || type == CvType.CV_32FC2) {
      float[] data = toFloatArray(decoder.decode(dataString.getBytes()));
      mat.put(0, 0, data);
    } else if (type == CvType.CV_64F || type == CvType.CV_64FC2) {
      double[] data = toDoubleArray(decoder.decode(dataString.getBytes()));
      mat.put(0, 0, data);
    } else if (type == CvType.CV_8U) {
      byte[] data = decoder.decode(dataString.getBytes());
      mat.put(0, 0, data);
    } else {
      throw new UnsupportedOperationException("unknown type");
    }

    return mat;
  }

  private static int[] toIntArray(byte[] data) {
    IntBuffer intBuf = ByteBuffer.wrap(data)
      .order(ByteOrder.BIG_ENDIAN)
      .asIntBuffer();
    int[] array = new int[intBuf.remaining()];
    intBuf.get(array);
    return array;
  }

  private static float[] toFloatArray(byte[] data) {
    FloatBuffer floatBuffer = ByteBuffer.wrap(data)
      .order(ByteOrder.BIG_ENDIAN)
      .asFloatBuffer();
    float[] array = new float[floatBuffer.remaining()];
    floatBuffer.get(array);
    return array;
  }

  private static double[] toDoubleArray(byte[] data) {
    DoubleBuffer doubleBuffer = ByteBuffer.wrap(data)
      .order(ByteOrder.BIG_ENDIAN)
      .asDoubleBuffer();
    double[] array = new double[doubleBuffer.remaining()];
    doubleBuffer.get(array);
    return array;
  }

  private static byte[] toByteArray(double[] data) {
    ByteBuffer byteBuffer = ByteBuffer.allocate(data.length * Double.BYTES);
    DoubleBuffer doubleBuffer = byteBuffer.asDoubleBuffer();
    doubleBuffer.put(data);

    return byteBuffer.array();
  }

  private static byte[] toByteArray(float[] data) {
    ByteBuffer byteBuffer = ByteBuffer.allocate(data.length * Float.BYTES);
    FloatBuffer floatBuffer = byteBuffer.asFloatBuffer();
    floatBuffer.put(data);

    return byteBuffer.array();
  }

  private static byte[] toByteArray(int[] data) {
    ByteBuffer byteBuffer = ByteBuffer.allocate(data.length * Integer.BYTES);
    IntBuffer intBuffer = byteBuffer.asIntBuffer();
    intBuffer.put(data);

    return byteBuffer.array();
  }
}
