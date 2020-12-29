package com.uipath.dotnetjavatypes;

import com.uipath.instance.ObjectInstance;
import org.json.JSONException;
import org.json.JSONObject;

public class ShortTypeSerializer implements TypeSerializerInterface {

    public JavaObject DeserializeToJavaObject(JSONObject obj) {
        if (obj.has("value")) {
            try {
                short value = (short)obj.getInt("value");
                return new JavaObject(value, short.class);
            } catch (JSONException e) {  }
        }
        return new JavaObject(null, short.class);
    }

    public JSONObject SerializeToDotNet(ObjectInstance obj) throws JSONException{
        return obj.toJson("System.Int16");
    }
}
