using Godot;
using System;
using System.Collections.Generic;

public partial class BattleCamera : Camera3D
{
	const double CAMERA_PAN_TIME = 0.5;
	private Transform3D lastPosition;
	public Transform3D _goalPosition;
	public double timeToGoal = 0.0;
	public Basis basis = Basis.FromEuler(new Vector3((float)(Math.PI / -4.0), (float)(Math.PI / -4.0), 0));
	
	public EventFinishedHandler? _sceneFocusNode = null;
	public EventFinishedHandler? sceneFocusNode {
		get {
			return _sceneFocusNode;
		}
		set {
			_sceneFocusNode = value;
			if (value != null) {
				_sceneFocusPoints = null;
			}
		}
	}

	public List<Vector3>? _sceneFocusPoints = null;
	public List<Vector3>? sceneFocusPoints {
		get {
			return _sceneFocusPoints;
		}
		set {
			_sceneFocusPoints = value;
			if (value != null) {
				_sceneFocusNode = null;
			}
		}
	}
	
	public Transform3D goalPosition {
		get {
			return _goalPosition;
		}
		set {
			if (_goalPosition.IsEqualApprox(value)) {
				return;
			}
			lastPosition = this.Transform;
			_goalPosition = value;
			timeToGoal = CAMERA_PAN_TIME;
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.Transform = new Transform3D(this.basis, basis * new Vector3(0,0,6));
		this._goalPosition = this.Transform;
	}
	
	public bool hasActiveScene() {
		return this.sceneFocusNode != null || this.sceneFocusPoints != null;
	}

	public Transform3D transformFromPoints(List<Vector3> points) {
		var midpoint = new Vector3();
		foreach (var point in points) {
			midpoint += point;
		}
		midpoint /= points.Count;
		//(p1 + p2) / 2.0f;
		
		//GD.Print(p1);
		//GD.Print(p2);
		//GD.Print("***");
		var distance = 10;
		var viewportSize = GetViewport().GetVisibleRect().Size;
		var oldPosition = this.Position;
		while (true) {
			this.Position = midpoint + (basis * new Vector3(0,0,distance));
			//GD.Print(this.UnprojectPosition(p1));
			//GD.Print(this.UnprojectPosition(p2));
			//GD.Print("***");
			var failed = false;
			foreach (var point in points) {
				var viewportPoint = this.UnprojectPosition(point);
				if (
					viewportPoint.x < viewportSize.x / 5.0f ||
					viewportPoint.x > (viewportSize.x * 4.0f / 5.0f)
				) {
					failed = true;
					break;
				}
			}
			
			if (!failed) {
				//GD.Print($"Got distance {distance}");
				var goalPos = midpoint + (this.basis * new Vector3(0,0,distance));
				this.Position = oldPosition;
				return new Transform3D(this.basis, goalPos);
			}
			distance += 1;
			if (distance > 1000) {
				throw G.i.exception("Oops");
			}
			//GD.Print("Keep trying");
		}
		
		// NOTE: This assumes that the camera angle never changes
		//var firstUnitRay = ProjectRayOrigin(new Vector2(viewportSize.x / 5.0, viewportSize.y / 2.0)) - this.Position;
		//var secondUnitRay = ProjectRayOrigin(new Vector2(viewportSize.x - (viewportSize.x / 5.0), viewportSize.y / 2.0)) - this.Position;
		
		//firstUnitRay = firstUnitRay * -1;
		//secondUnitRay = secondUnitRay * -1;
		
		//p1 + t1 * firstUnitRay == p2 + t2 * secondUnitRay
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//GD.Print("camera process");
		if (this.sceneFocusNode != null) {
			if (this.sceneFocusPoints != null) {
				throw G.i.exception("Should not get here");
			}
			var basis = this.basis;
			var goalPos = this.sceneFocusNode.Position + (basis * new Vector3(0,0,12));
			this.goalPosition = new Transform3D(basis, goalPos);
		}
		
		if (this.sceneFocusPoints != null) {
			this.goalPosition = transformFromPoints(this.sceneFocusPoints);
		}

		if (timeToGoal > 0.0) {
			timeToGoal -= delta;
			if (timeToGoal <= 0.0) {
				this.Transform = this.goalPosition;
			} else {
				this.Transform = lastPosition.InterpolateWith(goalPosition, (float)((CAMERA_PAN_TIME - timeToGoal) / CAMERA_PAN_TIME));
			}
		}
	}
	
	public Coord? getNearestWalkableCellFromMouse() {
		var mousePosition = GetViewport().GetMousePosition();
		var origin = this.ProjectRayOrigin(mousePosition);
		var normal = this.ProjectRayNormal(mousePosition);
		ArenaGridMap gridMapNode = (ArenaGridMap)GetTree().Root.GetNode("Arena").GetNode<Node>("GridMap");
		return gridMapNode.pickNearestWalkableCell(origin, normal);
	}
	
	private bool dragging = false;
	private Vector3 startDragPosition = new();
	private Vector2 dragScale = new(0.02f, 0.03f);
	private Vector3 dragAnchor = new();

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
		{
			// Start dragging if the click is on the sprite.
			if (!dragging && mouseEvent.Pressed)
			{
				dragging = true;
				startDragPosition = this.Position;
				ArenaGridMap gridMapNode = (ArenaGridMap)GetTree().Root.GetNode("Arena").GetNode<Node>("GridMap");
				var origin = this.ProjectRayOrigin(mouseEvent.Position);
				var normal = this.ProjectRayNormal(mouseEvent.Position);
				var walkablePicked = gridMapNode!.pickNearestWalkableCell(origin, normal).Value;
				dragAnchor = ArenaGridMap.floorIntersect(ArenaGridMap.coordToPosition(walkablePicked).y, origin, normal);
				Input.SetDefaultCursorShape(Input.CursorShape.Drag);
			}
			// Stop dragging if the button is released.
			if (dragging && !mouseEvent.Pressed)
			{
				dragging = false;
				Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
			}
		}
		else
		{
			if (inputEvent is InputEventMouseMotion motionEvent && dragging)
			{
				this.Position = startDragPosition;
				var origin = this.ProjectRayOrigin(motionEvent.Position);
				var normal = this.ProjectRayNormal(motionEvent.Position);
				
				var locationOnFloor = ArenaGridMap.floorIntersect(dragAnchor.y, origin, normal);

				this.Position = startDragPosition + new Vector3((dragAnchor.x - locationOnFloor.x), 0, (dragAnchor.z - locationOnFloor.z));
			}
		}
	}
}
