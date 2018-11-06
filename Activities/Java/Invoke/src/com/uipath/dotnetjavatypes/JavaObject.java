package com.uipath.dotnetjavatypes;

public class JavaObject {
    private Object rawObject;
    private Class<?> type;

    public JavaObject(Object rawObject, Class<?> type) {
        this.rawObject = rawObject;
        this.type = type;
    }

    public Object getRawObject() {
        return rawObject;
    }

    public Class<?> getType() {
        return type;
    }
}
