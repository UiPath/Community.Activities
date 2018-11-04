package com.uipath.server;

import com.uipath.dotnetjavatypes.TypeSerializerFactory;
import com.uipath.dotnetjavatypes.TypeSerializerInterface;
import com.uipath.instance.ObjectInstance;
import com.uipath.invoker.InvokerContext;
import org.json.JSONObject;

public class Response {
    private ObjectInstance result;

    private boolean shouldStop;

    private boolean isRequestEmpty;

    private InvokerContext invokerContext;


    public Response withResult(ObjectInstance result) {
        this.result = result;
        return this;
    }

    public Response withShouldStop(boolean shouldStop) {
        this.shouldStop = shouldStop;
        return this;
    }

    public Response withInvoker(InvokerContext invokerContext) {
        this.invokerContext = invokerContext;
        return this;
    }

    public boolean shouldJavaStop() {
        return shouldStop;
    }

    public JSONObject toJson() {
        JSONObject json = new JSONObject();
        try {
            if (invokerContext != null) {
                if (result.getRawObject() != null) {
                    TypeSerializerInterface type = new TypeSerializerFactory(invokerContext).CreateType(result.getRawObject().getClass());
                    json.put("result", type.SerializeToDotNet(result));
                }
            }

            json.put("errors", result.getErrors());
            json.put("execution_errors", result.getExecutionErrors());
            json.put("result_state", result.getObjectState());
        } catch (Exception e) {}
        return json;
    }

}
