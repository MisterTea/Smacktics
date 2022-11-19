using System.Collections.Generic;
using System;
using System.Linq;

public enum MapEventType
{
    Empty,
    Movement,
    Ability,
    ChangeStatus,
}

public enum Height
{
    None,
    Single,
    Double,
}

public enum Slope
{
    None,
    RowPositive,
    RowNegative,
    ColPositive,
    ColNegative,
}

public class BlockType : EntityType
{
    public Height height = Height.None;
    public Slope slope = Slope.None;

    public BlockType() : base("") { }

    public BlockType(Height height, Slope slope, string name) : base(name)
    {
        this.height = height;
        this.slope = slope;
    }
}

public record MapEvent
{
    public MapEventType eventType;
    public Unit? source = null;
    public Unit? targetUnit = null;
    public Coord? targetLocation = null;
    public Coord? target
    {
        get
        {
            if (targetLocation != null && targetUnit != null)
            {
                throw G.i.exception("Invalid target location");
            }
            if (targetLocation != null)
            {
                return targetLocation;
            }
            if (targetUnit == null)
            {
                return null;
            }
            return targetUnit.location;
        }
    }
    public List<Coord>? path = null;
    public Dictionary<Guid, int> healthChange = new();
    public Dictionary<Guid, UnitStatus> statusChange = new();
}

public enum CoverType
{
    None = 0,
    Half = 1,
    Full = 2,
}

public record Walkable
{
    public CoverType[] coverType = new CoverType[4] { CoverType.None, CoverType.None, CoverType.None, CoverType.None };
}

public record Map
{
    public const int MAX_HEIGHT = 1024;

    public Dictionary<Coord, Walkable> walkableCoords = new();

    public Dictionary<Coord, Guid> blocks;

    public Dictionary<Guid, Team> teams = new Dictionary<Guid, Team>();

    public Dictionary<Guid, Unit> units = new Dictionary<Guid, Unit>();

    public List<MapEvent> events = new();

    public Map(Dictionary<Coord, Guid> blocks)
    {
        this.blocks = blocks;
        computeWalkable();
    }

    public BlockType? getBlock(Coord c)
    {
        if (!blocks.ContainsKey(c))
        {
            return null;
        }
        return G.i.settings.blocks[blocks[c]];
    }

    private void computeWalkable()
    {
        this.walkableCoords.Clear();

        foreach (var coordBlockPair in blocks)
        {
            var coord = coordBlockPair.Key;
            var block = getBlock(coord)!;

            if (block.height == Height.None)
            {
                // Can't walk on air
                continue;
            }

            var startWalkable = 2;

            if (block.height == Height.Single)
            {
                startWalkable = 1;
            }
            else
            {
                if (block.height != Height.Double)
                {
                    throw G.i.exception("Oops");
                }
            }

            var endWalkable = startWalkable + 4;

            var canWalk = true;

            for (var a = startWalkable; a <= endWalkable; a++)
            {
                if (blocks.ContainsKey(coord + new Coord(0, 0, a)))
                {
                    var aboveBlock = G.i.settings.blocks[blocks[coord + new Coord(0, 0, a)]];
                    if (aboveBlock.height != Height.None)
                    {
                        // Not walkable (collide with roof)
                        canWalk = false;
                        break;
                    }
                }
            }

            if (canWalk)
            {
                var walkableCoord = coord + new Coord(0, 0, startWalkable);

                var walkable = new Walkable();

                // Figure out cover situation
                for (int d = 0; d < 4; d++)
                {
                    var adjCoord = walkableCoord + Coord.fromDirection((Direction)d);

                    var blockCover = CoverType.None;

                    if (blocks.ContainsKey(adjCoord))
                    {

                        switch (getBlock(adjCoord)!.height)
                        {
                            case Height.None:
                                blockCover = CoverType.None;
                                break;
                            case Height.Single:
                                blockCover = CoverType.Half;
                                break;
                            case Height.Double:
                                blockCover = CoverType.Full;
                                break;
                        }
                    }

                    if (blocks.ContainsKey(adjCoord + new Coord(0, 0, -1)))
                    {
                        if (getBlock(adjCoord + new Coord(0, 0, -1))!.height == Height.Double)
                        {
                            if (blockCover == CoverType.None)
                            {
                                blockCover = CoverType.Half;
                            }
                        }
                    }

                    walkable.coverType[d] = blockCover;
                }

                // Walkable
                this.walkableCoords.Add(walkableCoord, walkable);
            }
        }
    }

