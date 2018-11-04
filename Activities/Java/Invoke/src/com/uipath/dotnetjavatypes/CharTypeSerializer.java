package com.uipath.dotnetjavatypes;

import com.uipath.instance.ObjectInstance;
import org.json.JSONException;
import org.json.JSONObject;

public class CharTypeSerializer implements TypeSerializerInterface {

    public JavaObject DeserializeToJavaObject(JSONObject obj) {
        if (obj.has("value")) {
            try {
                char value = obj.getString("value").charAt(0);
                return new JavaObject(value, char.class);
            } catch (JSONException e) {  }
        }
        return new JavaObject(new EmptyClass(), EmptyClass.class);
    }

    public JSONObject SerializeToDotNet(ObjectInstance obj) throws JSONException{
        return obj.toJson("System.Char");
    }
}
