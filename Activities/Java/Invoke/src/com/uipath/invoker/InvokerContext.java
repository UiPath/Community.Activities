package com.uipath.invoker;

import com.uipath.dotnetjavatypes.EmptyClass;
import com.uipath.instance.ObjectInstance;
import com.uipath.instance.ObjectState;

import java.io.File;
import java.net.MalformedURLException;
import java.net.URL;
import java.net.URLClassLoader;
import java.util.*;

public class InvokerContext {

    private Set<URL> loadedClassURLs = new HashSet<URL>();

    private URLClassLoader classLoader = new URLClassLoader(new URL[0], ClassLoader.getSystemClassLoader());
    /**
     * A cache for the classes that are already loaded.
     */
    private Map<String, Class<?>> classMap = new HashMap<String, Class<?>>();
    /**
     * A cache for the instances used in the current invocation context.
     */
    private Map<UUID, ObjectInstance> objectMap = new HashMap<UUID, ObjectInstance>();


    public ObjectInstance newInstance(MethodArguments arguments) {
        arguments.withInvoker(this);
        return newInstance(arguments.getClassName(), arguments.getParameterTypes(), arguments.getParameters());
    }

    public ObjectInstance invokeMethod(MethodArguments arguments) {
        arguments.withInvoker(this);
        return invokeMethod(arguments.getInstanceId(), arguments.getMethodName(), arguments.getParameterTypes(), arguments.getParameters());
    }

    public ObjectInstance invokeStaticMethod(MethodArguments arguments) {
        arguments.withInvoker(this);
        return invokeStaticMethod(arguments.getClassName(), arguments.getMethodName(), arguments.getParameterTypes(), arguments.getParameters());
    }

    public ObjectInstance getField(MethodArguments arguments) {
        arguments.withInvoker(this);
        return getField(arguments.getFieldName(), arguments.getClassName(), arguments.getInstanceId());
    }

    public ObjectInstance loadJar(MethodArguments arguments) {
        try {
            URL jarUrl = new File(arguments.getJarPath()).toURI().toURL();
            if (jarLoaded(jarUrl)) {
                return new ObjectInstance().withObjectState(ObjectState.JarAlreadyLoaded);
            }
            if (loadJar(jarUrl)) {
                return  new ObjectInstance().withObjectState(ObjectState.Successful);
            } else  {
                return new ObjectInstance().withObjectState(ObjectState.JarNotFound);
            }
        } catch (MalformedURLException e) {
            return new ObjectInstance().withObjectState(ObjectState.JarNotFound).withException(e);
        } catch (Exception e) {
            return new ObjectInstance().withObjectState(ObjectState.JarNotLoaded).withException(e);
        }
    }

    /**
     * Loads a jar from the specified path. Clears the object and class caches.
     * @param jarUrl The path to the jar
     * @return True if the jar was loaded. False, Otherwise.
     */
    private boolean loadJar(URL jarUrl) {
        if (jarUrl == null) {
            return false;
        }
        URL[] newURLs = new URL[1];
        newURLs[0] = jarUrl;
        classLoader = new URLClassLoader(newURLs, classLoader);
        return true;
    }

    private boolean jarLoaded(URL jarURL) {
        return loadedClassURLs.contains(jarURL);
    }


    /**
     * Search for a class name in the cache if it can not find one then loads the class and updates the cache
     * @param className The name of the class.
     * @return The java class type.
     */
    private Class<?> getClassFromName(String className) {
        try {
            if (className == null) {
                return null;
            }

            if (classMap.containsKey(className)) {
                return classMap.get(className);
            }
            Class<?> loadedClass = classLoader.loadClass(className);
            classMap.put(className, loadedClass);
            return loadedClass;
        } catch (ClassNotFoundException e) {
            return null;
        }
    }

    /**
     * Creates a new instance.
     * @param className The name of the class the new instance will be created.
     * @param types The arguement types of the constructor. If the constructor has no arguments than types is null
     * @param params The arguments for the constructor. If the constructor has no arguments then params is null
     * @return Return a custom object, ObjectInstance, which stores the raw value of the new instance. Updates the
     * object instances cache.
     */

    private ObjectInstance newInstance(String className, Class<?>[] types, Object... params) {
        Class<?> loadedClass = getClassFromName(className);
        if (loadedClass == null) {
            return new ObjectInstance().withObjectState(ObjectState.ClassNotFound);
        }
        ObjectInstance result = ObjectInstance.newInstanceFactory(loadedClass, types, params);
        if (result.getObjectState() == ObjectState.Successful) {
            objectMap.put(result.getReferenceId(), result);
        }
        return result;
    }

    /**
     * Invokes a member method on an object. A prior instance is required.
     * @param instanceId The UUID of the previously created instance
     * @param methodName The name of the method that will be invoked
     * @param types The argument types of the method. If the method has no arguments than types is null
     * @param params The arguments for the method. If the method has no arguments then params is null
     * @return
     */

    private ObjectInstance invokeMethod(UUID instanceId, String methodName, Class<?>[] types, Object... params) {
        if (instanceId == null || !objectMap.containsKey(instanceId)) {
            return new ObjectInstance().withObjectState(ObjectState.InstanceNotFound);
        }

        ObjectInstance result = objectMap.get(instanceId).InvokeMethod(methodName, types, params);
        if (result.getObjectState() == ObjectState.Successful) {
            objectMap.put(result.getReferenceId(), result);
        }
        return result;
    }

    /**
     * Invokes a static method on the specified class.
     * @param className The name of the class the method will be invoked
     * @param methodName The name of the method that will be invoked
     * @param types The argument types of the method. If the method has no arguments than types is null
     * @param params The arguments for the method. If the method has no arguments then params is null
     * @return
     */

    private ObjectInstance invokeStaticMethod(String className, String methodName, Class<?>[] types, Object... params) {
        Class<?> loadedClass = getClassFromName(className);
        if (loadedClass == null) {
            return new ObjectInstance().withObjectState(ObjectState.ClassNotFound);
        }
        ObjectInstance instance = new ObjectInstance().withObjectClass(getClassFromName(className));
        ObjectInstance result = instance.InvokeMethod(methodName, types, params);
        if (result.getObjectState() == ObjectState.Successful) {
            objectMap.put(result.getReferenceId(), result);
        }
        return result;
    }

    private ObjectInstance getField(String fieldName, String className, UUID instanceId) {
        ObjectInstance instance = new ObjectInstance();
        if (instanceId != null && objectMap.containsKey(instanceId)) {
            instance = objectMap.get(instanceId);
        } else if (className  != null) {
            instance.withObjectClass(getClassFromName(className));
        } else {
            return instance.withObjectState(ObjectState.FieldNotFound);
        }

        return instance.getField(fieldName);
    }

    public ObjectInstance getInstance(UUID id) {
        if (objectMap.containsKey(id)) {
            return objectMap.get(id);
        } else {
            return new ObjectInstance().withObject(new EmptyClass()).withObjectState(ObjectState.InstanceNotFound);
        }
    }
}
