// #define REACTIVE_VALUE_SYSTEM_LOGGING
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


public class FormulaBindings
{
    public (int sourceIndex, float value)[] bindings;
}

public class ReactiveValuesSystem
{
    const uint MAX_VERSION_DIFF_TOLERANCE = uint.MaxValue/2;
    // const uint MAX_VERSION_DIFF_TOLERANCE = 20;
    const uint MAX_VERSION_BEFORE_COMPRESS = uint.MaxValue-100;
    // const uint MAX_VERSION_BEFORE_COMPRESS = 100;
    const uint MAX_VERSION_BEFORE_COMPRESS_BUFFER = 100;
    public uint SystemVersion { get; private set; }
    public bool AnyChangesSinceLastGet { get; private set; }
    HashSet<int> changed;
    ModifiedDynamicFloat[] allValues;
    static ModifiedDynamicFloat[] formulas;
    FormulaBindings[] bindings;
    int valueCount = 0, startingValueBlockLength = 0;
    public ReactiveValuesSystem()
    {
        this.SystemVersion = 0;
        this.AnyChangesSinceLastGet = false;
        this.changed = new HashSet<int>();
        this.allValues = new ModifiedDynamicFloat[8];
    }

    public static int MakeFormula (params ValueModifier[] modifiers)
    {
        ModifiedDynamicFloat newFormula = new ModifiedDynamicFloat(modifiers);
    }

    public int MakeValue(params ValueModifier[] modifiers)
    {
        ModifiedDynamicFloat newValue = new ModifiedDynamicFloat(modifiers);
        while (allValues[startingValueBlockLength] != null) startingValueBlockLength++;
        allValues[startingValueBlockLength] = newValue;
        Recalculate(newValue);
        valueCount++;
        newValue.Version = SystemVersion++;
        LogConditional(string.Format("RxValues#{0}: [MAKE] {1}: {2} (ver{3}, s.ver{4})", SystemID, GetValueName(startingValueBlockLength), newValue.CachedModifiedValue, newValue.Version, SystemVersion));
        CompressIfVersionWillOverflow();
        // Never a node will point to this node before it's created so we don't chek for cyclic here.
        return startingValueBlockLength;
    }

    public int MakeValueAtIndex(int index, params ValueModifier[] modifiers)
    {
        if (allValues[index] != null) throw new System.ArgumentException("You can not create new value on existing value's slot!");
        ModifiedDynamicFloat newValue = new ModifiedDynamicFloat(modifiers);
        if (allValues.Length <= index)
        {
            int newCapacity = 0;
            while (allValues.Length <= index)
            {
                newCapacity = allValues.Length >= int.MaxValue / 2 ? int.MaxValue : allValues.Length * 2;
            }
            ModifiedDynamicFloat[] old = allValues;
            allValues = new ModifiedDynamicFloat[newCapacity];
            System.Array.Copy(old, allValues, old.Length);
        }
        allValues[index] = newValue;
        Recalculate(newValue);
        valueCount++;
        newValue.Version = SystemVersion++;
        LogConditional(string.Format("RxValues#{0}: [MAKE] {1}: {2} (ver{3}, s.ver{4})", SystemID, GetValueName(index), newValue.CachedModifiedValue, newValue.Version, SystemVersion));
        CompressIfVersionWillOverflow();
        // Never a node will point to this node before it's created so we don't chek for cyclic here.
        return index;
    }

    internal bool CheckCyclic(int target, int startIndex)
    {
        for (int i = 0; i < allValues[startIndex].modifiers.Length; i++)
        {
            if (allValues[startIndex].modifiers[i].sourceIndex == -1) continue;
            if (allValues[startIndex].modifiers[i].sourceIndex == target) return true;
            else return CheckCyclic(target, allValues[startIndex].modifiers[i].sourceIndex);
        }
        return false;
    }

