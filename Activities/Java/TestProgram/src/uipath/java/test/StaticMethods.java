package uipath.java.test;

public class StaticMethods {
    public static int[] getArrayInt() {
        return new int[] {1, 4, 5, 6, 7, 8};
    }

    public static int getSumInt(int[] arr) {
        int res = 0;
        for (int i = 0; i < arr.length; ++i) {
            res += arr[i];
        }
        return res;
    }

    public static void throwException() {
        throw new NullPointerException("This is a test");
    }

    public static char getChar() {
        return 'a';
    }

    public static Float getFloat() {
        return 2.3f;
    }

    public static int getSumWrapped(Integer a, int b) {
        return a + b;
    }

    public static Double[] getArrayDoubleBoxed() {
        return new Double[] {1.3, 4.5, 2.3, 23.12, 123.1, 23.1, 43.1};
    }


    public static Integer getSumDoubleBoxed(Double[] arr) {
        int res = 0;
        for (int i = 0; i < arr.length; ++i) {
            res += arr[i];
        }
        return res;
    }
    public static int compare(Integer a, Integer b) {
        return 123;
    }

    public static <T extends Comparable<T>> int compare(T a, T b) {
        return a.compareTo(b);
    }
}
