package com.uipath.instance;

import com.uipath.dotnetjavatypes.EmptyClass;
import org.json.JSONException;
import org.json.JSONObject;

import java.io.*;
import java.lang.reflect.*;
import java.util.*;

public class ObjectInstance {
    private Class<?> belongsTo;
    private Object rawObject = null;
    private ObjectState objectState;
    private List<String> errors = new ArrayList<String>();
    private List<String> executionErrors = new ArrayList<String>();
    private UUID id;

    public  ObjectInstance() {}

    public ObjectInstance withObject(Object object){
        if (object == null) {
            this.belongsTo = EmptyClass.class;
        } else {
            this.belongsTo = object.getClass();
        }
        this.rawObject = object;

        return this;
    }

    public ObjectInstance withObjectState(ObjectState state){
        this.objectState = state;
        return this;
    }

    public Object getRawObject() {
        return rawObject;
    }

    public ObjectState getObjectState() {
        return objectState;
    }

    public Class<?> getInstanceClass() {
        return belongsTo;
    }

    public UUID getReferenceId() {
        return id;
    }

    public List<String> getErrors() {

        return errors;
    }

    /**
     * Errors are a sub set of execution errors. Thus, we remove our current Invoke java exceptions to get the errors
     * inside the loaded jar.
     * @return
     */

    public List<String> getExecutionErrors() {
        return executionErrors;
    }

    public ObjectInstance withObjectClass(Class<?> objectClass) {
        this.belongsTo = objectClass;
        return this;
    }

    public ObjectInstance withException(Exception e) {
        StringBuilder executionExceptions = new StringBuilder();
        if (e.getCause() != null) {
            executionExceptions.append(e.getCause().toString());
            for(StackTraceElement stackElement:e.getCause().getStackTrace()) {
                if (belongsTo == null || stackElement == null) {
                    continue;
                }
                if (stackElement.getClassName().equals(belongsTo.getName())) {
                    executionExceptions.append('\n');
                    executionExceptions.append(stackElement.toString());
                }
            }
            executionErrors.add(executionExceptions.toString());
        }
        errors.add(getStackTraceAsString(e));
        return this;
    }

    /**
     *
     * @param loadedClass
     * @param types
     * @param params
     * @return
     */
    public static ObjectInstance newInstanceFactory(Class<?> loadedClass, Class<?>[] types, Object... params) {
        ObjectInstance newInstance = new ObjectInstance();
        try {
            newInstance.belongsTo = loadedClass;
            newInstance.rawObject = getConstructor(loadedClass, types).newInstance(params);
            newInstance.id = UUID.randomUUID();
            newInstance.objectState = ObjectState.Successful;
            return  newInstance;
        } catch (NoSuchMethodException e) {
            newInstance.objectState = ObjectState.ConstructorNotFound;
            newInstance.withException(e);
        } catch (InstantiationException e) {
            newInstance.objectState = ObjectState.InstantiationException;
            newInstance.withException(e);
        } catch (IllegalAccessException e) {
            newInstance.objectState = ObjectState.IllegalAccess;
            newInstance.withException(e);
        } catch (IllegalArgumentException e) {
            newInstance.objectState = ObjectState.IllegalArguments;
            newInstance.withException(e);
        } catch (InvocationTargetException e) {
            newInstance.objectState = ObjectState.InvocationTarget;
            newInstance.withException(e);
        } catch (Exception e) {
            newInstance.objectState = ObjectState.UnknownException;
            newInstance.withException(e);
        }
        return newInstance;
    }


    /**
     *
     * @param methodName
     * @param types
     * @param params
     * @return
     */
    public ObjectInstance InvokeMethod(String methodName, Class<?>[] types, Object... params) {
        ObjectInstance result = new ObjectInstance();
        try {
            Method method = getMethod(methodName, belongsTo, types);
            result.withObject(method.invoke(rawObject, params)).withObjectState(ObjectState.Successful);
            result.id = UUID.randomUUID();
        } catch (NoSuchMethodException e) {
            result.objectState = ObjectState.MethodNotFound;
            result.withException(e);
        } catch (IllegalAccessException e) {
            result.objectState =  ObjectState.IllegalAccess;
            result.withException(e);
        } catch (InvocationTargetException e) {
            result.objectState = ObjectState.InvocationTarget;
            result.belongsTo = this.belongsTo;
            result.withException(e);
        } catch (Exception e) {
            result.objectState = ObjectState.UnknownException;
            result.withException(e);
        }
        return result;
    }

    public ObjectInstance getField(String fieldName) {
        ObjectInstance result = new ObjectInstance();
        try {
            Field field = belongsTo.getField(fieldName);
            result.withObject(field.get(rawObject)).withObjectState(ObjectState.Successful);
            result.id = UUID.randomUUID();

        } catch (NoSuchFieldException e) {
            result.objectState = ObjectState.FieldNotFound;
            result.withException(e);
        } catch (IllegalAccessException e) {
            result.objectState = ObjectState.IllegalAccess;
            result.withException(e);
        } catch (Exception e) {
            result.objectState = ObjectState.UnknownException;
            result.withException(e);
        }
        return result;
    }

