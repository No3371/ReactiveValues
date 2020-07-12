
using System;

public struct ValueModifier : IEquatable<ValueModifier>
{
    internal int sourceIndex;
    internal float value;

    internal ValueModifier(ModifierAction modType)
    {
        this.sourceIndex = -1;
        this.value = 0;
        Action = modType;
        Removed = false;
    }

    internal ValueModifier(ModifierAction modType, float initValue)
    {
        this.sourceIndex = -1;
        this.value = initValue;
        Action = modType;
        Removed = false;
    }
    internal ValueModifier(ModifierAction modType, int sourceIndex, float initValue)
    {
        this.sourceIndex = sourceIndex;
        this.value = initValue;
        Action = modType;
        Removed = false;
    }

    public ModifierAction Action { get; set; }
    public bool Removed { get; internal set; }

    public bool Equals(ValueModifier other)
    {
        return Action == other.Action && sourceIndex == other.sourceIndex && (sourceIndex == -1 ? true : value == other.value) && Removed == other.Removed;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            uint hash = (uint) System.Math.Abs(sourceIndex);
            hash = hash * 13 + (byte) Action; 
            hash = hash * 13 + (uint) (System.Math.Abs(value) * 100);
            return (int) hash;
        }
    }

    public static ValueModifier PlaceHolder ()
    {
        ValueModifier m = new ValueModifier(ModifierAction.PlaceHolder);
        m.Removed = true;
        return m;
    }
}