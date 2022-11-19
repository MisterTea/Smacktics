using Godot;
using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

public partial class BattleLogic : Node
{
	public int onEvent = 0;
	public double delayUntilNextEvent = 0.0;
	public Map map;
	public Dictionary<Guid, UnitScene> unitScenes = new();
	public BattleCamera camera;
	public Node3D rootNode;
	public ArenaGridMap gridMapNode;
	public GridMap highlightGridMapNode;
	Battle battle;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.rootNode = GetTree().Root.GetNode<Node3D>("Arena");
		this.camera = (BattleCamera)GetTree().Root.GetNode("Arena").GetNode<BattleCamera>("Camera3D");
		gridMapNode = (ArenaGridMap)GetTree().Root.GetNode("Arena").GetNode<Node>("GridMap");
		highlightGridMapNode = (GridMap)GetTree().Root.GetNode("Arena").GetNode<Node>("HighlightGridMap");
		
		CallDeferred("StartBattle");
	}
	
	public void DebugPrint(
		string message,
		[CallerMemberName] string callingMethod = "",
		[CallerFilePath] string callingFilePath = "",
		[CallerLineNumber] int callingFileLineNumber = 0) {
			GD.Print($"{message} ({callingFilePath}:{callingFileLineNumber})");
		}
	
	public void StartBattle(
		) {
		//GameSettings gameSettings = JsonSerializer.Deserialize<GameSettings>(File.ReadAllText("./GameSettings.json"));
		GameSettings gameSettings = JsonConvert.DeserializeObject<GameSettings>(File.ReadAllText("./GameSettings.json"));

		GodotGlobals.create(gameSettings);

		Dictionary<Coord, Guid> blocks = gridMapNode.getBlocks();

		string blockDataString = JsonConvert.SerializeObject(blocks, G.newtonSettings());
		File.WriteAllText("Blocks.json", blockDataString);

		blocks = JsonConvert.DeserializeObject<Dictionary<Coord, Guid>>(blockDataString, G.newtonSettings());
		if (blocks == null)
		{
			throw new InvalidOperationException("Could not deserialize walkable json");
		}

		this.map = new Map(blocks);
		gridMapNode.map = map;

		List<Team> teamList = new() {
			new Team("Red Team", true),
			new Team("Blue Team", false)
		};
		this.map.teams = Util.listToKeyedDict(teamList);
		
		var infantry = G.i.settings.unitNameMap["Infantry"];

		List<Unit> unitList = new() {
			new Unit(map, "John Pavek", map.raiseToNearestWalkableFloor(new Coord(0,0,0)), infantry, teamList[0].id),
			//new Unit(map, "Nhawdge", map.raiseToNearestWalkableFloor(new Coord(1,0,-1)), infantry, teamList[0].id),
			//new Unit(map, "Jason G.", map.raiseToNearestWalkableFloor(new Coord(2,0,-1)), infantry, teamList[0].id),
			///
			new Unit(map, "Soldier1", map.raiseToNearestWalkableFloor(new Coord(13,13,0)), infantry, teamList[1].id)
			//new Unit(map, "Soldier2", map.raiseToNearestWalkableFloor(new Coord(1,10,-1)), infantry, teamList[1].id),
			//new Unit(map, "Soldier3", map.raiseToNearestWalkableFloor(new Coord(2,10,-1)), infantry, teamList[1].id),
		};
		this.map.units = Util.listToKeyedDict(unitList);

		foreach (Unit unit in unitList) {
			UnitScene unitScene = (UnitScene)(ResourceLoader.Load<PackedScene>("res://Battle/UnitScene.tscn").Instantiate());
			unitScene.init(unit);
			rootNode.AddChild(unitScene);
			this.unitScenes[unit.id] = unitScene;
		}

		string mapJsonString = JsonConvert.SerializeObject(this.map, G.newtonSettings());
		File.WriteAllText("Map.json", mapJsonString);

		battle = new Battle(this.map);
		battle.beginRound();

		advanceBattle();
	}
	
	public void advanceBattle() {
		while (!battle.finished)
		{
			GD.Print($"Team {battle.currentTeam.name} is up!");
			if (battle.currentTeam.playerControlled) {
				// Check if there's a unit that can take an action
				bool canAct = false;
				foreach (var unit in battle.activeUnits) {
					if (unit.hasAction()) {
						GD.Print("UNIT HAS ACTION");
						canAct = true;
						break;
					}
				}
				
				if (canAct) {
					break;
				} else {
					battle.advanceTeam();
					battle.beginRound();
				}
			}
			DebugPrint("ROUND OVER");
			battle.stepTeam();
			battle.beginRound();
		}
		
		if (battle.finished) {
			DebugPrint("GAME OVER");
		}
	}

	public double? timeToSceneTeardown = null;
	public Node? sceneNode = null;
	
	public void updateHighlights() {
		highlightGridMapNode.Clear();
		var unit = this.map.unitForTeam(battle.currentTeam);
		
		if (unit.moveCoords != null) {
			foreach (var moveCoord in unit.moveCoords) {
				highlightGridMapNode.SetCellItem(new Vector3i(moveCoord.col, moveCoord.floor, moveCoord.row), 1);
			}
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		highlightGridMapNode.Visible = false;

		if (timeToSceneTeardown != null) {
			timeToSceneTeardown -= delta;
			if (timeToSceneTeardown <= 0.0) {
				timeToSceneTeardown = null;
				if (this.sceneNode != null) {
					this.sceneNode.QueueFree();
					this.sceneNode = null;
				}
			}
			return;
		}

		if (this.delayUntilNextEvent > 0.0) {
			this.delayUntilNextEvent -= delta;
			if (this.delayUntilNextEvent <= 0.0) {
				// Reset view back to main character
				var unit = this.map.unitForTeam(battle.currentTeam);
				this.camera.sceneFocusNode = this.unitScenes[unit.id];
				this.camera.sceneFocusPoints = null;
			}
			return;
		}

		if (this.map.events.Count <= this.onEvent) {
			// Nothing left to process
			
			updateHighlights();
			highlightGridMapNode.Visible = true;

			if (this.camera.sceneFocusNode == null && this.camera.sceneFocusPoints == null) {
				// If we don't know what to look at, look at the main character
				GD.Print(battle.currentTeam);
				GD.Print(this.map);
				var unit = this.map.unitForTeam(battle.currentTeam);
				this.camera.sceneFocusNode = this.unitScenes[unit.id];
				this.camera.sceneFocusPoints = null;
			}

			return;
		}

		this.delayUntilNextEvent -= delta;
		if (this.delayUntilNextEvent < 0.0) {
			this.delayUntilNextEvent = 1.0;
			DebugPrint("Starting new event");
			DebugPrint($"{this.map.events[this.onEvent]}");
			var mapEvent = this.map.events[this.onEvent];
			if (mapEvent.eventType == MapEventType.Movement) {
				if (mapEvent.path == null) {
					throw G.i.exception("Somehow path was null for movement event");
				}
				this.unitScenes[mapEvent.source.id].followPath(mapEvent.path);
				this.camera.sceneFocusNode = this.unitScenes[mapEvent.source.id];
				this.camera.sceneFocusPoints = null;
			}
			if (mapEvent.eventType == MapEventType.Ability) {
				var sourceScene = this.unitScenes[mapEvent.source.id];
				this.camera.sceneFocusNode = null;
				this.camera.sceneFocusPoints = new List<Vector3>() {sourceScene.Position, this.unitScenes[mapEvent.targetUnit.id].Position};
				GD.Print($"Focusing on {this.camera.sceneFocusPoints[0]} {this.camera.sceneFocusPoints[1]}");
				this.timeToSceneTeardown = 1.0;

				var abilityScene = new Node();
				rootNode.AddChild(abilityScene);
				this.sceneNode = abilityScene;
				
				// TODO: Only do this for attack abilities
				Node3D projectileScene = (Node3D)(ResourceLoader.Load<PackedScene>("res://Battle/BulletScene.tscn").Instantiate());
				projectileScene.Position = ArenaGridMap.coordToPosition(mapEvent.source.location + new Coord(0,0,3));
				abilityScene.AddChild(projectileScene);
				//projectileScene.DestinationPosition = ArenaGridMap.coordToPosition(mapEvent.targetUnit.location + new Coord(0,0,3));

				Tween tween = GetTree().CreateTween();
				var destination = ArenaGridMap.coordToPosition(mapEvent.targetUnit.location + new Coord(0,0,3));
				tween.TweenProperty(projectileScene, "position", destination, 0.2f);
				tween.TweenProperty(projectileScene, "visible", false, 0.001f);
				//tween.TweenCallback(new Callable(projectileScene, "QueueFree"));


				foreach (var healthChange in mapEvent.healthChange) {
					var unitScene = this.unitScenes[healthChange.Key];
					var unit = this.map.units[healthChange.Key];
					var amount = healthChange.Value;
					CombatText combatTextScene = (CombatText)(ResourceLoader.Load<PackedScene>("res://Battle/CombatText.tscn").Instantiate());
					combatTextScene.anchor = ArenaGridMap.coordToPosition(unit.location + new Coord(0,0,6));
					abilityScene.AddChild(combatTextScene);
					if (amount > 0) {
						combatTextScene.label.Theme = ResourceLoader.Load<Theme>("res://Themes/Healing.tres");
					} else {
						amount *= -1;
						combatTextScene.label.Theme = ResourceLoader.Load<Theme>("res://Themes/Damage.tres");
					}
					combatTextScene.label.Text = amount.ToString();
					combatTextScene.Visible = false;
					tween.TweenProperty(combatTextScene, "visible", true, 0.001f);
				}
			}
			this.onEvent += 1;
			if (this.map.events.Count <= this.onEvent) {
				DebugPrint("Finished!");
			}
		}
	}

	public bool pressed = false;
	public Vector2 pressPosition = new();
	public bool dragged = false;
	public bool attackMode = false;

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
		{
			if (!pressed && mouseEvent.Pressed)
			{
				GD.Print("Handling press");
				pressed = true;
				dragged = false;
				pressPosition = mouseEvent.Position;
			}
			if (pressed && !mouseEvent.Pressed)
			{
				pressed = false;
				if (!dragged) {
					// Mouse press
					GD.Print("CLICK");
					if (battle.currentTeam.playerControlled) {
						var unit = this.map.unitForTeam(battle.currentTeam);
						if (unit.moveCoords != null) {
							var origin = this.camera.ProjectRayOrigin(mouseEvent.Position);
							var normal = this.camera.ProjectRayNormal(mouseEvent.Position);
							var coordsClicked = gridMapNode.pickWalkableCells(origin, normal);
							Coord? clickedInRange = null;
							foreach (Coord coordClicked in coordsClicked) {
								if (unit.moveCoords.Contains(coordClicked)) {
									if (clickedInRange == null || clickedInRange.Value.floor < coordClicked.floor) {
										clickedInRange = coordClicked;
									}
								}
							}
							if (clickedInRange != null) {
								var path = map.movementDistance(unit.location, clickedInRange.Value, unit.type.jumpHeight, unit.moveCoords)!.Value.Item2;
								//var path = Coord.pathFromBackReferences(coordsInRangeWithBackReferences, clickedInRange.Value);
								unit.moveTo(clickedInRange.Value, path);
								advanceBattle();
							}
						}
					}
				}
				dragged = false;
			}
		}
		else
		{
			if (inputEvent is InputEventMouseMotion motionEvent && pressed)
			{
				var currentPosition = motionEvent.Position;
				//GD.Print($"Dragged by distance: {currentPosition.DistanceTo(pressPosition)}");
				if (currentPosition.DistanceTo(pressPosition) > 10) {
					dragged = true;
				}
			}
		}
	}
	
	public int? abilityChosen = null;

	private void _on_attack_button_button_down()
	{
		GD.Print("Attack button down");
		attackMode = true;
		if (this.timeToSceneTeardown != null) {
			return;
		}
		this.timeToSceneTeardown = null;
		var unit = this.map.unitForTeam(battle.currentTeam);

		// TODO: Pick the best ability when there are multiple
		abilityChosen = 0;
		Ability unitAbility = unit.abilities[abilityChosen.Value];

		var enemies = this.map.findAllTargetableEnemies(unit, unitAbility);

		List<Vector3> points = new();
		points.Add(this.unitScenes[unit.id].Position);
		foreach (var enemy in enemies) {
			points.Add(this.unitScenes[enemy.id].Position);
		}
		this.camera.sceneFocusNode = null;
		this.camera.sceneFocusPoints = points;
	}
	
	private void _on_attack_button_button_up()
	{
		GD.Print("Release");
		attackMode = false;
		this.camera.sceneFocusPoints = null;
		
		var unit = this.map.unitForTeam(battle.currentTeam);
		var ability = unit.abilities[abilityChosen.Value];

		this.camera.sceneFocusNode = this.unitScenes[unit.id];

		// Check if we picked a unit
		var walkable = this.camera.getNearestWalkableCellFromMouse();
		if (walkable != null) {
			var target = this.map.unitFromCoord(walkable.Value);
			if (target != null) {
				// Attack the target!
				GD.Print("ATTACKING");
				if (this.map.attackCoords(target.location, unit, ability).Contains(unit.location)) {
					GD.Print("Can reach!");
					this.battle.attack(unit, ability, target);
					advanceBattle();
				}
				return;
			}
		}

	}
}

