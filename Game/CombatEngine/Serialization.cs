using System.Text.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System;

public interface FromStringable<T>
{
    public T FromString(String s);
}

public class CustomDictionaryJsonConverter<TKey, TValue> : JsonConverter<Dictionary<TKey, TValue>> where TKey : FromStringable<TKey>, new()
{
    public override bool CanConvert(Type typeToConvert)
    {
        /* Only use this converter if 
		 * 1. It's a dictionary
		 * 2. The key is not a string
		 */
        if (typeToConvert != typeof(Dictionary<TKey, TValue>))
        {
            return false;
        }
        else if (typeToConvert.GenericTypeArguments[0] == typeof(string))
        {
            return false;
        }
        return true;
    }
    public override Dictionary<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        //Step 1 - Use built-in serializer to deserialize into a dictionary with string key
        object? nullableDictionary = JsonSerializer.Deserialize(ref reader, typeof(Dictionary<string, TValue>), options);
        if (nullableDictionary == null)
        {
            throw new InvalidCastException();
        }
        var dictionaryWithStringKey = (Dictionary<string, TValue>)nullableDictionary;


        //Step 2 - Convert the dictionary to one that uses the actual key type we want
        var dictionary = new Dictionary<TKey, TValue>();

        foreach (var kvp in dictionaryWithStringKey)
        {
            //dictionary.Add((TKey)Convert.ChangeType(kvp.Key, typeof(TKey)), kvp.Value);
            dictionary.Add(new TKey().FromString(kvp.Key), kvp.Value);
        }

        return dictionary;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<TKey, TValue> value, JsonSerializerOptions options)
    {
        //Step 1 - Convert dictionary to a dictionary with string key
        var dictionary = new Dictionary<string, TValue>(value.Count);

        foreach (var kvp in value)
        {
            var stringValue = kvp.Key.ToString();
            dictionary.Add(stringValue!, kvp.Value);
        }
        //Step 2 - Use the built-in serializer, because it can handle dictionaries with string keys
        JsonSerializer.Serialize(writer, dictionary, options);

    }
}
