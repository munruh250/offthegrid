using System;

namespace LastOut.Data;

public readonly struct Float2(float x, float y) : IEquatable<Float2>
{
    public float X { get; } = x;
    public float Y { get; } = y;

    public override bool Equals(object? obj) => obj is Float2 other && Equals(other);
    public bool Equals(Float2 other) => X.Equals(other.X) && Y.Equals(other.Y);
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"({X:F2},{Y:F2})";

    public static bool operator ==(Float2 left, Float2 right) => left.Equals(right);
    public static bool operator !=(Float2 left, Float2 right) => !left.Equals(right);
}
