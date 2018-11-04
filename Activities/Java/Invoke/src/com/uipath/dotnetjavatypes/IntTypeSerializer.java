package com.uipath.dotnetjavatypes;

import com.uipath.instance.ObjectInstance;
import org.json.JSONException;
import org.json.JSONObject;

public class IntTypeSerializer implements TypeSerializerInterface {
    public JavaObject DeserializeToJavaObject(JSONObject obj) {
        if (obj.has("value")) {
            try {
                int value = obj.getInt("value");
                return new JavaObject(value, int.class);
            } catch (JSONException e) {  }
        }
        return new JavaObject(new EmptyClass(), EmptyClass.class);
    }

    public JSONObject SerializeToDotNet(ObjectInstance obj) throws JSONException{
        return obj.toJson("System.Int32");
    }
}
