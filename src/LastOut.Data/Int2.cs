using System;

namespace LastOut.Data;

public readonly struct Int2(int x, int y) : IEquatable<Int2>
{
    public int X { get; } = x;
    public int Y { get; } = y;

    public override bool Equals(object? obj) => obj is Int2 other && Equals(other);
    public bool Equals(Int2 other) => X == other.X && Y == other.Y;
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"({X},{Y})";

    public static bool operator ==(Int2 left, Int2 right) => left.Equals(right);
    public static bool operator !=(Int2 left, Int2 right) => !left.Equals(right);
}
