using Godot;
using System;

public partial class CombatText : Control
{
	[Export]
	public Vector3 anchor = new();
	
	public Label label;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		label = GetNode<Label>("Label");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var camera = GetViewport().GetCamera3d();
		
		if (camera == null) {
			return;
		}
		
		var unprojectedPosition = camera.UnprojectPosition(this.anchor);
		
		this.Position = unprojectedPosition;
	}
}
