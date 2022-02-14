package uipath.java.test;

public class Coordinate {
    protected double x;
    protected float y;
    private static final int MOD = 666013;
    private static int instance_counter = 0;

    public Coordinate() {

        ++instance_counter;
    }

    public Coordinate(double x, float y) {
        this.x = x;
        this.y = y;
        ++instance_counter;
    }

    public Coordinate(Double x, Double y) {
        this.x = x;
        this.y = y.floatValue();
        ++instance_counter;
    }

    public double getX() {
        return x;
    }

    public float getY() {
        return y;
    }

    public Coordinate setX(double x) {
        this.x = x;
        return this;
    }

    public Coordinate setY(float y) {
        this.y = y;
        return this;
    }


    public double getCoordinateSum() {
        return x + y;
    }

    public static double getXCoordinateSum(Coordinate[] coordinates) {
        double res = 0;
        for (Coordinate coordinate:coordinates) {
            res += coordinate.x;
        }
        return res;
    }

    public static int getInstanceCounter() {
        return instance_counter;
    }

    public boolean equalsCoordinate(Coordinate that) {
        if (that == null) {
            return false;
        }
        return this.x == that.x && this.y == that.y;
    }

    @Override
    public int hashCode() {
        return (int)((long) x * y) % MOD;
    }

    @Override
    public String toString() {
        return x + " " + y;
    }
}
