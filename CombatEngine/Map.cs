public record Map
{
    public int rows;
    public int cols;
    public int floors;

    public HashSet<Coord> walkableCoords;

    public Dictionary<Guid, Team> teams = new Dictionary<Guid, Team>();

    public Dictionary<Guid, Unit> units = new Dictionary<Guid, Unit>();

    public Map(int rows, int cols, int floors, HashSet<Coord> walkableCoords)
    {
        this.rows = rows;
        this.cols = cols;
        this.floors = floors;
        this.walkableCoords = walkableCoords;
    }

    public Unit? findNearestTargetableEnemy(Unit attacker, Coord location)
    {
        Guid teamId = attacker.teamId;
        Unit? retval = null;
        int smallestDist = 0;
        foreach (var target in this.units.Values)
        {
            if (target.teamId != teamId && target.targetable)
            {
                // Get all squares we can hit the target from
                var targetAttackLocations = this.attackCoords(target.location, attacker);

                // Get the shortest distance to one of those squares
                var distanceToAttackLocation = this.shortestDistance(attacker.location, new HashSet<Coord>(targetAttackLocations), attacker.type.jumpHeight);
                if (retval == null || distanceToAttackLocation < smallestDist)
                {
                    smallestDist = distanceToAttackLocation;
                    retval = target;
                }
            }
        }
        if (retval != null)
        {
            Console.WriteLine($"Moving to {retval.name}");
        }
        return retval;
    }

    public HashSet<Coord> getCoordsInRange(Coord center, int range, int jumpHeight)
    {
        HashSet<Coord> visited = new HashSet<Coord>();

        LinkedList<Tuple<Coord, int>> queue = new LinkedList<Tuple<Coord, int>>();
        queue.AddLast(new Tuple<Coord, int>(center, 0));

        while (queue.Count > 0)
        {
            var entry = queue.First();
            var currentPoint = entry.Item1;
            var distanceTraveled = entry.Item2;
            queue.RemoveFirst();
            if (visited.Contains(currentPoint))
            {
                continue;
            }
            visited.Add(currentPoint);

            foreach (var p in this.adjacentMovementCoords(currentPoint, 1, jumpHeight))
            {
                var newDistance = distanceTraveled + 1;
                if (newDistance > range)
                {
                    continue;
                }
                queue.AddLast(new Tuple<Coord, int>(p, newDistance));
            }
        }

        return visited;
    }

    public List<Coord> adjacentMovementCoords(Coord point, int horizontalRange, int verticalRange)
    {
        bool allowDiagonal = (horizontalRange > 1);
        if (!this.walkableCoords.Contains(point))
        {
            throw new InvalidOperationException($"Tried to get adjacent coords to a non walkable coord {point}");
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
                    if (this.walkableCoords.Contains(c))
                    {
                        retval.Add(c);
                    }
                }
            }
        }
        return retval;
    }

    public List<Coord> attackCoords(Coord point, Unit unit)
    {
        return this.adjacentMovementCoords(point, unit.type.attackHorizontalRange, unit.type.attackVerticalRange);
    }

    public void moveToAttackRange(Unit unit, Coord targetLocation)
    {
        // Get adjacent coords to target
        var adjCoordList = this.attackCoords(targetLocation, unit);

        foreach (var adjCoord in adjCoordList)
        {
            if (unit.location == adjCoord)
            {
                // Already at the target
                return;
            }
        }

        // Get all locations that the unit can move to
        var coordsInRange = this.getCoordsInRange(unit.location, unit.type.movementRange, unit.type.jumpHeight);

        Coord? nearestCoord = null;
        int nearestDistance = 0;
        foreach (var adjCoord in adjCoordList)
        {
            if (coordsInRange.Contains(adjCoord))
            {
                int dist = this.movementDistance(unit.location, adjCoord, unit.type.jumpHeight);
                if (nearestCoord == null || dist < nearestDistance)
                {
                    nearestDistance = dist;
                    nearestCoord = adjCoord;
                }
            }
        }

        if (nearestCoord != null)
        {
            // Unit can move adjacent to target, move there.
            Console.WriteLine($"Moved {unit.name} from {unit.location} to {nearestCoord} and can attack (target location {targetLocation}).");
            unit.location = nearestCoord;
            return;
        }

        foreach (var coordInRange in coordsInRange)
        {
            int dist = this.movementDistance(coordInRange, targetLocation, unit.type.jumpHeight);
            if (nearestCoord == null || dist < nearestDistance)
            {
                nearestDistance = dist;
                nearestCoord = coordInRange;
            }
        }

        if (nearestCoord == null)
        {
            throw new InvalidOperationException("Tried to move to a target that we couldn't get to");
        }

        // Move unit to nearest coord
        Console.WriteLine($"Moved {unit.name} from {unit.location} to {nearestCoord} but can't attack (target location {targetLocation}).");
        unit.location = nearestCoord;
    }

    public int movementDistance(Coord source, Coord dest, int jumpHeight)
    {
        Coord[] dests = { dest, };
        return this.shortestDistance(source, new HashSet<Coord>(dests), jumpHeight);
    }

    public int shortestDistance(Coord source, HashSet<Coord> destinations, int jumpHeight)
    {
        HashSet<Coord> visited = new HashSet<Coord>();

        LinkedList<Tuple<Coord, int>> queue = new LinkedList<Tuple<Coord, int>>();
        queue.AddLast(new Tuple<Coord, int>(source, 0));

        while (queue.Count > 0)
        {
            var entry = queue.First();
            var currentPoint = entry.Item1;
            var distanceTraveled = entry.Item2;
            queue.RemoveFirst();
            if (visited.Contains(currentPoint))
            {
                continue;
            }
            visited.Add(currentPoint);
            if (destinations.Contains(currentPoint))
            {
                return distanceTraveled;
            }

            foreach (var p in this.adjacentMovementCoords(currentPoint, 1, jumpHeight))
            {
                var newDistance = distanceTraveled + 1;
                queue.AddLast(new Tuple<Coord, int>(p, newDistance));
            }
        }

        throw new InvalidOperationException($"Could not find a path from {source} to {destinations.ToList()}!");
    }

    public List<Coord> getAttackSourceCoords(Coord target, int horizontalRange, int verticalRange)
    {
        var retval = new List<Coord>();
        foreach (var coord in this.walkableCoords)
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
        while (true)
        {
            if (this.walkableCoords.Contains(coord))
            {
                return coord;
            }
            coord = coord.shift(new Coord(0, 0, 1));
            if (coord.floor == this.floors)
            {
                throw new InvalidOperationException($"Could not find a walkabale floor on this coord: {coord}");
            }
        }
    }
}
