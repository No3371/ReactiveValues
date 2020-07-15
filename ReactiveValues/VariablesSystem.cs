// #define REACTIVE_VALUE_SYSTEM_LOGGING
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


public class FormulaBindings
{
    public (int sourceIndex, float value)[] bindings;
}

public class VariablesSystem
{
    const uint MAX_VERSION_DIFF_TOLERANCE = uint.MaxValue/2;
    // const uint MAX_VERSION_DIFF_TOLERANCE = 20;
    const uint MAX_VERSION_BEFORE_COMPRESS = uint.MaxValue-100;
    // const uint MAX_VERSION_BEFORE_COMPRESS = 100;
    const uint MAX_VERSION_BEFORE_COMPRESS_BUFFER = 100;
    public uint SystemVersion { get; private set; }
    public bool AnyChangesSinceLastGet { get; private set; }
    private HashSet<int> changed;
    private Variable[] allValues;
    static Variable[] formulas;
    private static int startingFormulaBlockLength = 0;
    private static Dictionary<int, string> formulaName;
    private FormulaBindings[] bindings;
    private static Dictionary<int, string> FormulaName { get => formulaName = formulaName?? new Dictionary<int, string>(); }
    private FormulaBindings[] Bindings { get => bindings = bindings ?? new FormulaBindings[1]; set => bindings = value; }
    private static Variable[] Formulas { get => formulas = formulas ?? new Variable[1]; set => formulas = value; }
    int valueCount = 0, startingValueBlockLength = 0;
    public VariablesSystem()
    {
        this.SystemVersion = 0;
        this.AnyChangesSinceLastGet = false;
        this.changed = new HashSet<int>();
        this.allValues = new Variable[8];
    }

    public static int MakeFormula (string ID = null, params Modifier[] modifiers)
    {
        while (Formulas[startingFormulaBlockLength] != null) startingFormulaBlockLength++;
        MakeFormulaInternal(startingFormulaBlockLength, modifiers);
        if (ID != null) FormulaName.Add(startingFormulaBlockLength, ID);
        LogConditional(string.Format("VariablesSystem: [MAKE_FORMULA] {0}: {1}", ID, startingFormulaBlockLength));
        return startingFormulaBlockLength;
    }

