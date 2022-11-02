using Godot;
using System;
using Newtonsoft.Json;

public class DumpTerrain : GridMap
{
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("whatever");
		GridMap gm = this;
		GD.Print(gm);
		var usedCells = gm.GetUsedCells();
		var meshLibrary = this.MeshLibrary;
		GD.Print(meshLibrary);
		var levelData = new System.Collections.Generic.List<int[]>();
		foreach (Vector3 usedCell in usedCells) {
			GD.Print(usedCell);
			var cellItem = this.GetCellItem((int)usedCell.x, (int)usedCell.y, (int)usedCell.z);
			GD.Print(cellItem);
			var itemName = meshLibrary.GetItemName(cellItem);
			GD.Print(itemName);
			// Check if the square above is walkable
			if (this.GetCellItem((int)usedCell.x, ((int)usedCell.y) + 1, (int)usedCell.z) == GridMap.InvalidCellItem &&
				this.GetCellItem((int)usedCell.x, ((int)usedCell.y) + 2, (int)usedCell.z) == GridMap.InvalidCellItem) {
				// Walkable
				GD.Print("WALKABLE");
				levelData.Add(new[] {(int)usedCell.x, (int)usedCell.z, ((int)usedCell.y) + 1});
			}
		}
		
		//var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
		string jsonString = JsonConvert.SerializeObject(levelData, Formatting.Indented);//, jsonOptions);
		//File.WriteAllText("GameSettings.json", jsonString);
		var file = new File();
		file.Open("res://save_game.json", File.ModeFlags.Write);
		file.StoreString(jsonString);
		file.Close();
	}

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