    public ObjectInstance setField(String fieldName, ObjectInstance value) {
        ObjectInstance result = new ObjectInstance();
        Class<?> instanceClass = belongsTo;
        try {
            Field field = instanceClass.getField(fieldName);
            field.set(rawObject, value.getRawObject());
            result.withObjectState(ObjectState.Successful);
        } catch (NoSuchFieldException e) {
            result.objectState = ObjectState.FieldNotFound;
        } catch (IllegalAccessException e) {
            result.objectState = ObjectState.IllegalAccess;
            result.withException(e);
        } catch (Exception e) {
            result.objectState = ObjectState.UnknownException;
            result.withException(e);
        }
        return result;
    }

    /**
     *
     * @param e
     * @return
     */
    private static String getStackTraceAsString(Throwable e) {
        StringWriter sw = new StringWriter();
        PrintWriter pw = new PrintWriter(sw);
        e.printStackTrace(pw);
        return sw.toString();
    }

    private static Method getMethod(String methodName, Class<?> loadedClass, Class<?>[] parameterTypes) throws NoSuchMethodException {
        Method[] methods = loadedClass.getMethods();
        if (methods == null) {
            throw new NoSuchMethodException();
        }
        InvocationTarget invocationTarget = new InvocationTarget(methodName, parameterTypes);
        for (Method method:methods) {
            InvocationTarget loadedInvocationTarget = new InvocationTarget(method);
            if (loadedInvocationTarget.equals(invocationTarget)) {
                return method;
            }
        }
        throw new NoSuchMethodException();
    }

    private static Constructor<?> getConstructor(Class<?> loadedClass, Class<?>[] parameterTypes) throws NoSuchMethodException {
        Constructor<?>[] constructors = loadedClass.getConstructors();
        if (constructors == null) {
            throw new NoSuchMethodException();
        }
        InvocationTarget invocationTarget = new InvocationTarget(parameterTypes);
        for (Constructor<?> constructor:constructors) {
            InvocationTarget loadedInvocationTarget = new InvocationTarget(constructor);
            if (invocationTarget.equals(loadedInvocationTarget)) {
                return constructor;
            }
        }
        throw new NoSuchMethodException();
    }

    public JSONObject toJson(String dotNetType) throws JSONException {
        JSONObject json = new JSONObject();
        json.put("value", rawObject);
        json.put("reference_id", getReferenceId());
        if (dotNetType != null && dotNetType.length() > 0) {
            json.put("runtime_type", dotNetType);
        }
        return json;
    }
}

class InvocationTarget {
    private String methodName;
    private Class<?>[] parameterTypes;

    private static final int MOD = 666013;
    private final static Map<Class<?>, Class<?>> primitiveWrappedTypesMap = new HashMap<Class<?>, Class<?>>();
    static {
        primitiveWrappedTypesMap.put(boolean.class, Boolean.class);
        primitiveWrappedTypesMap.put(byte.class, Byte.class);
        primitiveWrappedTypesMap.put(short.class, Short.class);
        primitiveWrappedTypesMap.put(char.class, Character.class);
        primitiveWrappedTypesMap.put(int.class, Integer.class);
        primitiveWrappedTypesMap.put(long.class, Long.class);
        primitiveWrappedTypesMap.put(float.class, Float.class);
        primitiveWrappedTypesMap.put(double.class, Double.class);
    }

    public InvocationTarget(String methodName, Class<?>[] parameterTypes) {
        this.methodName = methodName;
        this.parameterTypes = parameterTypes;
        setFieldsIfNull();
    }

    public InvocationTarget(Method method) {
        this.methodName = method.getName();
        this.parameterTypes = method.getParameterTypes();
        setFieldsIfNull();
    }

    public InvocationTarget(Constructor<?> constructor) {
        this.methodName = null;
        this.parameterTypes = constructor.getParameterTypes();
        setFieldsIfNull();
    }

    public InvocationTarget(Class<?>[] parameterTypes) {
        this.parameterTypes = parameterTypes;
        setFieldsIfNull();
    }

    private void setFieldsIfNull() {
        if (parameterTypes == null) {
            parameterTypes = new Class<?>[0];
        }
        if (methodName == null) {
            methodName = "";
        }
    }

    private static Class<?> getWrappedType(Class<?> type) {
        if (primitiveWrappedTypesMap.containsKey(type)) {
            return primitiveWrappedTypesMap.get(type);
        }
        return type;
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == null) {
            return false;
        }
        InvocationTarget that = (InvocationTarget) obj;
        if (this == that) {
            return true;
        }
        if (!this.methodName.equals(that.methodName)) {
            return false;
        }
        if (this.parameterTypes.length != that.parameterTypes.length) {
            return false;
        }
        for (int i = 0; i < parameterTypes.length; ++i) {
            if (parameterTypes[i] == null || that.parameterTypes[i] == null) {
                return false;
            }
            Class<?> thisType = getWrappedType(parameterTypes[i]);
            Class<?> thatType = getWrappedType(that.parameterTypes[i]);
            if (!thisType.isAssignableFrom(thatType)) {
                return false;
            }
        }
        return true;
    }

    @Override
    public int hashCode() {
        return (int)((long)methodName.hashCode() * parameterTypes.length) % 666013;
    }
}


