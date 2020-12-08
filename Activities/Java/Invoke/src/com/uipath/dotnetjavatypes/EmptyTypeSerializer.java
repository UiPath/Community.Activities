package com.uipath.dotnetjavatypes;

import com.uipath.instance.ObjectInstance;
import org.json.JSONObject;

import java.lang.reflect.Array;

public class EmptyTypeSerializer implements TypeSerializerInterface {

    public JavaObject DeserializeToJavaObject(JSONObject obj) {
        return new JavaObject(new EmptyClass(), EmptyClass.class);
    }

    public static JavaObject DeserializeToJavaObject(String type){
        if ("System.Int32[]".equals(type)) {
            return new JavaObject(new int[0], Array.newInstance(int.class, 0).getClass());
        } else if ("System.Boolean[]".equals(type)) {
            return new JavaObject(new boolean[0], Array.newInstance(boolean.class, 0).getClass());
        } else if ("System.Byte[]".equals(type)) {
            return new JavaObject(new byte[0], Array.newInstance(byte.class, 0).getClass());
        } else if ("System.Char[]".equals(type)) {
            return new JavaObject(new char[0], Array.newInstance(char.class, 0).getClass());
        } else if ("System.Double[]".equals(type)) {
            return new JavaObject(new double[0], Array.newInstance(double.class, 0).getClass());
        } else if ("System.Single[]".equals(type)) {
            return new JavaObject(new float[0], Array.newInstance(float.class, 0).getClass());
        } else if ("System.Int64[]".equals(type)) {
            return new JavaObject(new long[0], Array.newInstance(long.class, 0).getClass());
        } else if ("System.String[]".equals(type)) {
            return new JavaObject(new String[0], Array.newInstance(String.class, 0).getClass());
        } else if ("System.Int16[]".equals(type)) {
            return new JavaObject(new short[0], Array.newInstance(short.class, 0).getClass());
        }
        return new JavaObject(new Object[0], Array.newInstance(Object.class, 0).getClass());
    }
    public static JavaObject DeserializeToNullJavaObject(String type){
        if ("System.Int32[]".equals(type)) {
            return new JavaObject(null, Array.newInstance(int.class, 0).getClass());
        } else if ("System.Boolean[]".equals(type)) {
            return new JavaObject(null, Array.newInstance(boolean.class, 0).getClass());
        } else if ("System.Byte[]".equals(type)) {
            return new JavaObject(null, Array.newInstance(byte.class, 0).getClass());
        } else if ("System.Char[]".equals(type)) {
            return new JavaObject(null, Array.newInstance(char.class, 0).getClass());
        } else if ("System.Double[]".equals(type)) {
            return new JavaObject(null, Array.newInstance(double.class, 0).getClass());
        } else if ("System.Single[]".equals(type)) {
            return new JavaObject(null, Array.newInstance(float.class, 0).getClass());
        } else if ("System.Int64[]".equals(type)) {
            return new JavaObject(null, Array.newInstance(long.class, 0).getClass());
        } else if ("System.String[]".equals(type)) {
            return new JavaObject(null, Array.newInstance(String.class, 0).getClass());
        } else if ("System.Int16[]".equals(type)) {
            return new JavaObject(null, Array.newInstance(short.class, 0).getClass());
        }
        return new JavaObject(new Object[0], Array.newInstance(Object.class, 0).getClass());
    }

    public JSONObject SerializeToDotNet(ObjectInstance obj) {
        return new JSONObject();
    }
}
