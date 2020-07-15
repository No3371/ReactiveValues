
using System;

public struct Modifier : IEquatable<Modifier>
{
    internal int source;
    internal float value;
    internal bool sourceIsFormula;
    internal Modifier(ModifierAction modType)
    {
        this.source = -1;
        this.value = 0;
        Action = modType;
        Removed = false;
        sourceIsFormula = false;
    }

    internal Modifier(ModifierAction modType, float initValue)
    {
        this.source = -1;
        this.value = initValue;
        Action = modType;
        Removed = false;
        sourceIsFormula = false;
    }
    internal Modifier(ModifierAction modType, int sourceIndex, float initValue, bool isFormula = false)
    {
        this.source = sourceIndex;
        this.value = initValue;
        Action = modType;
        Removed = false;
        sourceIsFormula = isFormula;
    }

    public ModifierAction Action { get; set; }
    public bool Removed { get; internal set; }

    public bool Equals(Modifier other)
    {
        return Action == other.Action && source == other.source && (source == -1 ? true : value == other.value) && Removed == other.Removed && sourceIsFormula == other.sourceIsFormula;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            uint hash = (uint) System.Math.Abs(source);
            hash = hash * 13 + (byte) Action; 
            hash = hash * 13 + (uint) (System.Math.Abs(value) * 100);
            if (Removed) hash *= 31;
            if (sourceIsFormula) hash *= 71;
            return (int) hash;
        }
    }

    public static Modifier PlaceHolder ()
    {
        Modifier m = new Modifier(ModifierAction.PlaceHolder);
        m.Removed = true;
        return m;
    }
}