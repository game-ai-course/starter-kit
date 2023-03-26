namespace bot
{
    public class Robot : Entity
    {
        public Robot(int id, V pos, EntityType item) : base(id, pos, item)
        { }

        public bool IsDead() => Pos.X < 0 && Pos.Y < 0;
    }
}