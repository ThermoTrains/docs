package ch.sebastianhaeni.thermotrains.serialization;

import com.google.gson.*;
import org.opencv.core.Mat;

import java.lang.reflect.Type;

import static ch.sebastianhaeni.thermotrains.util.SerializationUtil.matFromJson;
import static ch.sebastianhaeni.thermotrains.util.SerializationUtil.matToJson;

/**
 * Serializes {@link Mat} with GSON.
 */
public class MatSerialization implements JsonSerializer<Mat>, JsonDeserializer<Mat> {
  @Override
  public JsonElement serialize(Mat src, Type type, JsonSerializationContext context) {
    return matToJson(src);
  }

  @Override
  public Mat deserialize(JsonElement jsonElement, Type type, JsonDeserializationContext jsonDeserializationContext) throws JsonParseException {
    return matFromJson(jsonElement.getAsJsonObject());
  }
}
