package uipath.java.test;

public class GenericMethods {
  private String message;
  
  public GenericMethods() {}
  
  public GenericMethods(String sParam) {
    this.message = sParam;
  }
  
  public String getMessage() {
    return this.message;
  }
  
  public String[] GenericMethods(String[] strArrParam) {
    return strArrParam;
  }
  
  public String GenericMethods(String s1, String s2) {
    return s1 + s2;
  }
  
  public int[] IntArr(int[] intArrParam) {
    return intArrParam;
  }
  
  public static String[] StringArrStatic(String[] args) {
    return args;
  }
  
  public static int IntType(int args) {
    return args;
  }
  
  public static boolean BoolType(boolean args) {
    return args;
  }
  
  public static float FloatType(float args) {
    return args;
  }
  
  public String[] StringArrNonStatic(String[] args) {
    return args;
  }
  
  public <T> T GenericsExtObj(Object a) {
    this.message = "Generic method with Object " + a;
    return (T)this.message;
  }
  
  public <T extends String> Object GenericsExtString(T a) {
    this.message = "Generic method " + a;
    return this.message;
  }
  
  public <T> T GenericsR(T a) {
    this.message = "Generic method with return " + a;
    return (T)this.message;
  }
  
  public void finalize() throws Throwable {}
  
  public String StringParamValidation(String X) {
    return X;
  }
  
  public String ConcatenateXYZ() {
    String X = "X";
    String Y = "Y";
    String Z = "Z";
    return StringParamValidation(X) + StringParamValidation(Y) + StringParamValidation(Z);
  }
  
  public int RecursiveCallTest(int k) {
    if (k < 2)
      return 1; 
    return k * RecursiveCallTest(k - 1);
  }
  
  public static int StaticRecursiveCallTest(int k) {
    if (k < 2)
      return 1; 
    return k * StaticRecursiveCallTest(k - 1);
  }
}
