public record GameSettings(Dictionary<Guid, UnitType> unitTypes);

public sealed class Globals
{
    public GameSettings settings;

    private static Globals? _instance;

    public static Globals create(GameSettings settings)
    {
        Globals.Instance = new Globals(settings);
        return Globals.Instance;
    }

    public static Globals Instance
    {
        get { return _instance ?? throw new ArgumentNullException("Globals"); }
        set { _instance = value; }
    }

    private Globals(GameSettings settings)
    {
        this.settings = settings;
    }
}