    /// <summary>
    /// The function will traverse up through all dependencies, and update the values if needed
    /// If none change is performed, the step would be skipped.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    internal float GetValue(int index)
    {
        if (changed.Count > 0) throw new System.InvalidOperationException("All changes must be applied before getting values!");
        ModifiedDynamicFloat subject = allValues[index];
        if (subject == null) throw new System.NullReferenceException("Nonexist value at " + index);

        bool recalculate = false;
        if (subject.LastAccessedVersion < SystemVersion) // If this is true, this means
            for (int i = 0; i < subject.modifiers.Length; i++) // If any of the source values is newer then this, RECALCULATE 
            {
                int source = subject.modifiers[i].sourceIndex;
                if (source != -1)
                {
                    if (allValues[source].Version > subject.Version)
                    {
                        subject.modifiers[i].value = GetValue(source); // Update cache of source value
                        Recalculate(subject);
                        recalculate = true;
                        LogConditional(string.Format("RxValues#{0}: [RECALC] {1}: {2} (ver{3}, s.ver{4}) (reason: source updated)", SystemID, GetValueName(index), subject.CachedModifiedValue, subject.Version, SystemVersion));
                        break;
                    }
                }
            }

        if (SystemVersion - subject.Version > MAX_VERSION_DIFF_TOLERANCE) // Handling extreame case: Even if there's no value change, RECALCULATE to prevent any value being updated too rarely that makes the system can not handle version numver overflowing 
        {
            Recalculate(subject);
            recalculate = true;
            LogConditional(string.Format("RxValues#{0}: [RECALC] {1}: {2} (ver{3}, s.ver{4}) (reason: version too old, MAX_VERSION_DIFF_TOLERANCE triggered)", SystemID, GetValueName(index), subject.CachedModifiedValue, subject.Version, SystemVersion));
        }

        if (recalculate)
        {
            subject.changesNotRecalculated = false;
            subject.Version = SystemVersion++;
            CompressIfVersionWillOverflow();
        }

        subject.LastAccessedVersion = SystemVersion;
        return subject.CachedModifiedValue;
    }

    internal float RunFormula (int index)
    {
        if (index >)
        ModifiedDynamicFloat formula = formulas[index];
        if (formula == null) throw new System.NullReferenceException("Nonexist formula at " + index);
        
        if (changed.Count > 0) throw new System.InvalidOperationException("All changes must be applied before getting values!");
        ModifiedDynamicFloat subject = allValues[index];

        bool recalculate = false;
        if (subject.LastAccessedVersion < SystemVersion) // If this is true, this means
            for (int i = 0; i < subject.modifiers.Length; i++) // If any of the source values is newer then this, RECALCULATE 
            {
                int source = subject.modifiers[i].sourceIndex;
                if (source != -1)
                {
                    if (allValues[source].Version > subject.Version)
                    {
                        subject.modifiers[i].value = GetValue(source); // Update cache of source value
                        Recalculate(subject);
                        recalculate = true;
                        LogConditional(string.Format("RxValues#{0}: [RECALC] {1}: {2} (ver{3}, s.ver{4}) (reason: source updated)", SystemID, GetValueName(index), subject.CachedModifiedValue, subject.Version, SystemVersion));
                        break;
                    }
                }
            }

        if (SystemVersion - subject.Version > MAX_VERSION_DIFF_TOLERANCE) // Handling extreame case: Even if there's no value change, RECALCULATE to prevent any value being updated too rarely that makes the system can not handle version numver overflowing 
        {
            Recalculate(subject);
            recalculate = true;
            LogConditional(string.Format("RxValues#{0}: [RECALC] {1}: {2} (ver{3}, s.ver{4}) (reason: version too old, MAX_VERSION_DIFF_TOLERANCE triggered)", SystemID, GetValueName(index), subject.CachedModifiedValue, subject.Version, SystemVersion));
        }

        if (recalculate)
        {
            subject.changesNotRecalculated = false;
            subject.Version = SystemVersion++;
            CompressIfVersionWillOverflow();
        }

        subject.LastAccessedVersion = SystemVersion;
        return subject.CachedModifiedValue;
    }

    public int ModifyValue(int index, ValueModifier m)
    {
        if (m.sourceIndex != -1)
            if (!CheckCyclic(m.sourceIndex, index)) throw new System.StackOverflowException("Discovered cyclic dependencies!");

        changed.Add(index);
        ModifiedDynamicFloat subject = allValues[index];
        return subject.Modify(m);
    }

