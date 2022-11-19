using Godot;
using System;
using System.Collections.Generic;

public partial class ArenaGridMap : TerrainSerializationGridMap
{
	public Map? map = null;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	public static Vector3 coordToPosition(Coord c) {
		return new Vector3(c.col + 0.5f, (c.floor / 2.0f) - 0.25f, c.row + 0.5f);
	}
	
	public static Vector3 floorIntersect(float height, Vector3 origin, Vector3 direction) {
		var t = (height - origin.y) / direction.y;
		return origin + (direction * t);
	}
	
	public List<Coord> pickWalkableCells(Vector3 origin, Vector3 direction) {
		List<Coord> retval = new();
		if (map == null) {
			return retval;
		}
		foreach (Coord coord in this.map.walkableCoords.Keys) {
			var coordCenterPosition = coordToPosition(coord);
			var t = (coordCenterPosition.y - origin.y) / direction.y;
			var rayOnCoordFloor = origin + (direction * t);
			if (rayOnCoordFloor.DistanceTo(coordCenterPosition) < 0.5) {
				GD.Print($"Walkable: {rayOnCoordFloor} {coord} {rayOnCoordFloor.DistanceTo(coordCenterPosition)}");
				retval.Add(coord);
			}
		}
		return retval;
	}

	public Coord? pickNearestWalkableCell(Vector3 origin, Vector3 direction) {
		Coord? retval = null;
		if (map == null) {
			return retval;
		}
		double distance = -1.0;
		foreach (Coord coord in this.map.walkableCoords.Keys) {
			var coordCenterPosition = coordToPosition(coord);
			var rayOnCoordFloor = floorIntersect(coordCenterPosition.y, origin, direction);
			var distanceToCoordCenter = rayOnCoordFloor.DistanceTo(coordCenterPosition);
			if (retval == null || distanceToCoordCenter < distance) {
				GD.Print($"Walkable: {rayOnCoordFloor} {coord} {rayOnCoordFloor.DistanceTo(coordCenterPosition)}");
				retval = coord;
				distance = distanceToCoordCenter;
			}
		}
		if (retval == null) {
			throw G.i.exception("Somehow didn't find any walkable cells");
		}
		return retval;
	}
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
