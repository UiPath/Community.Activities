package com.uipath.dotnetjavatypes;

import com.uipath.invoker.InvokerContext;
import com.uipath.instance.ObjectInstance;
import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.lang.reflect.Array;
import java.util.Arrays;

public class ArrayTypeSerializer implements TypeSerializerInterface {

    private InvokerContext context;

    ArrayTypeSerializer(InvokerContext context) {
        this.context = context;
    }

    public JavaObject DeserializeToJavaObject(JSONObject obj) {
        JSONArray array = null;
        String runtimeArrayType = null;
        try {
            array = obj.getJSONArray("value");
            runtimeArrayType = obj.getString("runtime_arrayType");
        }
        catch (JSONException e) {  }

        if (array == null) {
            return new JavaObject(new EmptyClass(), boolean.class);
        }
        Class<?> arrayType = getArrayType(context, array);
        int length = array.length();
        if (array.length() == 0){
            return EmptyTypeSerializer.DeserializeToJavaObject(runtimeArrayType);
        }
        Object result = Array.newInstance(arrayType, length);

        for (int i = 0; i < length; ++i) {
            try {
                TypeSerializerInterface type = new TypeSerializerFactory(context).CreateType(array.getJSONObject(i));
                Array.set(result, i, type.DeserializeToJavaObject(array.getJSONObject(i)).getRawObject());
            } catch (Exception e) {
                return new JavaObject(new EmptyClass(), EmptyClass.class);
            }
        }
        return new JavaObject(result, Array.newInstance(arrayType, 0).getClass());
    }

    public JSONObject SerializeToDotNet(ObjectInstance obj) {
        JSONObject json = new JSONObject();
        JSONArray array = new JSONArray();
        Object rawObject = obj.getRawObject();
        int len = Array.getLength(rawObject);
        for(int i = 0; i < len; ++i) {
            Object item = Array.get(rawObject, i);
            Class<?> itemType = null;
            if (item != null) {
                itemType = item.getClass();
            }
            TypeSerializerInterface type = new TypeSerializerFactory(context).CreateType(itemType);
            try {
                array.put(type.SerializeToDotNet(new ObjectInstance().withObject(item)).put("__type", "JavaObjectInstance:#UiPath.Java.Service"));
            } catch (Exception e) {
                array.put(new EmptyTypeSerializer().SerializeToDotNet(new ObjectInstance()));
            }
        }
        try {
            json.put("value", array);
            json.put("reference_id", obj.getReferenceId());
            json.put("runtime_type", "Array");
        } catch (Exception e) { }

        return json;
    }

    private Class<?>  getArrayType(InvokerContext context, JSONArray array) {
        Class<?> currentType = null;
        if (array == null) {
            return EmptyClass.class;
        }
        try {
            int length = array.length();
            for (int i = 0; i < length; ++i) {
                TypeSerializerInterface type = new TypeSerializerFactory(this.context).CreateType(array.getJSONObject(i));
                Class<?> itemType = type.DeserializeToJavaObject(array.getJSONObject(i)).getType();
                if (i > 0 && currentType != itemType) {
                    return EmptyClass.class;
                }
                currentType = itemType;
            }
        }
        catch (Exception e) { }

        if (currentType == null) {
            return EmptyClass.class;
        }
        return currentType;
    }
}
