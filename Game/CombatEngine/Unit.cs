using System;
using System.Collections.Generic;

public class UnitType : EntityType
{
    public int armor = 0;
    public int health = 1;
    public int movementRange = 1;
    public int jumpHeight = 2;
    public List<Guid> abilities = new();

    public UnitType() : base("") { }

    public UnitType(
    string name
    ) : base(name)
    {
    }
}

public enum UnitStatusType
{
    Normal,
    Incapacitated,
    Emerging,
}

public record UnitStatus
{
    public UnitStatusType type;
    public int? duration = null;
}

public class Unit : Entity
{
    public Guid _type;
    public UnitType type
    {
        get { return G.i.settings.unitTypes[this._type]; }
        private set { }
    }

    public Dictionary<UnitStatusType, UnitStatus> statuses = new();
    public void addStatus(UnitStatus status)
    {
        map.events.Add(new MapEvent()
        {
            eventType = MapEventType.ChangeStatus,
            statusChange = new() {
                {id, status }
            }
        });
        G.i.log($"{this.name} has status {status}.");
        statuses[status.type] = status;
    }

    public List<Ability> abilities = new();

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
            if (oldHealth > 0 && _health <= 0)
            {
                addStatus(new UnitStatus() { type = UnitStatusType.Incapacitated });
            }
            else
            {
                G.i.log($"{this.name} takes {oldHealth - value} damage ({value} health remaining)!");
            }
        }
    }

    public Guid teamId;

    public Team team
    {
        get
        {
            return this.map.teams[teamId];
        }
    }

    public bool targetable
    {
        get
        {
            return !incapacitated;
        }
    }

    public bool incapacitated
    {
        get
        {
            return this.statuses.ContainsKey(UnitStatusType.Incapacitated);
        }
    }

    public bool canAct
    {
        get
        {
            return this.health > 0;
        }
    }

    public HashSet<Coord>? moveCoords = null;


    public Unit(Map map, string name, Coord location, UnitType type, Guid teamId) : base(map, name, location)
    {
        this._type = type.id;
        this._health = this.type.health;
        this.teamId = teamId;

        foreach (var abilityType in type.abilities)
        {
            abilities.Add(new Ability(this.map, G.i.settings.abilityTypes[abilityType]));
        }
    }

    public bool hasAbility()
    {
        foreach (var ability in abilities)
        {
            if (ability.cooldown == 0)
            {
                return true;
            }
        }
        return false;
    }

    public bool hasAction()
    {
        return moveCoords != null || hasAbility();
    }

    public void beginRound()
    {
        moveCoords = new HashSet<Coord>(this.map.getCoordsInRange(this.location, this.type.movementRange, this.type.jumpHeight).Keys);
        foreach (var ability in abilities)
        {
            ability.cooldown = Math.Max(0, ability.cooldown - 1);
        }
    }

    public void moveTo(Coord destination, List<Coord> path)
    {
        G.i.log($"Moved {this.name} from {this.location} to {destination} using {string.Join(",", path)}.");
        this.map.events.Add(new MapEvent()
        {
            eventType = MapEventType.Movement,
            source = this,
            targetLocation = destination,
            path = path,
        });
        this.location = destination;
    }
}


