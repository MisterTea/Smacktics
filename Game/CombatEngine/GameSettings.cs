using System.Collections.Generic;
using System;
//using System.Text.Json.Serialization;
using Newtonsoft.Json;

public record GameSettings(Dictionary<Guid, UnitType> unitTypes, Dictionary<Guid, AbilityType> abilityTypes, Dictionary<Guid, BlockType> blocks)
{
    [JsonIgnore]
    public Dictionary<string, BlockType> blockNameMap
    {
        get
        {
            return generateNameMap(blocks);
        }
    }

    [JsonIgnore]
    public Dictionary<string, AbilityType> abilityNameMap
    {
        get
        {
            return generateNameMap(abilityTypes);
        }
    }

    [JsonIgnore]
    public Dictionary<string, UnitType> unitNameMap
    {
        get
        {
            return generateNameMap(unitTypes);
        }
    }

    private Dictionary<string, V> generateNameMap<V>(Dictionary<Guid, V> d) where V : EntityType
    {
        Dictionary<string, V> retval = new();
        foreach (var v in d.Values)
        {
            retval.Add(v.name, v);
        }
        return retval;
    }
}
