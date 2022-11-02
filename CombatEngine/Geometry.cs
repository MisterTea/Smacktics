public record Coord(int row, int col, int floor)
{
    public int horizontalDistance(Coord other)
    {
        return Math.Max(Math.Abs(this.row - other.row), Math.Abs(this.col - other.col));
    }

    public int verticalDistance(Coord other)
    {
        return Math.Abs(floor - other.floor);
    }

    public int distance(Coord other)
    {
        return this.horizontalDistance(other);
    }

    public bool isAdjacent(Coord other)
    {
        return this.horizontalDistance(other) == 1;
    }

    public Coord shift(Coord delta)
    {
        return new Coord(this.row + delta.row, this.col + delta.col, this.floor + delta.floor);
    }
}

public class Geometry
{
}
