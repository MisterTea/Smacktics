using System;
using System.Collections.Generic;

public class AbilityType : EntityType
{
    public int horizontalRange = 1;
    public int verticalRange = 1;
    public int splashRange = 0;
    public int damage = 1;

    public AbilityType() : base("") { }

    public AbilityType(
    string name
    ) : base(name)
    {
    }
}

public enum AbilityStatus
{
    Normal,
    Incapacitated,
    Emerging,
}

public class Ability : Entity
{
    public Guid _type;
    public AbilityType type
    {
        get { return G.i.settings.abilityTypes[this._type]; }
        private set { }
    }

    public override Coord location
    {
        get
        {
            return new Coord(0, 0, 0);
        }
        set
        {
            throw G.i.exception("Tried to set location of ability");
        }
    }

    public int cooldown = 0;

    public Ability(Map map, AbilityType type) : base(map, type.name, new Coord(0, 0, 0))
    {
        this._type = type.id;
    }
}
