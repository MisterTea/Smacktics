using System.Text.Json;
using System;
using System.Threading;
public class Program
{
    // Main Method
    static public void Main()
    {
        var archer = new UnitType("Archer") { health = 2, movementRange = 3, attackHorizontalRange = 4, attackVerticalRange = 4 };
        var thief = new UnitType("Thief") { health = 2, movementRange = 5 };
        var soldier = new UnitType("Soldier") { health = 2, movementRange = 3, damage = 2, armor = 1 };
        UnitType[] unitTypes = { archer, thief, soldier };
        GameSettings gameSettings = new GameSettings(Util.listToKeyedDict(unitTypes));

        Globals.create(gameSettings);

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
        string jsonString = JsonSerializer.Serialize(gameSettings, jsonOptions);
        File.WriteAllText("GameSettings.json", jsonString);

        var heightMap = new HashSet<Coord>();
        for (int r = 0; r < 16; r++)
        {
            for (int c = 0; c < 16; c++)
            {
                // Triangle function from https://www.wolframalpha.com/input?i=y+%3D+%28%284*1%2F4%29+*+abs%28%28%28%28x-%284%2F4%29%29-%284%2F4%29%29%254%29+-+%284%2F2%29%29%29
                int height = ((4 * 1 / 4) * Math.Abs((((c - (4 / 4)) - (4 / 4)) % 4) - (4 / 2)));
                heightMap.Add(new Coord(r, c, height));
            }
        }
        Map map = new Map(16, 16, 16, heightMap);
        Team[] teamList = {
            new Team("Red Team"),
            new Team("Blue Team")
        };
        map.teams = Util.listToKeyedDict(teamList);

        Unit[] unitList = {
            new Unit(map, "John Pavek", map.raiseToNearestWalkableFloor(new Coord(0,0,-1)), archer, teamList[0].id),
            //new Unit(map, "Nhawdge", map.raiseToNearestWalkableFloor(new Coord(1,0,-1)), archer, teamList[0].id),
            //new Unit(map, "Jason G.", map.raiseToNearestWalkableFloor(new Coord(2,0,-1)), archer, teamList[0].id),
            ///
            new Unit(map, "Soldier1", map.raiseToNearestWalkableFloor(new Coord(0,10,-1)), soldier, teamList[1].id),
            //new Unit(map, "Soldier2", map.raiseToNearestWalkableFloor(new Coord(1,10,-1)), soldier, teamList[1].id),
            //new Unit(map, "Soldier3", map.raiseToNearestWalkableFloor(new Coord(2,10,-1)), soldier, teamList[1].id),
        };
        map.units = Util.listToKeyedDict(unitList);

        string mapJsonString = JsonSerializer.Serialize(map, jsonOptions);
        File.WriteAllText("Map.json", mapJsonString);

        var battle = new Battle(map);
        while (true)
        {
            battle.step();
            Console.WriteLine("ROUND OVER");
            System.Threading.Thread.Sleep(1000);
        }
    }
}
