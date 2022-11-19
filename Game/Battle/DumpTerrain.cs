using Godot;
using System;
//using Newtonsoft.Json;
using System.Text.Json;


public partial class DumpTerrain : TerrainSerializationGridMap
{
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("whatever");
	}

	//  // Called every frame. 'delta' is the elapsed time since the previous frame.
	//  public override void _Process(float delta)
	//  {
	//      
	//  }
	private void _on_button_pressed()
	{
		// Replace with function body.
		var levelData = this.getBlocks();

		var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
		string jsonString = JsonSerializer.Serialize(levelData, jsonOptions);
		//string jsonString = JsonConvert.SerializeObject(levelData, Formatting.Indented);//, jsonOptions);
																						//File.WriteAllText("GameSettings.json", jsonString);
																						//var file = new File();
																						//file.Open("res://save_game.json", File.ModeFlags.Write);
		var file = FileAccess.Open("res://../CombatEngine/save_game.json", FileAccess.ModeFlags.Write);
		file.StoreString(jsonString);
		//file.Close();
	}

}
