using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

public class Variable
{
    private Variable () {}
    internal Variable(params ValueModifier[] modifiers)
    {
        this.modifiers = modifiers;
        this.modifierCount = modifiers.Length;
        changesNotRecalculated = true;
    }
    public float CachedModifiedValue { get; internal set; }
    internal ValueModifier[] modifiers;
    public ReadOnlyCollection<ValueModifier> ExamineModifiers()
    {
        return Array.AsReadOnly<ValueModifier>(modifiers);
    }


    int modifierCount = 0;
    public uint Version { get; internal set; }
    public uint LastAccessedVersion { get; internal set; }

    internal bool changesNotRecalculated;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int Modify(ValueModifier m, int specificIndex = -1)
    {
        int index = 0;
        if (specificIndex == -1)
        {
            if (modifierCount == modifiers.Length)
            {
                modifiers = new ValueModifier[modifiers.Length * 2];
                modifiers[modifierCount] = m;
                index = modifierCount++;
            }
            else
            {
                for (; index < modifiers.Length; index++)
                {
                    if (modifiers[index].Removed) break;
                }
            }
        }
        else index = specificIndex;
        
        if (modifiers[index].Equals(m)) return index;
        else modifiers[index] = m;
        changesNotRecalculated = true;
        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ModifyModifierValue (int index, float value)
    {
        if (index >= modifiers.Length) throw new System.IndexOutOfRangeException(string.Format("The value only has {0} modifiers, trying to modify the {1}", modifiers.Length, index)); 
        if (modifiers[index].Removed) throw new System.InvalidOperationException("Modifying a removed modifieris not allowed!");
        if (modifiers[index].sourceIndex != -1) throw new System.InvalidOperationException("Setting value on a dynamic modifier is not allowed!");
        if ((byte) modifiers[index].Action >= 100) throw new System.InvalidOperationException("Setting value on a" + modifiers[index].Action + " modifier is not allowed!");
        if (modifiers[index].value == value) return false;
        modifiers[index].value = value;
        changesNotRecalculated = true;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ModifyModifierSource (int index, int source)
    {
        if (index >= modifiers.Length) throw new System.IndexOutOfRangeException(string.Format("The value only has {0} modifiers, trying to modify the {1}", modifiers.Length, index));
        if (modifiers[index].Removed)throw new System.InvalidOperationException("Modifying a removed modifieris not allowed!");
        if ((byte) modifiers[index].Action >= 100) throw new System.InvalidOperationException("Setting source on a" + modifiers[index].Action + " modifier is not allowed!");
        if (modifiers[index].sourceIndex == source) return false;
        modifiers[index].sourceIndex = source;
        changesNotRecalculated = true;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ModifyModifierAction (int index, ModifierAction action)
    {
        if (index >= modifiers.Length) throw new System.IndexOutOfRangeException(string.Format("The value only has {0} modifiers, trying to modify the {1}", modifiers.Length, index));
        if (modifiers[index].Removed)throw new System.InvalidOperationException("Modifying a removed modifieris not allowed!");
        if (modifiers[index].Action == action) return false;
        modifiers[index].Action = action;
        changesNotRecalculated = true;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool RemoveModifier (int index)
    {
        if (modifiers[index].Removed) return false;
        changesNotRecalculated = true;
        modifiers[index].Removed = true;
        modifierCount--;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void MarkChanged ()
    {
        changesNotRecalculated = true;
    }
}
