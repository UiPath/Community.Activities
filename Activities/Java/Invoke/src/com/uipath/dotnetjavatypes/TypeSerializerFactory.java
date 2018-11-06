package com.uipath.dotnetjavatypes;

import com.uipath.instance.ObjectInstance;
import com.uipath.invoker.InvokerContext;
import org.json.JSONException;
import org.json.JSONObject;

public class TypeSerializerFactory {
    private InvokerContext context;

    public TypeSerializerFactory(InvokerContext context) {
        this.context = context;
    }

    public TypeSerializerInterface CreateType(JSONObject json) {
        if (json.has("reference_id")) {
            return new JavaObjectSerializer(context);
        }

        if (!json.has("runtime_type")) {
            return new EmptyTypeSerializer();
        }

        // Because Java 1.5
        String type = null;
        try {
            type = json.getString("runtime_type");
        } catch (JSONException e) {}

        if ("System.Int32".equals(type)) {
            return new IntTypeSerializer();
        } else if ("System.Boolean".equals(type)) {
            return new BoolTypeSerializer();
        } else if ("System.Byte".equals(type)) {
            return new ByteTypeSerializer();
        } else if ("System.Char".equals(type)) {
            return new CharTypeSerializer();
        } else if ("System.Double".equals(type)) {
            return new DoubleTypeSerializer();
        } else if ("System.Single".equals(type)) {
            return new FloatTypeSerializer();
        } else if ("System.Int64".equals(type)) {
            return new LongTypeSerializer();
        } else if ("System.String".equals(type)) {
            return new StringTypeSerializer();
        } else if ("System.Int16".equals(type)) {
            return new ShortTypeSerializer();
        } else if ("JavaObject".equals(type)) {
            return new JavaObjectSerializer(context);
        } else if ("Array".equals(type)) {
            return new ArrayTypeSerializer(context);
        } else {
            return new EmptyTypeSerializer();
        }
    }

    public TypeSerializerInterface CreateType(Class<?> objClass) {
        if (objClass == null || objClass == EmptyClass.class) {
            return new EmptyTypeSerializer();
        }
        if (objClass == Integer.class) {
            return new IntTypeSerializer();
        }
        if (objClass == Boolean.class) {
            return new BoolTypeSerializer();
        }
        if (objClass == Byte.class) {
            return new ByteTypeSerializer();
        }
        if (objClass == Character.class) {
            return new CharTypeSerializer();
        }
        if (objClass == Double.class) {
            return new DoubleTypeSerializer();
        }
        if (objClass == Float.class) {
            return new FloatTypeSerializer();
        }
        if (objClass == Long.class) {
            return new LongTypeSerializer();
        }
        if (objClass == String.class) {
            return new StringTypeSerializer();
        }
        if (objClass == Short.class) {
            return new ShortTypeSerializer();
        }
        if (objClass.isArray()) {
            return new ArrayTypeSerializer(context);
        }
        return new JavaObjectSerializer(context);
    }
}
