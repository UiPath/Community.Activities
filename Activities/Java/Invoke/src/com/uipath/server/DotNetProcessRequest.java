package com.uipath.server;

import com.uipath.instance.ObjectInstance;
import com.uipath.instance.ObjectState;
import com.uipath.invoker.InvokerContext;
import com.uipath.invoker.MethodArguments;
import org.json.JSONException;
import org.json.JSONObject;

import java.util.logging.Logger;

public class DotNetProcessRequest {

    private InvokerContext invokerContext;
    private static final Logger LOGGER = Logger.getLogger( DotNetProcessRequest.class.getName() );

    public DotNetProcessRequest() {
        invokerContext = new InvokerContext();
    }
    /**
     * Parses the json request and delegates work to the invoker context.
     * @param text Text read from the pipe as String
     * @return A response with the result of the invocation if that is the case. If there were not encountered any bugs
     * in answering the request the result state will ObjectState.Successful.
     */
    public Response processRequest(String text) {
        Response response = new Response();
        if (text == null) {
            response.withResult(new ObjectInstance().withObjectState(ObjectState.Successful));;
            return response;
        }
        JSONObject request = new JSONObject();
        try {
            request = new JSONObject(text);
        }
        catch (JSONException e) { }
        RequestType requestType;
        try {
            requestType = RequestType.valueOf(request.getString("request_type"));
        } catch (Exception e) {
            // The enum member was not found
            response.withResult(new ObjectInstance().withObjectState(ObjectState.RequestNotProcessed));;
            return response;
        }

        MethodArguments arguments = new MethodArguments(request);
        switch (requestType) {
            case LoadJar:
                response.withResult(invokerContext.loadJar(arguments));
                break;
            case InvokeStaticMethod:
                response.withResult(invokerContext.invokeStaticMethod(arguments)).withInvoker(invokerContext);
                break;
            case InvokeMethod:
                response.withResult(invokerContext.invokeMethod(arguments)).withInvoker(invokerContext);
                break;
            case InvokeConstructor:
                response.withResult(invokerContext.newInstance(arguments)).withInvoker(invokerContext);
                break;
            case GetField:
                response.withResult(invokerContext.getField(arguments)).withInvoker(invokerContext);
                break;
            case StopConnection:
                response.withResult(new ObjectInstance().withObjectState(ObjectState.Successful)).withShouldStop(true);
                break;
        }
        return response;
    }
}
