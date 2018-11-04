package com.uipath.dotnetjavatypes;

import com.uipath.invoker.InvokerContext;
import com.uipath.instance.ObjectInstance;
import org.json.JSONException;
import org.json.JSONObject;

import java.util.UUID;

public class JavaObjectSerializer implements TypeSerializerInterface {

    private InvokerContext invokerContext;

    public JavaObjectSerializer(InvokerContext invokerContext) {
        this.invokerContext = invokerContext;
    }

    public JavaObject DeserializeToJavaObject(JSONObject obj) {
        if (obj.has("reference_id")) {
            try {
                UUID id = UUID.fromString(obj.getString("reference_id"));
                ObjectInstance instance = invokerContext.getInstance(id);
                return new JavaObject(instance.getRawObject(), instance.getInstanceClass());

            } catch (JSONException e) {  }
        }
        return new JavaObject(new EmptyClass(), EmptyClass.class);
    }

    public JSONObject SerializeToDotNet(ObjectInstance obj) throws JSONException{
        return obj.toJson("");
    }
}

