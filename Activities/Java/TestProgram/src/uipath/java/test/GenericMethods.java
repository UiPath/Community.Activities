package uipath.java.test;

public class GenericMethods {
    public  String message;
    String[] arr;

    public GenericMethods() {
        System.out.println("Constructor without parameters");
    }

    public GenericMethods(String[] cons) {
        System.out.println("Constructor with String[]");
    }

    public GenericMethods(String p1, String p2) {
        System.out.println("Constructor with String and String");
    }

    public GenericMethods(String cons) {
        System.out.println("Constructor with String");
    }

    public GenericMethods(int[] cons) {
        System.out.println("Constructor with int[]");
    }

    public static void Write(String[] args) {
        System.out.println("Static method with String[]");
        if (args!=null) {
            System.out.println(args.length);
        }
    }

    public static void Write(String args) {
        System.out.println("Static method with String");
        System.out.println(args);
    }

    public static void Write(int args) {
        System.out.println("Static method with int");
        System.out.println(args);
    }

    public static void Write(boolean args) {
        System.out.println("Static method with boolean");
        System.out.println(args);
    }

    public static void Write(float args) {
        System.out.println("Static method with float");
        System.out.println(args);
    }

    public void WriteObj(String[] args) {
        System.out.println("Instance method with String[]");
        System.out.println(args);
    }

    public void WriteObj(String args) {
        System.out.println("Instance method with String");
        System.out.println(args);
    }

    public void WriteObj(float args) {
        System.out.println("Instance method with float");
        System.out.println(args);
    }

    public <T extends Object> T GenericsExtObj(Object a) {
        message = "Generic method with Object " + a;
        return (T) ("Generic method with Object " + a);
    }

    public <T extends String> Object GenericsExtString(T a) {
        message = "Generic method " + a;
        return "Generic method: " + a;
    }

    public void GenericsS(String a) {
        message = "Method that is named Generics " + a;
        System.out.println("Method that is named Generics");
    }

    public <T> T GenericsR(T a) {
        message = "Generic method with return " + a;
        System.out.println("Generic method with return");
        System.out.println(a);
        return a;
    }

    public void finalize() throws Throwable{
        System.out.println("Object is destroyed by the Garbage Collector");
    }

    public static void foo(){}
}
