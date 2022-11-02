public class UnitType : EntityType
{
    public int armor = 0;
    public int health = 1;
    public int movementRange = 1;
    public int jumpHeight = 1;
    public int attackHorizontalRange = 1;
    public int attackVerticalRange = 1;
    public int damage = 1;

    public UnitType(
    string name
    ) : base(name)
    {
    }
}

public class Unit : Entity
{
    public Guid _type;
    public UnitType type
    {
        get { return Globals.Instance.settings.unitTypes[this._type]; }
        private set { }
    }
    public int _health;
    public int health
    {
        get
        {
            return _health;
        }
        set
        {
            var oldHealth = _health;
            _health = value;
            if (!this.targetable)
            {
                Console.WriteLine($"{this.name} has been incapacitated!");
            }
            else
            {
                Console.WriteLine($"{this.name} takes {oldHealth - value} damage ({value} health remaining)!");
            }
        }
    }

    public Guid teamId;

    public bool targetable
    {
        get
        {
            return this.health > 0;
        }
    }

    public bool canAct
    {
        get
        {
            return this.health > 0;
        }
    }


    public Unit(Map map, string name, Coord location, UnitType type, Guid teamId) : base(map, name, location)
    {
        this._type = type.id;
        this._health = this.type.health;
        this.teamId = teamId;
    }
}


