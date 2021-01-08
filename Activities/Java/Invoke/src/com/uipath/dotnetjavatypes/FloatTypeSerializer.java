package com.uipath.dotnetjavatypes;

import com.uipath.instance.ObjectInstance;
import org.json.JSONException;
import org.json.JSONObject;

public class FloatTypeSerializer implements TypeSerializerInterface {

    public JavaObject DeserializeToJavaObject(JSONObject obj) {
        if (obj.has("value")) {
            try {
                float value = (float) obj.getDouble("value");
                return new JavaObject(value, float.class);
            } catch (JSONException e) {  }
        }
        return new JavaObject(null, float.class);
    }

    public JSONObject SerializeToDotNet(ObjectInstance obj) throws JSONException{
        float value = (Float) obj.getRawObject();
        if (!Float.isInfinite(value)) {
            return obj.toJson("System.Single");
        }
        return new JSONObject();
    }
}