    public static int MakeFormulaAtIndex(int index, string ID = null, params Modifier[] modifiers)
    {
        if (Formulas[index] != null) throw new System.ArgumentException("You can not create new formula on existing formula's slot!");
        MakeFormulaInternal(index, modifiers);
        if (ID != null) FormulaName.Add(startingFormulaBlockLength, ID);
        LogConditional(string.Format("VariablesSystem: [MAKE_FORMULA] {0}: {1}", ID, index));
        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void MakeFormulaInternal (int index, params Modifier[] modifiers)
    {
        if (Formulas.Length <= index)
        {
            int newCapacity = 0;
            while (Formulas.Length <= index)
            {
                newCapacity = Formulas.Length >= int.MaxValue / 2 ? int.MaxValue : Formulas.Length * 2;
            }
            Variable[] old = Formulas;
            Formulas = new Variable[newCapacity];
            System.Array.Copy(old, Formulas, old.Length);
        }
        Variable newFormula = new Variable(modifiers);
        Formulas[index] = newFormula;
    }

    public void Bind (int formulaIndex, FormulaBindings binding)
    {
        if (Bindings.Length <= formulaIndex)
        {
            int newCapacity = 0;
            while (Bindings.Length <= formulaIndex)
            {
                newCapacity = Bindings.Length >= int.MaxValue / 2 ? int.MaxValue : Bindings.Length * 2;
            }
            FormulaBindings[] old = Bindings;
            Bindings = new FormulaBindings[newCapacity];
            System.Array.Copy(old, Bindings, old.Length);
        }
        Bindings[formulaIndex] = binding;
    }

    public int MakeValue(params Modifier[] modifiers)
    {
        while (allValues[startingValueBlockLength] != null) startingValueBlockLength++;
        MakeVariableInternal(startingValueBlockLength);
        // Never a node will point to this node before it's created so we don't chek for cyclic here.
        return startingValueBlockLength;
    }

    public int MakeValueAtIndex(int index, params Modifier[] modifiers)
    {
        if (allValues[index] != null) throw new System.ArgumentException("You can not create new value on existing value's slot!");
        MakeVariableInternal(index, modifiers);
        // Never a node will point to this node before it's created so we don't chek for cyclic here.
        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MakeVariableInternal (int index, params Modifier[] modifiers)
    {
        if (allValues.Length <= index)
        {
            int newCapacity = 0;
            while (allValues.Length <= index)
            {
                newCapacity = allValues.Length >= int.MaxValue / 2 ? int.MaxValue : allValues.Length * 2;
            }
            Variable[] old = allValues;
            allValues = new Variable[newCapacity];
            System.Array.Copy(old, allValues, old.Length);
        }
        Variable newValue = new Variable(modifiers);
        allValues[index] = newValue;
        Recalculate(newValue);
        valueCount++;
        newValue.Version = SystemVersion++;
        LogConditional(string.Format("VariablesSystem#{0}: [MAKE] {1}: {2} (ver{3}, s.ver{4})", SystemID, GetValueName(index), newValue.CachedModifiedValue, newValue.Version, SystemVersion));
        CompressIfVersionWillOverflow();
    }

    internal bool CheckCyclic(int target, int startIndex)
    {
        for (int i = 0; i < allValues[startIndex].modifiers.Length; i++)
        {
            if (allValues[startIndex].modifiers[i].source == -1) continue;
            if (allValues[startIndex].modifiers[i].source == target) return true;
            else return CheckCyclic(target, allValues[startIndex].modifiers[i].source);
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
        Variable subject = allValues[index];
        if (subject == null) throw new System.NullReferenceException("Nonexist value at " + index);

        bool recalculate = false;
        if (subject.LastAccessedVersion < SystemVersion) // If this is true, this means
        {
            for (int i = 0; i < subject.modifiers.Length; i++) // If any of the source values is newer then this, RECALCULATE 
            {
                Modifier modifier = subject.modifiers[i];
                if (modifier.source != -1)
                {
                    if (allValues[modifier.source].Version > subject.Version)
                    {
                        subject.modifiers[i].value = modifier.sourceIsFormula? RunFormula(modifier.source) : GetValue(modifier.source); // Update cache of source value
                        Recalculate(subject);
                        recalculate = true;
                        LogConditional(string.Format("VariablesSystem#{0}: [RECALC] {1}: {2} (ver{3}, s.ver{4}) (reason: source updated)", SystemID, GetValueName(index), subject.CachedModifiedValue, subject.Version, SystemVersion));
                        break;
                    }
                }
            }
        }

        if (SystemVersion - subject.Version > MAX_VERSION_DIFF_TOLERANCE) // Handling extreame case: Even if there's no value change, RECALCULATE to prevent any value being updated too rarely that makes the system can not handle version numver overflowing 
        {
            Recalculate(subject);
            recalculate = true;
            LogConditional(string.Format("VariablesSystem#{0}: [RECALC] {1}: {2} (ver{3}, s.ver{4}) (reason: version too old, MAX_VERSION_DIFF_TOLERANCE triggered)", SystemID, GetValueName(index), subject.CachedModifiedValue, subject.Version, SystemVersion));
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
        Variable formula = Formulas[index];
        if (formula == null) throw new System.NullReferenceException("Nonexist formula at " + index);
        if (changed.Count > 0) throw new System.InvalidOperationException("All changes must be applied before running values!");
        if (Bindings.Length <= index || Bindings[index] == null) throw new System.NullReferenceException("The system has not bind to the formula# " + index);
        formula.CachedModifiedValue = formula.BaseValue;
        for (int i = 0; i < formula.modifiers.Length; i++)
        {
            var binding = Bindings[index].bindings[formula.modifiers[i].source];
            if (binding.sourceIndex != -1)
                formula.modifiers[i].value = GetValue(binding.sourceIndex);
            else
                formula.modifiers[i].value = binding.value;
        }
        for (int i = 0; i < formula.modifiers.Length; i++)
        {
            if (formula.modifiers[i].Removed) continue;
            switch (formula.modifiers[i].Action)
            {
                case ModifierAction.SET:
                    formula.CachedModifiedValue = formula.modifiers[i].value;
                    break;
                case ModifierAction.ADD:
                    formula.CachedModifiedValue += formula.modifiers[i].value;
                    break;
                case ModifierAction.SUBTRACT:
                    formula.CachedModifiedValue -= formula.modifiers[i].value;
                    break;
                case ModifierAction.MULTIPLY:
                    formula.CachedModifiedValue *= formula.modifiers[i].value;
                    break;
                case ModifierAction.DEVIDE:
                    formula.CachedModifiedValue /= formula.modifiers[i].value;
                    break;
                case ModifierAction.BeginGroup:
                    float result = CalculateGroup(formula, i, out i, out ModifierAction nestedGroupAction);
                    switch (nestedGroupAction)
                    {
                        case ModifierAction.EndGroupADD:
                            formula.CachedModifiedValue += result;
                            break;
                        case ModifierAction.EndGroupSUBTRACT:
                            formula.CachedModifiedValue -= result;
                            break;
                        case ModifierAction.EndGroupMULTIPLY:
                            formula.CachedModifiedValue *= result;
                            break;
                        case ModifierAction.EndGroupDEVIDE:
                            formula.CachedModifiedValue /= result;
                            break;
                        default:
                            throw new System.ArgumentException("Not EndGroupXXX action when calculated group!");
                    }
                    break;
            }
        }

        return formula.CachedModifiedValue;
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ModifyBaseValue(int index, float baseValue)
    {
        Variable subject = allValues[index];
        if (subject.BaseValue == baseValue) return;
        changed.Add(index);
        subject.BaseValue = baseValue;
        subject.changesNotRecalculated = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ModifyValue(int index, Modifier m)
    {
        if (m.source != -1)
            if (!CheckCyclic(m.source, index)) throw new System.StackOverflowException("Discovered cyclic dependencies!");

        changed.Add(index);
        Variable subject = allValues[index];
        return subject.Modify(m);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReplaceModifier(int index, int modifierIndex, Modifier m)
    {
        if (m.source != -1)
            if (!CheckCyclic(m.source, index)) throw new System.StackOverflowException("Discovered cyclic dependencies!");

        changed.Add(index);
        Variable subject = allValues[index];
        return subject.Modify(m, modifierIndex) != -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFixedModifierValue(int index, int modifierIndex, float value)
    {
        Variable subject = allValues[index];
        if (subject.ModifyModifierValue(modifierIndex, value))
        {
            changed.Add(index);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ModifyModifierValue(int index, int modifierIndex, int source)
    {
        Variable subject = allValues[index];
        if (subject.ModifyModifierSource(modifierIndex, source))
        {
            changed.Add(index);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ModifyModifierAction(int index, int modifierIndex, ModifierAction action)
    {
        Variable subject = allValues[index];
        if (subject.ModifyModifierAction(modifierIndex, action))
        {
            changed.Add(index);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveModifier(int index, int modifierIndex)
    {
        Variable subject = allValues[index];
        if (subject.RemoveModifier(modifierIndex))
        {
            changed.Add(index);
        }
    }

    public void ApplyChanges()
    {
        foreach (int index in changed)
        {
            Variable subject = allValues[index];
            if (!subject.changesNotRecalculated) continue;

            Recalculate(subject);
            subject.changesNotRecalculated = false;
            subject.Version = SystemVersion++;
            LogConditional(string.Format("VariablesSystem#{0}: [RECALC] {1}: {2} (ver{3}, s.ver{4}) (reason: apply changes)", SystemID, GetValueName(index), subject.CachedModifiedValue, subject.Version, SystemVersion));
            CompressIfVersionWillOverflow();
        }
        changed.Clear();
    }

    void Recalculate(Variable subject)
    {
        subject.CachedModifiedValue = subject.BaseValue;
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

    float CalculateGroup(Variable subject, int groupStart, out int groupEnd, out ModifierAction groupAction)
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
            Variable subject = allValues[i];
            if (subject == null) continue;
            if (SystemVersion - subject.Version > MAX_VERSION_DIFF_TOLERANCE && SystemVersion < MAX_VERSION_BEFORE_COMPRESS - 1) // Handling extreame case: Even if there's no value change, RECALCULATE to prevent any value being updated too rarely that makes the system can not handle version numver overflowing 
            {
                Recalculate(subject);
                LogConditional(string.Format("VariablesSystem#{0}: [COMPRESS] [RECALC] {1}: {2} (ver{3}, s.ver{4}) (reason: version too old, MAX_VERSION_DIFF_TOLERANCE triggered)", SystemID, GetValueName(i), subject.CachedModifiedValue, subject.Version, SystemVersion));
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
        LogConditional(string.Format("VariablesSystem#{0}: [COMPRESS] MinAt: {1} ({2}), Shifted: -{3}", SystemID, minIndex, GetValueName(minIndex), minVersion));
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
