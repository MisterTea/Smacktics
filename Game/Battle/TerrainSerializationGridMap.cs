using Godot;
using System;
using System.Collections.Generic;

public partial class TerrainSerializationGridMap : GridMap
{
	public Dictionary<Coord, Guid> getBlocks() {
		GridMap gm = this;
		//GD.Print(gm);
		var usedCells = gm.GetUsedCells();
		var meshLibrary = this.MeshLibrary;
		//GD.Print(meshLibrary);
		
		var filledCells = new HashSet<Vector3i>();
		
		Dictionary<Coord, Guid> retval = new();
		
		var blockNames = G.i.settings.blockNameMap;
		GD.Print("<MZNC<MNXZ");
		foreach (var blockName in blockNames.Keys) {
			GD.Print(blockName);
		}
		GD.Print("LASKHJDLKASJ");
		foreach (var blockName in G.i.settings.blocks.Keys) {
			GD.Print(blockName);
		}
		GD.Print("OKJ");
		
		var itemList = new HashSet<int>();
		foreach (var itemId in meshLibrary.GetItemList()) {
			itemList.Add(itemId);
		}

		foreach (Vector3i usedCell in usedCells)
		{
			//GD.Print(usedCell);
			var cellItem = this.GetCellItem(usedCell);
			//GD.Print(cellItem);
			if (!itemList.Contains(cellItem)) {
				//GD.Print("Skipping cell item");
				continue;
			}
			var itemName = meshLibrary.GetItemName(cellItem);
			//GD.Print(itemName);
			
			retval.Add(new Coord(usedCell.z, usedCell.x, usedCell.y), blockNames[itemName].id);
		}
		
		return retval;
	}
}
