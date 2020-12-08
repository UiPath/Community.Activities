package com.uipath.dotnetjavatypes;

import com.uipath.instance.ObjectInstance;
import org.json.JSONException;
import org.json.JSONObject;

public class BoolTypeSerializer implements TypeSerializerInterface {

    public JavaObject DeserializeToJavaObject(JSONObject obj) {
        if (obj.has("value")) {
            try {
                boolean value = obj.getBoolean("value");
                return new JavaObject (value, boolean.class);
            } catch (JSONException e) {  }
        }
        return new JavaObject(null, boolean.class);
    }

    public JSONObject SerializeToDotNet(ObjectInstance obj) throws JSONException{
        return obj.toJson("System.Boolean");
    }
}
