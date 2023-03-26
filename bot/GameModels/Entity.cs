namespace bot
{
    public class Entity
    {
        public int Id { get; set; }
        public V Pos { get; set; }
        public EntityType Item { get; set; }

        public Entity(int id, V pos, EntityType item)
        {
            Id = id;
            Pos = pos;
            Item = item;
        }
    }
}