package com.uipath.dotnetjavatypes;

import com.uipath.instance.ObjectInstance;
import org.json.JSONException;
import org.json.JSONObject;

public interface TypeSerializerInterface {

    public JavaObject DeserializeToJavaObject(JSONObject obj);

    public JSONObject SerializeToDotNet(ObjectInstance obj) throws JSONException;

}
