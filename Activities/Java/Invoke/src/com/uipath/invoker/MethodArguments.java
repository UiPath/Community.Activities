package com.uipath.invoker;

import com.uipath.dotnetjavatypes.JavaObject;
import com.uipath.dotnetjavatypes.TypeSerializerFactory;
import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.util.UUID;
import java.util.logging.Level;
import java.util.logging.Logger;

public class MethodArguments {

    private JSONObject json;
    private InvokerContext context;
    private Object[] parameters = null;
    private Class<?>[] parametersTypes = null;
    private boolean parametersSet = false;
    private static final Logger LOGGER = Logger.getLogger( MethodArguments.class.getName() );


    public MethodArguments(JSONObject json) {
        this.json = json;
    }

    public MethodArguments withInvoker(InvokerContext invokerContext) {
        this.context = invokerContext;
        setParameters();
        return this;
    }

    /**
     * Iterates over json array of parameters and deserealizes each item(from dot net) to a java type.
     */
    private  void setParameters() {
        if (json.has("parameters")) {
            JSONArray array = null;
            try {
                array = json.getJSONArray("parameters");
            } catch (JSONException e) {
                return ;
            }
            if (array == null) {
                return;
            }
            int length = array.length();
            if (length == 0) {
                return;
            }
            parameters = new Object[length];
            parametersTypes = new Class<?>[length];
            for (int i = 0; i < length; ++i) {
                JSONObject item = new JSONObject();
                try {
                    item = array.getJSONObject(i);
                } catch (JSONException e) {}
                JavaObject result = new TypeSerializerFactory(context).CreateType(item).DeserializeToJavaObject(item);
                parameters[i] = result.getRawObject();
                parametersTypes[i] = result.getType();
            }
        }
    }

    public Class<?>[] getParameterTypes() {
        return parametersTypes;
    }

    public Object[] getParameters() {
        return parameters;
    }


    public String getClassName() {
        return tryGetString("class_name");
    }

    public String getMethodName() {
        return tryGetString("method_name");
    }

    public String getJarPath() {
        return  tryGetString("jar_path");
    }

    public String getFieldName() {
        return tryGetString("field_name");
    }

    private String tryGetString(String key) {
        if (json.has(key)) {
            try {
                return json.getString(key);
            } catch (JSONException e) { }
        }
        return null;
    }

    public UUID getInstanceId() {
        if (json.has("instance")) {
            JSONObject jInstance = new JSONObject();
            try {
                jInstance = json.getJSONObject("instance");
            } catch (Exception e) { }

            if (jInstance.has("reference_id")) {
                try {
                    return UUID.fromString(jInstance.getString("reference_id"));
                } catch (Exception e) {
                    LOGGER.log(Level.WARNING, "Invokation on instance does not have a valid UUID.");
                }
            }
        }
        return null;
    }
}
