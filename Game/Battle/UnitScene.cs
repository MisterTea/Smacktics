using Godot;
using System;
using System.Collections.Generic;

public partial class UnitScene : EventFinishedHandler
{
	public Unit? unit;
	public Node3D? mesh;
	public List<Coord>? path = null;
	public double timeUntilNextMove = 0.0;
	public double startWalkingDelay = 0.0;
	const double MOVE_TIME = 0.1;
	

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}
	
	public UnitScene init(Unit unit) {
		this.unit = unit;
		this.mesh = this.GetNode<Node3D>("Mesh");
		this.Position = ArenaGridMap.coordToPosition(unit.location);
		return this;
	}
	
	public void setPosition(Coord c) {
		this.Position = ArenaGridMap.coordToPosition(c);
	}
	
	public void followPath(List<Coord> path) {
		this.path = new List<Coord>(path);
		this.startWalkingDelay = 0.33;
		this.timeUntilNextMove = MOVE_TIME;
		this.finishedEvent = false;
	}
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (this.path != null) {
			if (this.startWalkingDelay > 0.0) {
				this.startWalkingDelay -= delta;
				return;
			}

			this.timeUntilNextMove -= delta;
			
			var fracDone = (MOVE_TIME - this.timeUntilNextMove) / MOVE_TIME;
			this.Position = ArenaGridMap.coordToPosition(this.path[0]).Lerp(ArenaGridMap.coordToPosition(this.path[1]), (float)fracDone);

			if (this.timeUntilNextMove <= 0.0) {
				this.setPosition(this.path[1]);
				this.path.RemoveAt(0);
				this.timeUntilNextMove = MOVE_TIME;
				if (this.path.Count == 1) {
					this.path = null;
					this.finishedEvent = true;
				}
			}
		}
	}
}
