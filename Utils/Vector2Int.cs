using System;
using Microsoft.Xna.Framework;

namespace ChessEngine.Utils;

public struct Vector2Int
{
    public int x;
    public int y;

    public static readonly Vector2Int Zero = new Vector2Int(0, 0);
    public static readonly Vector2Int One = new Vector2Int(1, 1);

    public Vector2Int() : this(0, 0)
    {
    }

    public Vector2Int(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
    
    #region Arithmetic Operators

    public static Vector2Int operator +(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(a.x + b.x, a.y + b.y);
    }

    public static Vector2Int operator -(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(a.x - b.x, a.y - b.y);
    }

    public static Vector2Int operator *(Vector2Int v, int n)
    {
        return new Vector2Int(v.x * n, v.y * n);
    }

    public static Vector2Int operator *(int n, Vector2Int v)
    {
        return new Vector2Int(v.x * n, v.y * n);
    }

    public static Vector2Int operator /(Vector2Int v, int n)
    {
        return new Vector2Int(v.x / n, v.y / n);
    }

    #endregion

    #region Equality Operators

    public static bool operator ==(Vector2Int a, Vector2Int b)
    {
        return a.x == b.x && a.y == b.y;
    }

    public static bool operator !=(Vector2Int a, Vector2Int b)
    {
        return a.x != b.x || a.y != b.y;
    }

    #endregion

    #region Equality & HashCode

    private bool Equals(Vector2Int other)
    {
        return x == other.x && y == other.y;
    }

    public override bool Equals(object obj)
    {
        return obj is Vector2Int other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(x, y);
    }

    #endregion

    #region Implicit casts for Monogame

    public static implicit operator Point(Vector2Int v)
    {
        return new Point(v.x, v.y);
    }

    #endregion
}