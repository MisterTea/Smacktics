using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
//using System.Text.Json;
using Newtonsoft.Json;

public class G
{
    public GameSettings settings;
    //public JsonSerializerOptions jsonOptions = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };

    private static G? _instance;

    public virtual SystemException exception(string message,
    [CallerMemberName] string callingMethod = "",
        [CallerFilePath] string callingFilePath = "",
        [CallerLineNumber] int callingFileLineNumber = 0)
    {
        return new InvalidOperationException($"{message} ({callingFilePath}:{callingFileLineNumber})");
    }

    public virtual void log(string message,
    [CallerMemberName] string callingMethod = "",
        [CallerFilePath] string callingFilePath = "",
        [CallerLineNumber] int callingFileLineNumber = 0)
    {
        Console.WriteLine($"{message} ({callingFilePath}:{callingFileLineNumber})");
    }

    public static G create(GameSettings settings)
    {
        G.i = new G(settings);
        return G.i;
    }

    public static JsonSerializerSettings newtonSettings()
    {
        JsonSerializerSettings settings = new JsonSerializerSettings();
        settings.ContractResolver = new DictionaryAsArrayResolver();
        return settings;
    }

    public static G i
    {
        get { return _instance ?? throw new ArgumentNullException("Tried to use Globals before we were ready!"); }
        set { _instance = value; }
    }

    protected G(GameSettings settings)
    {
        this.settings = settings;
        //jsonOptions.Converters.Add(new CustomDictionaryJsonConverter<Coord, Guid>());
        //jsonOptions.Converters.Add(new CustomDictionaryJsonConverter<Coord, Walkable>());
    }
}
