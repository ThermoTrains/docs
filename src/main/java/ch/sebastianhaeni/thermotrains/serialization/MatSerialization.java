package ch.sebastianhaeni.thermotrains.serialization;

import com.google.gson.*;
import org.opencv.core.Mat;

import java.lang.reflect.Type;

import static ch.sebastianhaeni.thermotrains.serialization.Util.matFromJson;
import static ch.sebastianhaeni.thermotrains.serialization.Util.matToJson;

public class MatSerialization implements JsonSerializer<Mat>, JsonDeserializer<Mat> {
  @Override
  public JsonElement serialize(Mat src, Type type, JsonSerializationContext context) {
    JsonObject object = new JsonObject();
    object.add("data", matToJson(src));
    return object;
  }

  @Override
  public Mat deserialize(JsonElement jsonElement, Type type, JsonDeserializationContext jsonDeserializationContext) throws JsonParseException {
    JsonObject data = jsonElement.getAsJsonObject().get("data").getAsJsonObject();
    return matFromJson(data);
  }
}
