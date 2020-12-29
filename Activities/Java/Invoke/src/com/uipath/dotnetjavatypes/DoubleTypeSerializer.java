package com.uipath.dotnetjavatypes;

import com.uipath.instance.ObjectInstance;
import org.json.JSONException;
import org.json.JSONObject;

public class DoubleTypeSerializer implements TypeSerializerInterface {

    public JavaObject DeserializeToJavaObject(JSONObject obj) {
        if (obj.has("value")) {
            try {
                double value = obj.getDouble("value");
                return new JavaObject(value, double.class);
            } catch (JSONException e) {  }
        }
        return new JavaObject(null, double.class);
    }

    public JSONObject SerializeToDotNet(ObjectInstance obj) throws JSONException{
        double value = (Double) obj.getRawObject();
        if (!Double.isInfinite(value)) {
            return obj.toJson("System.Double");
        }
        return new JSONObject();
    }
}
