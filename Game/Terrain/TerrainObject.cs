using Godot;
using System;

public partial class TerrainObject : MeshInstance3D
{
	[Export]
	public Vector3i cellSize = new Vector3i(1,2,1);

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
