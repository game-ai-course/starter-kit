using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace bot
{
    public class VD : IEquatable<VD>
    {
        public static readonly VD Zero = new VD(0, 0);

        public readonly double X;
        public readonly double Y;

        public static VD Parse(string s)
        {
            var parts = s.Split(' ');
            return new VD(double.Parse(parts[0]), double.Parse(parts[1]));
        }

        public VD(double x, double y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(VD other)
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
            return Equals((VD)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public static bool operator ==(VD left, VD right) => Equals(left, right);
        public static bool operator !=(VD left, VD right) => !Equals(left, right);

        public double Len2 => X * X + Y * Y;
        public static readonly VD None = new VD(-1, -1);
        public static readonly VD Up = new VD(0, -1);
        public static readonly VD Down = new VD(0, 1);
        public static readonly VD Left = new VD(-1, 0);
        public static readonly VD Right = new VD(1, 0);

        public static readonly VD[] Directions2 = { new VD(1, 0), new VD(0, 1) }; 
        public static readonly VD[] Directions4 = { new VD(1, 0), new VD(0, 1), new VD(-1, 0), new VD(0, -1) }; 
        public static readonly VD[] Directions5 = { Zero, new VD(1, 0), new VD(0, 1), new VD(-1, 0), new VD(0, -1) }; 
        public static readonly VD[] Directions8 = {
            new VD(-1, -1), new VD(0, -1), new VD(1, -1), 
            new VD(-1, 0), new VD(0, 0), new VD(1, 0), 
            new VD(-1, 1), new VD(0, 1), new VD(1, 1), 
        }; 

        public static readonly VD[] Directions9 = {
            Zero,
            new VD(-1, -1), new VD(0, -1), new VD(1, -1), 
            new VD(-1, 0), new VD(0, 0), new VD(1, 0), 
            new VD(-1, 1), new VD(0, 1), new VD(1, 1), 
        }; 

        public override string ToString()
        {
            return $"{X.ToString(CultureInfo.InvariantCulture)} {Y.ToString(CultureInfo.InvariantCulture)}";
        }

        public static VD operator +(VD a, VD b) => new VD(a.X + b.X, a.Y + b.Y);
        public static VD operator -(VD a, VD b) => new VD(a.X - b.X, a.Y - b.Y);
        public static VD operator -(VD a) => new VD(-a.X, -a.Y);
        public static VD operator *(VD a, int k) => new VD(k * a.X, k * a.Y);
        public static VD operator *(int k, VD a) => new VD(k * a.X, k * a.Y);
        public static VD operator /(VD a, int k) => new VD(a.X / k, a.Y / k);
        public double ScalarProd(VD b) => X * b.X + Y * b.Y;
        public double VectorProd(VD b) => X * b.Y - Y * b.X;

        public double Dist2To(VD point) => (this - point).Len2;

        public double DistTo(VD b) => Math.Sqrt(Dist2To(b));
        
        public double GetCollisionTime(VD speed, VD other, double radius) {
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
    

        public double GetAngleTo(VD p2)
        {
            var (x, y) = p2;
            return Math.Atan2(y-Y, x-X);
        }

        public void Deconstruct(out double x, out double y)
        {
            x = X;
            y = Y;
        }

        public double MDistTo(VD v2)
        {
            var (x, y) = v2;
            return Math.Abs(x-X) + Math.Abs(y-Y);
        }

        public double MLen =>  Math.Abs(X) + Math.Abs(Y);

        public double CDistTo(VD v2)
        {
            var (x, y) = v2;
            return Math.Max(Math.Abs(x-X), Math.Abs(y-Y));
        }

        public double CLen => Math.Max(Math.Abs(X), Math.Abs(Y));

        public bool InRange(int width, int height)
        {
            return X >= 0 && X < width && Y >= 0 && Y < height;
        }
    }
}