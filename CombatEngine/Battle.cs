public class Battle
{
    Map map;

    public Battle(Map map)
    {
        this.map = map;
    }

    public void step()
    {
        foreach (var teamEntry in this.map.teams)
        {
            stepTeam(teamEntry.Value);
        }
    }

    void stepTeam(Team team)
    {
        foreach (var unitEntry in this.map.units)
        {
            if (unitEntry.Value.teamId == team.id)
            {
                stepUnit(unitEntry.Value);
            }
        }
    }

    void stepUnit(Unit unit)
    {
        if (!unit.canAct)
        {
            return;
        }
        Console.WriteLine($"{unit.name}'s turn!");
        Unit? nearestEnemy = this.map.findNearestTargetableEnemy(unit, unit.location);
        if (nearestEnemy == null)
        {
            Console.WriteLine("Battle Over");
            Environment.Exit(0);
        }

        this.map.moveToAttackRange(unit, nearestEnemy.location);

        if (this.map.attackCoords(nearestEnemy.location, unit).Contains(unit.location))
        {
            // Attack
            int damage = Math.Max(1, unit.type.damage - nearestEnemy.type.armor);
            Console.WriteLine($"{unit.name} hits {nearestEnemy.name} for {damage}!");
            nearestEnemy.health -= damage;
        }
    }
}
