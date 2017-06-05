package ch.sebastianhaeni.thermotrains.serialization;

import com.google.gson.*;
import org.opencv.core.Mat;

import javax.annotation.Nonnull;
import java.lang.reflect.Type;

import static ch.sebastianhaeni.thermotrains.util.SerializationUtil.matFromJson;
import static ch.sebastianhaeni.thermotrains.util.SerializationUtil.matToJson;

/**
 * Serializes {@link Mat} with GSON.
 */
public class MatSerialization implements JsonSerializer<Mat>, JsonDeserializer<Mat> {

  @Override
  public JsonElement serialize(
    @Nonnull Mat src,
    @Nonnull Type type,
    @Nonnull JsonSerializationContext context) {

    return matToJson(src);
  }

  @Override
  public Mat deserialize(
    @Nonnull JsonElement jsonElement,
    @Nonnull Type type,
    @Nonnull JsonDeserializationContext jsonDeserializationContext)
    throws JsonParseException {

    return matFromJson(jsonElement.getAsJsonObject());
  }
}
