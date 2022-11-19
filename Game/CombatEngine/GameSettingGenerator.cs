//using System.Text.Json;
using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

public class GameSettingGenerator
{
    static public void Run()
    {
        var blaster = new AbilityType("Blaster") { horizontalRange = 15, verticalRange = 15, damage = 1 };

        List<AbilityType> abilityTypes = new() { blaster };

        var infantry = new UnitType("Infantry") { health = 2, movementRange = 3, abilities = new List<Guid>() { blaster.id } };

        List<UnitType> unitTypes = new() { infantry };

        List<BlockType> blockTypes = new() {
            new BlockType(Height.Double, Slope.None, "GrassCube"),
        };

        GameSettings gameSettings = new GameSettings(Util.listToKeyedDict(unitTypes), Util.listToKeyedDict(abilityTypes), Util.listToKeyedDict(blockTypes));

        G.create(gameSettings);

        string jsonString = JsonConvert.SerializeObject(gameSettings);
        File.WriteAllText("../GameSettings.json", jsonString);
    }
}
