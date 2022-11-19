using System.Collections.Generic;
using System;

public class Util
{
    public static Dictionary<Guid, T> listToKeyedDict<T>(List<T> items) where T : EntityType
    {
        var retval = new Dictionary<Guid, T>();
        foreach (var item in items)
        {
            retval.Add(item.id, item);
        }
        return retval;
    }
}