    public bool ReplaceModifier(int index, int modifierIndex, ValueModifier m)
    {
        if (m.sourceIndex != -1)
            if (!CheckCyclic(m.sourceIndex, index)) throw new System.StackOverflowException("Discovered cyclic dependencies!");

        changed.Add(index);
        ModifiedDynamicFloat subject = allValues[index];
        return subject.Modify(m, modifierIndex) != -1;
    }

    public void SetFixedModifierValue(int index, int modifierIndex, float value)
    {
        ModifiedDynamicFloat subject = allValues[index];
        if (subject.ModifyModifierValue(modifierIndex, value))
        {
            changed.Add(index);
        }
    }

    public void ModifyModifierValue(int index, int modifierIndex, int source)
    {
        ModifiedDynamicFloat subject = allValues[index];
        if (subject.ModifyModifierSource(modifierIndex, source))
        {
            changed.Add(index);
        }
    }
    public void ModifyModifierAction(int index, int modifierIndex, ModifierAction action)
    {
        ModifiedDynamicFloat subject = allValues[index];
        if (subject.ModifyModifierAction(modifierIndex, action))
        {
            changed.Add(index);
        }
    }

    public void RemoveModifier(int index, int modifierIndex)
    {
        ModifiedDynamicFloat subject = allValues[index];
        if (subject.RemoveModifier(modifierIndex))
        {
            changed.Add(index);
        }
    }

