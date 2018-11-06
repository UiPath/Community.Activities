package com.uipath.dotnetjavatypes;

import com.uipath.instance.ObjectInstance;
import org.json.JSONException;
import org.json.JSONObject;

public class StringTypeSerializer implements TypeSerializerInterface {

    public JavaObject DeserializeToJavaObject(JSONObject obj) {
        if (obj.has("value")) {
            try {
                String value = obj.getString("value");
                return new JavaObject(value, String.class);
            } catch (JSONException e) {   }
        }
        return new JavaObject(new EmptyClass(), EmptyClass.class);
    }

    public JSONObject SerializeToDotNet(ObjectInstance obj) throws JSONException{
        return obj.toJson("System.String");
    }
}
