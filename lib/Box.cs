namespace bot;

public record Box(V LeftTop, V RightBottom)
{
    public int Left => LeftTop.X;
    public int Top => LeftTop.Y;
    public int Right => RightBottom.X;
    public int Bottom => RightBottom.Y;
    public int Width => Right - Left + 1;
    public int Height => Bottom - Top + 1;
        
    public Box(int x1, int y1, int x2, int y2)
        : this(new V(x1, y1), new V(x2, y2))
    {
    }
        
    public override string ToString() => $"{LeftTop} {RightBottom}";

    public V Middle => (LeftTop + RightBottom) / 2;

    public Box Intersect(Box other)
    {
        var leftTop = new V(Max(LeftTop.X, other.LeftTop.X), Max(LeftTop.Y, other.LeftTop.Y));
        var rightBottom = new V(Min(RightBottom.X, other.RightBottom.X),
            Min(RightBottom.Y, other.RightBottom.Y));
        return new Box(leftTop, rightBottom);
    }

    public static Box operator +(Box box, V v) => new(box.LeftTop + v, box.RightBottom + v);

    public bool IsSinglePoint => LeftTop.X == RightBottom.X && LeftTop.Y == RightBottom.Y;
    
    public long Area =>
        (long)Max(0, RightBottom.X - LeftTop.X + 1) * Max(0, RightBottom.Y - LeftTop.Y + 1);

    public static Box FromPoint(V p) => new(p, p);

    public Box AddMargin(int size) => new(LeftTop - new V(size, size), RightBottom + new V(size, size));

    public Box FlipHorizontal(int imageOfZero)
    {
        var rightTop = LeftTop.FlipHorizontal(imageOfZero);
        var leftBottom = RightBottom.FlipHorizontal(imageOfZero);
        return new Box(new V(leftBottom.X, rightTop.Y), new V(rightTop.X, leftBottom.Y));
    }

    public long Dist2To(V pos)
    {
        var dx = Max(0, Max(Left - pos.X, pos.X - Right));
        var dy = Max(0, Max(Top - pos.Y, pos.Y - Bottom));
        return dx * dx + dy * dy;
    }
    public double DistTo(V pos) => Sqrt(Dist2To(pos));
        
    public V Bound(V pos) => new(Max(Left, Min(Right, pos.X)), Max(Top, Min(Bottom, pos.Y)));

    public bool Contains(V pos) => Left <= pos.X && pos.X <= Right && Top <= pos.Y && pos.Y <= Bottom;

    public long MaxDist2To(V pos)
    {
        var dx = Max(Abs(pos.X - Left), Abs(Right - pos.X));
        var dy = Max(Abs(pos.Y - Top), Abs(Bottom - pos.Y));
        return dx * dx + dy * dy;
    }

    public static Box IntersectAll(IEnumerable<Box> boxes) => 
        boxes.Aggregate(Box.MaxValue, (current, box) => current.Intersect(box));

    public static readonly Box MaxValue = new Box(int.MinValue, int.MinValue, int.MaxValue, int.MaxValue);
}