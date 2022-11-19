using System;
using System.Collections.Generic;
using System.Globalization;
//using System.Text.Json;
//using System.Text.Json.Serialization;
using System.ComponentModel;
using Newtonsoft.Json;


public enum Direction
{
    ColUp = 0,
    ColDown = 1,
    RowUp = 2,
    RowDown = 3,
    FloorUp = 4,
    FloorDown = 5,
}

public struct Coord : IComparable, IComparable<Coord>, IEquatable<Coord>, FromStringable<Coord>
{
    public int row;
    public int col;
    public int floor;

    public Coord(int row, int col, int floor)
    {
        this.row = row;
        this.col = col;
        this.floor = floor;
    }

    public Coord(string s)
    {
        var coordinates = s.Split(",");
        row = Int32.Parse(coordinates[0]);
        col = Int32.Parse(coordinates[1]);
        floor = Int32.Parse(coordinates[2]);
    }

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

    public static List<Coord> pathFromBackReferences(Dictionary<Coord, Coord?> visited, Coord currentPoint)
    {
        var path = new LinkedList<Coord>();
        path.AddFirst(currentPoint);
        Coord? tracePoint = currentPoint;
        while (true)
        {
            tracePoint = visited[tracePoint.Value];
            if (tracePoint == null)
            {
                break;
            }
            path.AddFirst(tracePoint.Value);
        }
        return new List<Coord>(path);
    }

    public static Coord operator +(Coord a, Coord b)
    {
        return new Coord(a.row + b.row, a.col + b.col, a.floor + b.floor);
    }

    public static HashSet<Coord> deserializeList(List<List<int>> coordList)
    {
        HashSet<Coord> retval = new();
        // Convert to coords
        foreach (var coordAsList in coordList)
        {
            retval.Add(new Coord(coordAsList[0], coordAsList[1], coordAsList[2]));
        }
        return retval;
    }

    public static Coord fromDirection(Direction d)
    {
        switch (d)
        {
            case Direction.ColUp:
                return new Coord(0, 1, 0);
            case Direction.ColDown:
                return new Coord(0, -1, 0);
            case Direction.RowUp:
                return new Coord(1, 0, 0);
            case Direction.RowDown:
                return new Coord(-1, 0, 0);
            case Direction.FloorUp:
                return new Coord(0, 0, 1);
            case Direction.FloorDown:
                return new Coord(0, 0, -1);
            default:
                throw G.i.exception("Should never get here");
        }
    }

    public override string ToString()
    {
        return $"{row},{col},{floor}";
    }

    public int CompareTo(object? obj)
    {
        if (obj == null) return 1;

        Coord? other = obj as Coord?;
        if (other != null)
        {
            return this.CompareTo(other.Value);
        }
        else
        {
            throw new ArgumentException("Object is not a Coord");
        }
    }

    public int CompareTo(Coord other)
    {
        var floorCompare = this.floor.CompareTo(other.floor);
        if (floorCompare != 0)
        {
            return floorCompare;
        }
        var rowCompare = this.row.CompareTo(other.row);
        if (rowCompare != 0)
        {
            return rowCompare;
        }
        return this.col.CompareTo(other.col);
    }

    public bool Equals(Coord coord)
    {
        return (floor == coord.floor) && (row == coord.row) && (col == coord.col);
    }

    public override bool Equals(object? other)
    {
        if (other == null)
        {
            return false;
        }
        Coord? coord = other as Coord?;
        if (coord == null)
        {
            throw new ArgumentException("Object is not a Coord");
        }
        return Equals(coord.Value);
    }

    public override int GetHashCode()
    {
        return floor.GetHashCode() ^ row.GetHashCode() ^ col.GetHashCode();
    }

    public Coord FromString(string s)
    {
        var coordinates = s.Split(",");
        return new Coord(Int32.Parse(coordinates[0]), Int32.Parse(coordinates[1]), Int32.Parse(coordinates[2]));
    }

    public static bool operator ==(Coord coord1, Coord coord2)
    {
        if (((object)coord1) == null || ((object)coord2) == null)
            return Object.Equals(coord1, coord2);

        return coord1.Equals(coord2);
    }

    public static bool operator !=(Coord coord1, Coord coord2)
    {
        if (((object)coord1) == null || ((object)coord2) == null)
            return !Object.Equals(coord1, coord2);

        return !(coord1.Equals(coord2));
    }
}

/*
public class CoordJsonConverter : Newtonsoft.Json.JsonConverter<Coord>
{
    public override Coord ReadJson(JsonReader reader, Type objectType, Coord existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var coordinates = ((string)reader.Value)!.Split(",");
        return new Coord(Int32.Parse(coordinates[0]), Int32.Parse(coordinates[1]), Int32.Parse(coordinates[2]));
    }

    public override void WriteJson(JsonWriter writer, Coord value, JsonSerializer serializer)
    {
        Coord coord = (Coord)value;
        writer.WriteValue($"{coord.row},{coord.col},{coord.floor}");
    }
}

public class CoordConverter : TypeConverter
{
    // Overrides the CanConvertFrom method of TypeConverter.
    // The ITypeDescriptorContext interface provides the context for the
    // conversion. Typically, this interface is used at design time to 
    // provide information about the design-time container.
    public override bool CanConvertFrom(ITypeDescriptorContext context,
       Type sourceType)
    {

        if (sourceType == typeof(string))
        {
            return true;
        }
        return base.CanConvertFrom(context, sourceType);
    }
    // Overrides the ConvertFrom method of TypeConverter.
    public override object ConvertFrom(ITypeDescriptorContext context,
       CultureInfo culture, object value)
    {
        if (value is string)
        {
            var coordinates = ((string)value).Split(",");
            return new Coord(Int32.Parse(coordinates[0]), Int32.Parse(coordinates[1]), Int32.Parse(coordinates[2]));
        }
        return base.ConvertFrom(context, culture, value);
    }
    // Overrides the ConvertTo method of TypeConverter.
    public override object ConvertTo(ITypeDescriptorContext context,
       CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string))
        {
            Coord valueCoord = (Coord)value;
            return $"{valueCoord.row},{valueCoord.col},{valueCoord.floor}";
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}
*/