    public void ApplyChanges()
    {
        foreach (int index in changed)
        {
            ModifiedDynamicFloat subject = allValues[index];
            if (!subject.changesNotRecalculated) continue;

            Recalculate(subject);
            subject.changesNotRecalculated = false;
            subject.Version = SystemVersion++;
            LogConditional(string.Format("RxValues#{0}: [RECALC] {1}: {2} (ver{3}, s.ver{4}) (reason: apply changes)", SystemID, GetValueName(index), subject.CachedModifiedValue, subject.Version, SystemVersion));
            CompressIfVersionWillOverflow();
        }
        changed.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Recalculate(ModifiedDynamicFloat subject)
    {
        subject.CachedModifiedValue = 0;
        for (int i = 0; i < subject.modifiers.Length; i++)
        {
            if (subject.modifiers[i].Removed) continue;
            switch (subject.modifiers[i].Action)
            {
                case ModifierAction.SET:
                    subject.CachedModifiedValue = subject.modifiers[i].value;
                    break;
                case ModifierAction.ADD:
                    subject.CachedModifiedValue += subject.modifiers[i].value;
                    break;
                case ModifierAction.SUBTRACT:
                    subject.CachedModifiedValue -= subject.modifiers[i].value;
                    break;
                case ModifierAction.MULTIPLY:
                    subject.CachedModifiedValue *= subject.modifiers[i].value;
                    break;
                case ModifierAction.DEVIDE:
                    subject.CachedModifiedValue /= subject.modifiers[i].value;
                    break;
                case ModifierAction.BeginGroup:
                    float result = CalculateGroup(subject, i, out i, out ModifierAction nestedGroupAction);
                    switch (nestedGroupAction)
                    {
                        case ModifierAction.EndGroupADD:
                            subject.CachedModifiedValue += result;
                            break;
                        case ModifierAction.EndGroupSUBTRACT:
                            subject.CachedModifiedValue -= result;
                            break;
                        case ModifierAction.EndGroupMULTIPLY:
                            subject.CachedModifiedValue *= result;
                            break;
                        case ModifierAction.EndGroupDEVIDE:
                            subject.CachedModifiedValue /= result;
                            break;
                        default:
                            throw new System.ArgumentException("Not EndGroupXXX action when calculated group!");
                    }
                    break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    float CalculateGroup(ModifiedDynamicFloat subject, int groupStart, out int groupEnd, out ModifierAction groupAction)
    {
        float groupCache = 0;
        for (int i = groupStart + 1; i < subject.modifiers.Length; i++)
        {
            if (subject.modifiers[i].Removed) continue;
            switch (subject.modifiers[i].Action)
            {
                case ModifierAction.SET:
                    groupCache = subject.modifiers[i].value;
                    break;
                case ModifierAction.ADD:
                    groupCache += subject.modifiers[i].value;
                    break;
                case ModifierAction.SUBTRACT:
                    groupCache -= subject.modifiers[i].value;
                    break;
                case ModifierAction.MULTIPLY:
                    groupCache *= subject.modifiers[i].value;
                    break;
                case ModifierAction.DEVIDE:
                    groupCache /= subject.modifiers[i].value;
                    break;
                case ModifierAction.BeginGroup:
                    float result = CalculateGroup(subject, i, out i, out ModifierAction nestedGroupAction);
                    switch (nestedGroupAction)
                    {
                        case ModifierAction.EndGroupADD:
                            groupCache += result;
                            break;
                        case ModifierAction.EndGroupSUBTRACT:
                            groupCache -= result;
                            break;
                        case ModifierAction.EndGroupMULTIPLY:
                            groupCache *= result;
                            break;
                        case ModifierAction.EndGroupDEVIDE:
                            groupCache /= result;
                            break;
                        default:
                            throw new System.ArgumentException("Not EndGroupXXX action when calculated group!");
                    }
                    break;
                case ModifierAction.EndGroupADD:
                case ModifierAction.EndGroupSUBTRACT:
                case ModifierAction.EndGroupMULTIPLY:
                case ModifierAction.EndGroupDEVIDE:
                    groupEnd = i;
                    groupAction = subject.modifiers[i].Action;
                    return groupCache;
            }
        }
        throw new System.InvalidOperationException("A modifier group must be ended with EndGroupXXX action!");
    }

    public void CompressIfVersionWillOverflow()
    {
        if (SystemVersion < MAX_VERSION_BEFORE_COMPRESS - MAX_VERSION_BEFORE_COMPRESS_BUFFER) return;
        uint minVersion = uint.MaxValue;
        int minIndex = 0;
        for (int i = 0; i < allValues.Length; i++)
        {
            ModifiedDynamicFloat subject = allValues[i];
            if (subject == null) continue;
            if (SystemVersion - subject.Version > MAX_VERSION_DIFF_TOLERANCE && SystemVersion < MAX_VERSION_BEFORE_COMPRESS - 1) // Handling extreame case: Even if there's no value change, RECALCULATE to prevent any value being updated too rarely that makes the system can not handle version numver overflowing 
            {
                Recalculate(subject);
                LogConditional(string.Format("RxValues#{0}: [COMPRESS] [RECALC] {1}: {2} (ver{3}, s.ver{4}) (reason: version too old, MAX_VERSION_DIFF_TOLERANCE triggered)", SystemID, GetValueName(i), subject.CachedModifiedValue, subject.Version, SystemVersion));
                subject.changesNotRecalculated = false;
                subject.Version = SystemVersion++;
            }
            if (subject.Version >= minVersion) continue;
            minVersion =  subject.Version;
            minIndex = i;
        }

        for (int i = 0; i < allValues.Length; i++)
        {
            if (allValues[i] == null) continue;

            allValues[i].Version -= minVersion;
        }
        SystemVersion -= minVersion;
        LogConditional(string.Format("RxValues#{0}: [COMPRESS] MinAt: {1} ({2}), Shifted: -{3}", SystemID, minIndex, GetValueName(minIndex), minVersion));
    }

    private Dictionary<int, string> stringToValueID;
    Dictionary<int, string> StringToValueID { get => stringToValueID = stringToValueID?? new Dictionary<int, string>();}
    public static System.Action<string> logger;

    [System.Diagnostics.Conditional("REACTIVE_VALUE_SYSTEM_LOGGING")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogConditional(string message)
    {
        logger?.Invoke(message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void NameValue(int id, string name)
    {
        StringToValueID.Add(id, name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetValueName(int id)
    {
        if (StringToValueID.ContainsKey(id)) return StringToValueID[id];
        else return "Unnamed";
    }

    public string SystemID { get; set; } = "Unnamed";
}
