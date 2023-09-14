using System;

namespace signal_detection;

public struct PointD
{
    public double X, Y;

    public PointD(double x, double y)
    {
        X = x;
        Y = y;
    }

    public PointD ToPoint() => new(X, Y);

    public PointD Rotate(double angle) => new(X * Math.Cos(angle) - Y * Math.Sin(angle), X * Math.Sin(angle) + Y * Math.Cos(angle));
}