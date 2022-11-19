public class Team : EntityType
{
    public bool playerControlled;
    public Team(string name, bool playerControlled) : base(name)
    {
        this.playerControlled = playerControlled;
    }
}

