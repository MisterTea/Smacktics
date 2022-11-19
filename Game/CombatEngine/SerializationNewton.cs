using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections;

class DictionaryAsArrayResolver : DefaultContractResolver
{
    protected override JsonContract CreateContract(Type objectType)
    {
        var interfaces = objectType.GetInterfaces();
        var foundOne = false;
        foreach (var i in interfaces)
        {
            if (i == typeof(IDictionary) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
            {
                foundOne = true;
                break;
            }
        }
        if (foundOne)
        {
            return base.CreateArrayContract(objectType);
        }

        return base.CreateContract(objectType);
    }
}
