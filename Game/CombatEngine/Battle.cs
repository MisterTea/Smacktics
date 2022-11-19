using System;
using System.Collections.Generic;

public class Battle
{
    Map map;

    List<Guid> teamOrder;

    int currentTeamIndex = 0;

    public Battle(Map map)
    {
        this.map = map;
        this.teamOrder = new List<Guid>(map.teams.Keys);
    }

    public Team currentTeam
    {
        get
        {
            return this.map.teams[this.teamOrder[this.currentTeamIndex]];
        }
    }

    public List<Unit> activeUnits
    {
        get
        {
            List<Unit> retval = new();
            foreach (var unit in map.units.Values)
            {
                if (unit.teamId == currentTeam.id)
                {
                    retval.Add(unit);
                }
            }
            return retval;
        }
    }

    public bool finished
    {
        get
        {
            int numTeamsAlive = 0;
            foreach (var teamEntry in this.map.teams.Values)
            {
                foreach (var unit in this.map.unitsForTeam(teamEntry))
                {
                    if (!unit.incapacitated)
                    {
                        numTeamsAlive += 1;
                        break;
                    }
                }
            }
            return numTeamsAlive <= 1;
        }
    }

    public void advanceTeam()
    {
        this.currentTeamIndex = (this.currentTeamIndex + 1) % this.teamOrder.Count;
    }

    public void beginRound()
    {
        Team team = currentTeam;
        foreach (var unitEntry in this.map.units)
        {
            if (unitEntry.Value.teamId == team.id)
            {
                unitEntry.Value.beginRound();
            }
        }
    }

    public void stepTeam()
    {
        Team team = currentTeam;

        if (team.playerControlled)
        {
            throw G.i.exception("Tried to use AI on a player team");
        }

        foreach (var unitEntry in this.map.units)
        {
            if (unitEntry.Value.teamId == team.id)
            {
                stepUnit(unitEntry.Value);
            }
        }

        advanceTeam();
    }

    void stepUnit(Unit unit)
    {
        if (unit.team.playerControlled)
        {
            throw G.i.exception("Tried to use AI on a player team");
        }

        if (!unit.canAct)
        {
            return;
        }
        G.i.log($"{unit.name}'s turn!");

        // TODO: Pick the best ability when there are multiple
        Ability unitAbility = unit.abilities[0];

        Unit? nearestEnemy = this.map.findNearestTargetableEnemyIncludingMovement(unit, unitAbility);
        if (nearestEnemy == null)
        {
            G.i.log("No one to attack");
            return;
        }

        this.map.moveToAttackRange(unit, unitAbility, nearestEnemy.location);

        if (this.map.attackCoords(nearestEnemy.location, unit, unitAbility).Contains(unit.location))
        {
            this.attack(unit, unitAbility, nearestEnemy);
        }
    }

    public void attack(Unit source, Ability ability, Unit target)
    {
        // Attack
        int damage = Math.Max(1, ability.type.damage - target.type.armor);
        G.i.log($"{source.name} hits {target.name} for {damage}!");
        map.events.Add(new MapEvent()
        {
            source = source,
            targetUnit = target,
            eventType = MapEventType.Ability,
            healthChange = new Dictionary<Guid, int>() { { target.id, -damage } }
        });
        source.moveCoords = null;
        ability.cooldown += 1;
        target.health -= damage;
    }
}
