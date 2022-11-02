using System.Text.Json.Serialization;

public class EntityType
{
    public Guid id = Guid.NewGuid();
    public string name;

    public EntityType(string name)
    {
        this.name = name;
    }
}

public class Entity : EntityType
{
    public Coord _location;
    public Coord location
    {
        get
        {
            return _location;
        }
        set
        {
            if (!map.walkableCoords.Contains(value))
            {
                throw new InvalidOperationException($"Tried to set an entity location that isn't walkable: {value}");
            }
            _location = value;
        }
    }

    [JsonIgnore]
    public Map map;

    public Entity(Map map, string name, Coord location) : base(name)
    {
        this.map = map;
        this.name = name;
        this.location = location;
    }
}
