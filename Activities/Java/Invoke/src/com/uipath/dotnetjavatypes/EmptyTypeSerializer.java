package com.uipath.dotnetjavatypes;

import com.uipath.instance.ObjectInstance;
import org.json.JSONObject;

public class EmptyTypeSerializer implements TypeSerializerInterface {

    public JavaObject DeserializeToJavaObject(JSONObject obj) {
        return new JavaObject(new EmptyClass(), EmptyClass.class);
    }

    public JSONObject SerializeToDotNet(ObjectInstance obj) {
        return new JSONObject();
    }
}
