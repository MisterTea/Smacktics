using Godot;
using System;

public partial class BulletScene : Node3D
{
	[Export]
	public Vector3 SourcePosition = new Vector3(0,0,0);

	[Export]
	public Vector3 DestinationPosition = new Vector3(0,0,0);

	[Export]
	public double timeToTravel = 0.2;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (timeToTravel >= 0.0) {
			timeToTravel -= delta;
		}
	}
}