    public List<Unit> findAllTargetableEnemies(Unit attacker, Ability ability)
    {
        Guid teamId = attacker.teamId;
        List<Unit> retval = new();
        foreach (var target in this.units.Values)
        {
            if (target.teamId != teamId && target.targetable)
            {
                // Get all squares we can hit the target from
                var targetAttackLocations = this.attackCoords(target.location, attacker, ability);

                if (targetAttackLocations.Contains(attacker.location))
                {
                    retval.Add(target);
                }
            }
        }
        return retval;
    }

    public Unit? findNearestTargetableEnemyIncludingMovement(Unit attacker, Ability ability)
    {
        Guid teamId = attacker.teamId;
        Unit? retval = null;
        int smallestDist = 0;
        foreach (var target in this.units.Values)
        {
            if (target.teamId != teamId && target.targetable)
            {
                // Get all squares we can hit the target from
                var targetAttackLocations = this.attackCoords(target.location, attacker, ability);

                // Get the shortest distance to one of those squares
                var distancePathPair = this.shortestDistance(attacker.location, new HashSet<Coord>(targetAttackLocations), attacker.type.jumpHeight);
                if (distancePathPair != null && (retval == null || distancePathPair.Value.Item1 < smallestDist))
                {
                    var distanceToAttackLocation = distancePathPair.Value.Item1;
                    smallestDist = distanceToAttackLocation;
                    retval = target;
                }
            }
        }
        if (retval != null)
        {
            G.i.log($"Found enemy {retval.name}");
        }
        return retval;
    }

    public Dictionary<Coord, Coord?> getCoordsInRange(Coord center, int range, int jumpHeight)
    {
        Dictionary<Coord, Coord?> visited = new Dictionary<Coord, Coord?>();
        visited.Add(center, null);

        LinkedList<Tuple<Coord, int>> queue = new LinkedList<Tuple<Coord, int>>();
        queue.AddLast(new Tuple<Coord, int>(center, 0));

        while (queue.Count > 0)
        {
            var entry = queue.First!;
            var currentPoint = entry.ValueRef.Item1;
            var distanceTraveled = entry.ValueRef.Item2;
            queue.RemoveFirst();

            foreach (var p in this.adjacentMovementCoords(currentPoint, 1, jumpHeight))
            {
                var newDistance = distanceTraveled + 1;
                if (newDistance > range)
                {
                    continue;
                }
                if (visited.ContainsKey(p))
                {
                    continue;
                }
                visited.Add(p, currentPoint);
                queue.AddLast(new Tuple<Coord, int>(p, newDistance));
            }
        }

        return visited;
    }

    public List<Coord> adjacentMovementCoords(Coord point, int horizontalRange, int verticalRange, HashSet<Coord>? allowedWalkable = null)
    {
        bool allowDiagonal = (horizontalRange > 1);
        if (!this.walkableCoords.ContainsKey(point))
        {
            throw G.i.exception($"Tried to get adjacent coords to a non walkable coord {point}");
        }
        List<Coord> retval = new();
        for (int rMod = -1 * horizontalRange; rMod <= 1 * horizontalRange; rMod++)
        {
            for (int cMod = -1 * horizontalRange; cMod <= 1 * horizontalRange; cMod++)
            {
                if ((rMod == 0 && cMod == 0) || (rMod != 0 && cMod != 0 && !allowDiagonal))
                {
                    continue;
                }
                for (int fMod = -1 * verticalRange; fMod <= 1 * verticalRange; fMod++)
                {
                    var c = new Coord(point.row + rMod, point.col + cMod, point.floor + fMod);
                    if (allowedWalkable != null)
                    {
                        if (allowedWalkable.Contains(c))
                        {
                            retval.Add(c);
                        }
                    }
                    else
                    {
                        if (this.walkableCoords.ContainsKey(c))
                        {
                            retval.Add(c);
                        }
                    }
                }
            }
        }
        return retval;
    }

    public List<Coord> attackCoords(Coord point, Unit unit, Ability ability)
    {
        return this.adjacentMovementCoords(point, ability.type.horizontalRange, ability.type.verticalRange);
    }

