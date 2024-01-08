namespace bot;

public class Disk
{
    public Disk(V pos, int radius)
    {
        Radius = radius;
        Pos = pos;
    }

    public int Radius { get; }
    public V Pos { get; set; }

    public bool Intersect(Disk disk)
    {
        var minDist = disk.Radius + Radius;
        return minDist * minDist >= (Pos - disk.Pos).Len2;
    }

    public bool Contains(V point)
    {
        return (point - Pos).Len2 <= Radius * Radius;
    }

    public static Disk ParseDisk(string s)
    {
        var parts = s.Split(new []{','}).Select(int.Parse).ToList();
        return new Disk(new V(parts[0], parts[1]), parts[2]);
    }

    public override string ToString()
    {
        return $"[{Pos.X},{Pos.Y},{Radius}]";
    }
}