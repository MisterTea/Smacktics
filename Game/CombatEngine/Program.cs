using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

public class Program
{
    // Main Method
    static public void Main()
    {
        GameSettings gameSettings = JsonConvert.DeserializeObject<GameSettings>(File.ReadAllText("../GameSettings.json"))!;
        G.create(gameSettings);

        Dictionary<Coord, Guid>? blockData = JsonConvert.DeserializeObject<Dictionary<Coord, Guid>>(File.ReadAllText("../Blocks.json"), G.newtonSettings());

        if (blockData == null)
        {
            throw G.i.exception("Could not load block data");
        }

        Map map = new Map(blockData);
        List<Team> teamList = new() {
            new Team("Red Team", false),
            new Team("Blue Team", false)
        };
        map.teams = Util.listToKeyedDict(teamList);

        var infantry = G.i.settings.unitNameMap["Infantry"];

        List<Unit> unitList = new() {
            new Unit(map, "John Pavek", map.raiseToNearestWalkableFloor(new Coord(0,0,0)), infantry, teamList[0].id),
			//new Unit(map, "Nhawdge", map.raiseToNearestWalkableFloor(new Coord(1,0,-1)), archer, teamList[0].id),
			//new Unit(map, "Jason G.", map.raiseToNearestWalkableFloor(new Coord(2,0,-1)), archer, teamList[0].id),
			///
			new Unit(map, "Soldier1", map.raiseToNearestWalkableFloor(new Coord(2,3,0)), infantry, teamList[1].id),
			//new Unit(map, "Soldier2", map.raiseToNearestWalkableFloor(new Coord(1,10,-1)), soldier, teamList[1].id),
			//new Unit(map, "Soldier3", map.raiseToNearestWalkableFloor(new Coord(2,10,-1)), soldier, teamList[1].id),
		};
        map.units = Util.listToKeyedDict(unitList);

        string mapJsonString = JsonConvert.SerializeObject(map);
        File.WriteAllText("Map.json", mapJsonString);

        var battle = new Battle(map);
        while (!battle.finished)
        {
            battle.beginRound();
            battle.stepTeam();
            G.i.log("ROUND OVER");
            System.Threading.Thread.Sleep(1000);
        }
    }
}