    public void moveToAttackRange(Unit unit, Ability ability, Coord targetLocation)
    {
        // Get adjacent coords to target
        var adjCoordList = this.attackCoords(targetLocation, unit, ability);

        foreach (var adjCoord in adjCoordList)
        {
            if (unit.location == adjCoord)
            {
                // Already at the target
                return;
            }
        }

        // Get all locations that the unit can move to
        Coord? nearestCoord = null;
        int nearestDistance = 0;
        List<Coord>? nearestCoordPath = null;
        foreach (var adjCoord in adjCoordList)
        {
            if (unit.moveCoords!.Contains(adjCoord))
            {
                var movement = this.movementDistance(unit.location, adjCoord, unit.type.jumpHeight);
                if (movement == null)
                {
                    throw G.i.exception("Moverment shouldn't be null but was");
                }
                var distPathPair = movement.Value;
                if (nearestCoord == null || distPathPair.Item1 < nearestDistance)
                {
                    nearestCoord = adjCoord;
                    nearestDistance = distPathPair.Item1;
                    nearestCoordPath = distPathPair.Item2;
                }
            }
        }

        if (nearestCoord != null)
        {
            if (nearestCoordPath == null)
            {
                throw G.i.exception("Somehow coord path was null");
            }
            // Unit can move adjacent to target, move there.
            unit.moveTo(nearestCoord.Value, nearestCoordPath);
            return;
        }

        foreach (var coordInRange in unit.moveCoords!)
        {
            var distPathPair = this.movementDistance(coordInRange, targetLocation, unit.type.jumpHeight);
            if (distPathPair != null && (nearestCoord == null || distPathPair.Value.Item1 < nearestDistance))
            {
                nearestCoord = coordInRange;
                nearestDistance = distPathPair.Value.Item1;
                nearestCoordPath = this.movementDistance(unit.location, coordInRange, unit.type.jumpHeight, unit.moveCoords)!.Value.Item2;
            }
        }

        if (nearestCoord == null || nearestCoordPath == null)
        {
            throw G.i.exception("Tried to move to a target that we couldn't get to");
        }

        // Move unit to nearest coord
        unit.moveTo(nearestCoord.Value, nearestCoordPath);
    }

    public (int, List<Coord>)? movementDistance(Coord source, Coord dest, int jumpHeight, HashSet<Coord>? allowedWalkable = null)
    {
        Coord[] dests = { dest, };
        return shortestDistance(source, new HashSet<Coord>(dests), jumpHeight, allowedWalkable);
    }

    public (int, List<Coord>)? shortestDistance(Coord source, HashSet<Coord> destinations, int jumpHeight, HashSet<Coord>? allowedWalkable = null)
    {
        Dictionary<Coord, Coord?> visited = new Dictionary<Coord, Coord?>();
        visited.Add(source, null);

        LinkedList<Tuple<Coord, int>> queue = new LinkedList<Tuple<Coord, int>>();
        queue.AddLast(new Tuple<Coord, int>(source, 0));

        while (queue.Count > 0)
        {
            var entry = queue.First!;
            var currentPoint = entry.ValueRef.Item1;
            var distanceTraveled = entry.ValueRef.Item2;
            queue.RemoveFirst();
            if (destinations.Contains(currentPoint))
            {
                var path = Coord.pathFromBackReferences(visited, currentPoint);
                return (distanceTraveled, path.ToList());
            }

            foreach (var p in this.adjacentMovementCoords(currentPoint, 1, jumpHeight, allowedWalkable))
            {
                if (visited.ContainsKey(p))
                {
                    continue;
                }
                visited.Add(p, currentPoint);
                var newDistance = distanceTraveled + 1;
                queue.AddLast(new Tuple<Coord, int>(p, newDistance));
            }
        }

        G.i.log($"Could not find a path from {source} to {destinations.ToList()}!");
        return null;
    }

    public List<Coord> getAttackSourceCoords(Coord target, int horizontalRange, int verticalRange)
    {
        var retval = new List<Coord>();
        foreach (var coord in this.walkableCoords.Keys)
        {
            if (coord.horizontalDistance(target) <= horizontalRange && coord.verticalDistance(target) <= verticalRange)
            {
                retval.Add(coord);
            }
        }
        return retval;
    }

    public Coord raiseToNearestWalkableFloor(Coord coord)
    {
        Coord originalCoord = coord;
        while (true)
        {
            if (this.walkableCoords.ContainsKey(coord))
            {
                G.i.log($"Raised from {originalCoord} to {coord}");
                return coord;
            }
            coord = coord.shift(new Coord(0, 0, 1));
            if (coord.floor == MAX_HEIGHT)
            {
                throw G.i.exception($"Could not find a walkabale floor on this coord: {coord}");
            }
        }
    }

    public List<Unit> unitsForTeam(Team team)
    {
        var retval = new List<Unit>();
        foreach (var unit in this.units.Values)
        {
            if (unit.teamId != team.id)
            {
                continue;
            }
            retval.Add(unit);
        }
        return retval;
    }

    public Unit unitForTeam(Team team)
    {
        foreach (var unit in this.units.Values)
        {
            if (unit.teamId != team.id)
            {
                continue;
            }
            return unit;
        }
        throw G.i.exception("Missing any unit for this team");
    }

    public Unit? unitFromCoord(Coord coord)
    {
        foreach (var unit in this.units.Values)
        {
            if (unit.location == coord)
            {
                return unit;
            }
        }
        return null;
    }
}
