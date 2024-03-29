﻿namespace bot;

public class V : IEquatable<V>
{
    public static readonly V Zero = new V(0, 0);

    public readonly int X;
    public readonly int Y;

    public static V Parse(string s)
    {
        var parts = s.Split(' ');
        return new V(int.Parse(parts[0]), int.Parse(parts[1]));
    }

    public V(int x, int y)
    {
        X = x;
        Y = y;
    }
    public V(double x, double y)
        :this((int)Math.Round(x), (int)Math.Round(y))
    {
    }


    public bool Equals(V other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return X.Equals(other.X) && Y.Equals(other.Y);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;
        return Equals((V)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (X.GetHashCode() * 397) ^ Y.GetHashCode();
        }
    }

    public static bool operator ==(V left, V right) => Equals(left, right);
    public static bool operator !=(V left, V right) => !Equals(left, right);

    public long Len2 => (long)X * X + (long)Y * Y;
    public static readonly V None = new V(-1, -1);
    public static readonly V Up = new V(0, -1);
    public static readonly V Down = new V(0, 1);
    public static readonly V Left = new V(-1, 0);
    public static readonly V Right = new V(1, 0);

    public static readonly V[] Directions2 = { new V(1, 0), new V(0, 1) }; 
    public static readonly V[] Directions4 = { new V(1, 0), new V(0, 1), new V(-1, 0), new V(0, -1) }; 
    public static readonly V[] Directions5 = { Zero, new V(1, 0), new V(0, 1), new V(-1, 0), new V(0, -1) }; 
    public static readonly V[] Directions8 = {
        new V(-1, -1), new V(0, -1), new V(1, -1), 
        new V(-1, 0), new V(0, 0), new V(1, 0), 
        new V(-1, 1), new V(0, 1), new V(1, 1), 
    }; 

    public static readonly V[] Directions9 = {
        Zero,
        new V(-1, -1), new V(0, -1), new V(1, -1), 
        new V(-1, 0), new V(0, 0), new V(1, 0), 
        new V(-1, 1), new V(0, 1), new V(1, 1), 
    }; 

    public override string ToString()
    {
        return $"{X.ToString(CultureInfo.InvariantCulture)} {Y.ToString(CultureInfo.InvariantCulture)}";
    }

    public static V operator +(V a, V b) => new V(a.X + b.X, a.Y + b.Y);
    public static V operator -(V a, V b) => new V(a.X - b.X, a.Y - b.Y);
    public static V operator -(V a) => new V(-a.X, -a.Y);
    public static V operator *(V a, int k) => new V(k * a.X, k * a.Y);
    public static V operator *(int k, V a) => new V(k * a.X, k * a.Y);
    public static V operator /(V a, int k) => new V(a.X / k, a.Y / k);
    public long ScalarProd(V b) => X * b.X + Y * b.Y;
    public long VectorProd(V b) => X * b.Y - Y * b.X;

    public long Dist2To(V point) => (this - point).Len2;

    public double DistTo(V b) => Math.Sqrt(Dist2To(b));
        
    public double GetCollisionTime(V speed, V other, double radius) {
        if (DistTo(other) <= radius)
            return 0.0;

        if (speed.Equals(Zero))
            return double.PositiveInfinity;
        /*
         * x = x2 + vx * t
         * y = y2 + vy * t
         * x² + y² = radius²
         * ↓
         * (x2² + 2*vx*x2 * t + vx² * t²)  +  (y2² + 2*vy*y2 * t + vy² * t²) = radius²
         * ↓
         * t² * (vx² + vy²)  +  t * 2*(x2*vx + y2*vy) + x2² + y2² - radius² = 0
         */

        var x2 = X - other.X;
        var y2 = Y - other.Y;
        var vx = speed.X;
        var vy = speed.Y;

        var a = vx * vx + vy * vy;
        var b = 2.0 * (x2 * vx + y2 * vy);
        var c = x2 * x2 + y2 * y2 - radius * radius;
        var d = b * b - 4.0 * a * c;

        if (d < 0.0)
            return double.PositiveInfinity;

        var t = (-b - Math.Sqrt(d)) / (2.0 * a);
        return t <= 0.0 ? double.PositiveInfinity : t;
    }
    

    public double GetAngleTo(V p2)
    {
        var (x, y) = p2;
        return Math.Atan2(y-Y, x-X);
    }

    public void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }

    public static IEnumerable<V> AllInRange(int width, int height)
    {
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            yield return new V(x, y);
        }
    }

    public int MDistTo(V v2)
    {
        var (x, y) = v2;
        return Math.Abs(x-X) + Math.Abs(y-Y);
    }

    public int MLen =>  Math.Abs(X) + Math.Abs(Y);

    public int CDistTo(V v2)
    {
        var (x, y) = v2;
        return Math.Max(Math.Abs(x-X), Math.Abs(y-Y));
    }

    public int CLen => Math.Max(Math.Abs(X), Math.Abs(Y));
    public double Len => Math.Sqrt(Len2);

    public bool InRange(int width, int height)
    {
        return X >= 0 && X < width && Y >= 0 && Y < height;
    }
        
    public IEnumerable<V> Area9()
    {
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
            yield return new V(X + dx, Y + dy);
    }

    public IEnumerable<V> Area8()
    {
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
            if (dx != 0 || dy != 0)
                yield return new V(X + dx, Y + dy);
    }

    public IEnumerable<V> Area4()
    {
        yield return new V(X - 1, Y);
        yield return new V(X + 1, Y);
        yield return new V(X, Y - 1);
        yield return new V(X, Y + 1);
    }

    public IEnumerable<V> Area5()
    {
        yield return this;
        yield return new V(X - 1, Y);
        yield return new V(X + 1, Y);
        yield return new V(X, Y - 1);
        yield return new V(X, Y + 1);
    }

    public V Rescale(int len)
    {
        if (X==0 && Y==0) return this;
        var k = len / Len;
        return new V(X*k, Y*k);
    }

    public double AngleBetween(V dir1, V dir2)
    {
        var a = dir1 - this;
        var b = dir2 - this;
        var cos = a.ScalarProd(b) / Math.Sqrt(a.Len2 * b.Len2);
        return Math.Acos(cos);
    }
        
    public V FlipHorizontal(int imageOfZero = 0) => new V(imageOfZero - X, Y);
    public V FlipVertical(int imageOfZero = 0) => new V(X, imageOfZero - Y);

    public V BoundLenTo(int newLen) => Len2 <= newLen * newLen ? this : Rescale(newLen);

    public static V FromPolar(double angle, int len) => new(len * Math.Cos(angle), len * Math.Sin(angle));
}