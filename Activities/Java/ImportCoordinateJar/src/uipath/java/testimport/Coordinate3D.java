package uipath.java.testimport;

import uipath.java.test.Coordinate;

public class Coordinate3D extends Coordinate {
    private double z;
    private TransformCoordinate lambda;

    public Coordinate3D() {

    }

    public Coordinate3D(Coordinate coordinate) {
        super(coordinate.getX(), coordinate.getY());
        z = 0;
    }

    public Coordinate3D(Coordinate coordinate, double z) {
        super(coordinate.getX(), coordinate.getY());
        this.z = z;
    }

    public Coordinate3D(double x, float y, double z) {
        super(x, y);
        this.z = z;
    }

    public double getZ() {
        return z;
    }

    public double getCoordinateSum() {
        return super.getCoordinateSum() + z;
    }

    public boolean equals2DCoordinate(Coordinate that) {
        return super.equalsCoordinate(that);
    }

    public Coordinate3D ApplyTransform(TransformCoordinate lambda, int x, int y, int z) {
        return lambda.transform(this, x, y, z);
    }

    public static TransformCoordinate GetLambda() {
        return (Coordinate3D coordinate,int xx, int yy, int zz) ->
        {
            Coordinate3D newCoordinate = new Coordinate3D();
            newCoordinate.x = (double) coordinate.x + xx;
            newCoordinate.y = (float) coordinate.y + yy;
            newCoordinate.z = (double) coordinate.z + zz;
            return newCoordinate;
        };
    }
}

@FunctionalInterface
interface TransformCoordinate {
    Coordinate3D transform(Coordinate3D coordinate, int x, int y, int z);
}