#define REACTIVE_VALUE_SYSTEM_LOGGING
using UnityEngine;

public class StatExample : MonoBehaviour
{
    public TMPro.TextMeshProUGUI str, atk, dex, spd, luck;
    public UnityEngine.UI.Text VERSION;
    public UnityEngine.UI.InputField TEXT_STR_BASE, TEXT_STR_MULTIPLIER, TEXT_ATK_STR_RATIO, TEXT_ATK_BONUS;
    public UnityEngine.UI.InputField TEXT_DEX_BASE;
    public UnityEngine.UI.InputField TEXT_SPD_STR_WEIGHT, TEXT_SPD_DEX_WEIGHT;
    public UnityEngine.UI.InputField TEXT_LUCK_STR_WEIGHT, TEXT_LUCK_DEX_WEIGHT, TEXT_LUCK_FINAL_MODIFIER;
    
    ReactiveValuesSystem stat;
    float strBase = 5, STR_MULTIPLIER = 1.05f, dexBase = 1, atk_strMultiplier = 10, atk_Bonus = 10;
    float SPD_STR_WEIGHT = 0.2f, SPD_DEX_WEIGHT= 0.8f;
    float luck_STRWeight = 0.1f, luck_DEXWeight = 0.1f, luckFinalMultiplier = 0.5f;
    int damageID, attackID, strengthID, speedID, dexterityID, luckID;
    public bool GetEveryFrame { get; set; }
    void Start ()
    {
        TEXT_STR_BASE.text = strBase.ToString("F2");
        TEXT_STR_MULTIPLIER.text = STR_MULTIPLIER.ToString("F2");
        TEXT_ATK_STR_RATIO.text = atk_strMultiplier.ToString("F2");
        TEXT_ATK_BONUS.text = atk_Bonus.ToString("F2");
        TEXT_DEX_BASE.text = dexBase.ToString("F2");
        TEXT_SPD_STR_WEIGHT.text = SPD_STR_WEIGHT.ToString("F2");
        TEXT_SPD_DEX_WEIGHT.text = SPD_DEX_WEIGHT.ToString("F2");
        TEXT_LUCK_STR_WEIGHT.text = luck_STRWeight.ToString("F2");
        TEXT_LUCK_DEX_WEIGHT.text = luck_DEXWeight.ToString("F2");
        TEXT_LUCK_FINAL_MODIFIER.text = luckFinalMultiplier.ToString("F2");

        #if REACTIVE_VALUE_SYSTEM_LOGGING
        ReactiveValuesSystem.logger = Debug.Log;
        #endif
        stat = new ReactiveValuesSystem();
        int i = 0;
        #if REACTIVE_VALUE_SYSTEM_LOGGING
        stat.NameValue(i++, "STR");
        stat.NameValue(i++, "ATK");
        stat.NameValue(i++, "DMG");
        stat.NameValue(i++, "DEX");
        stat.NameValue(i++, "SPD");
        stat.NameValue(i++, "LUCK");
        #endif
        strengthID = stat.MakeValue(new ValueModifier(ModifierAction.SET, strBase),
                                    new ValueModifier(ModifierAction.MULTIPLY, STR_MULTIPLIER)); //version: 1
        attackID = stat.MakeValue(new ValueModifier(ModifierAction.ADD, strengthID, stat.GetValue(strengthID)),
                                  new ValueModifier(ModifierAction.MULTIPLY, atk_strMultiplier),
                                  new ValueModifier(ModifierAction.ADD, atk_Bonus)); //version: 2
        damageID = stat.MakeValue(
            new ValueModifier(ModifierAction.SET, attackID, stat.GetValue(attackID)),
            new ValueModifier(ModifierAction.MULTIPLY, 1.05f)); //version: 3
        dexterityID = stat.MakeValue(new ValueModifier(ModifierAction.SET, dexBase));

        speedID = stat.MakeValue(
            new ValueModifier(ModifierAction.BeginGroup), // 0 -> CalculateGroup
                new ValueModifier(ModifierAction.ADD, strengthID, stat.GetValue(strengthID)), // 1
                new ValueModifier(ModifierAction.MULTIPLY, SPD_STR_WEIGHT), // 2
            new ValueModifier(ModifierAction.EndGroupADD), // 3 <-CalculateGroup
            new ValueModifier(ModifierAction.BeginGroup),
                new ValueModifier(ModifierAction.ADD, dexterityID, stat.GetValue(dexterityID)),
                new ValueModifier(ModifierAction.MULTIPLY, SPD_DEX_WEIGHT),
            new ValueModifier(ModifierAction.EndGroupADD));

        
        luckID = stat.MakeValue(
            new ValueModifier(ModifierAction.BeginGroup),
                new ValueModifier(ModifierAction.BeginGroup),
                    new ValueModifier(ModifierAction.ADD, strengthID, stat.GetValue(strengthID)), 
                    new ValueModifier(ModifierAction.MULTIPLY, luck_STRWeight),
                new ValueModifier(ModifierAction.EndGroupADD), 
                new ValueModifier(ModifierAction.BeginGroup),
                    new ValueModifier(ModifierAction.ADD, dexterityID, stat.GetValue(dexterityID)),
                    new ValueModifier(ModifierAction.MULTIPLY, luck_DEXWeight),
                new ValueModifier(ModifierAction.EndGroupADD),
            new ValueModifier(ModifierAction.EndGroupADD),
            new ValueModifier(ModifierAction.ADD, speedID, stat.GetValue(speedID)),
            new ValueModifier(ModifierAction.MULTIPLY, luckFinalMultiplier)
        );

        UpdateString();
    }
    public uint version;
    void Update ()
    {
        version = stat.SystemVersion;
        if (GetEveryFrame)
        {
            stat.GetValue(strengthID);
            stat.GetValue(attackID);
            stat.GetValue(dexterityID);
            stat.GetValue(speedID);
            stat.GetValue(luckID);
        }
    }

    public void SetSTRBase (float value)
    {
        strBase = value;
        stat.SetFixedModifierValue(strengthID, 0, strBase);
        stat.ApplyChanges();
        UpdateString();
    }

    public void SetSTRWithString (string valueString)
    {
        if (!float.TryParse(valueString, out float parsed)) return;
        SetSTRBase(parsed);
    }

    public void SetSTR_MULTIPLIER (float value)
    {
        STR_MULTIPLIER = value;
        stat.SetFixedModifierValue(strengthID, 1, STR_MULTIPLIER);
        stat.ApplyChanges();
        UpdateString();
    }

    public void SetSTR_MULTIPLIERWithString (string valueString)
    {
        if (!float.TryParse(valueString, out float parsed)) return;
        SetSTR_MULTIPLIER(parsed);
    }

    public void SetDEXBase (float value)
    {
        dexBase = value;
        stat.SetFixedModifierValue(dexterityID, 0, dexBase);
        stat.ApplyChanges();
        UpdateString();
    }

    public void SetDEXWithString (string valueString)
    {
        if (!float.TryParse(valueString, out float parsed)) return;
        SetDEXBase(parsed);
    }

    public void SetAtkMultiplier (float value)
    {
        atk_strMultiplier = value;
        stat.SetFixedModifierValue(attackID, 1, atk_strMultiplier);
        stat.ApplyChanges();
        UpdateString();
    }

    public void SetAtkMultiplierWithString (string valueString)
    {
        if (!float.TryParse(valueString, out float parsed)) return;
        SetAtkMultiplier(parsed);
    }

    public void OnATKBonusToggled (bool toggle)
    {
        if (!toggle) stat.RemoveModifier(attackID, 2);
        else stat.ReplaceModifier(attackID, 2, new ValueModifier(ModifierAction.ADD, atk_Bonus));
        stat.ApplyChanges();
        UpdateString();
    }

    public void SetAtkBonus (float value)
    {
        atk_Bonus = value;
        stat.SetFixedModifierValue(attackID, 2, atk_Bonus);
        stat.ApplyChanges();
        UpdateString();
    }

    public void SetAtk_bonusWithString (string valueString)
    {
        if (!float.TryParse(valueString, out float parsed)) return;
        SetAtkBonus(parsed);
    }

    public void SetSPD_STRWeight (float value)
    {
        SPD_STR_WEIGHT = value;
        stat.SetFixedModifierValue(speedID, 2, SPD_STR_WEIGHT);
        stat.ApplyChanges();
        UpdateString();
    }

    public void SetSPD_STRWeightWithString (string valueString)
    {
        if (!float.TryParse(valueString, out float parsed)) return;
        SetSPD_STRWeight(parsed);
    }

    public void SetSPD_DEXWeight (float value)
    {
        SPD_DEX_WEIGHT = value;
        stat.SetFixedModifierValue(speedID, 6, SPD_DEX_WEIGHT);
        stat.ApplyChanges();
        UpdateString();
    }

    public void SetSPD_DEXWeightWithString (string valueString)
    {
        if (!float.TryParse(valueString, out float parsed)) return;
        SetSPD_DEXWeight(parsed);
    }

    public void SetLuck_STR_Weight (float value)
    {
        luck_STRWeight = value;
        stat.SetFixedModifierValue(luckID, 3, luck_STRWeight);
        stat.ApplyChanges();
        UpdateString();
    }

    public void SetLuck_STR_WeightWithString (string valueString)
    {
        if (!float.TryParse(valueString, out float parsed)) return;
        SetLuck_STR_Weight(parsed);
    }

    public void SetLuckDEXWeight (float value)
    {
        luck_DEXWeight = value;
        stat.SetFixedModifierValue(luckID, 7, luck_DEXWeight);
        stat.ApplyChanges();
        UpdateString();
    }

    public void SetLuckDEXWeightWithString (string valueString)
    {
        if (!float.TryParse(valueString, out float parsed)) return;
        SetLuckDEXWeight(parsed);
    }

    public void SetLuckFinalMultiplier (float value)
    {
        luckFinalMultiplier = value;
        stat.SetFixedModifierValue(luckID, 11, luckFinalMultiplier);
        stat.ApplyChanges();
        UpdateString();
    }

    public void SetLuckFinalMultiplierWithString (string valueString)
    {
        if (!float.TryParse(valueString, out float parsed)) return;
        SetLuckFinalMultiplier(parsed);
    }
    public void UpdateString()
    {
        str.text = "STR:  " + stat.GetValue(strengthID).ToString("F4");
        atk.text = "ATK:  " + stat.GetValue(attackID).ToString("F4");
        dex.text = "DEX:  " + stat.GetValue(dexterityID).ToString("F4");
        spd.text = "SPD:  " + stat.GetValue(speedID).ToString("F4");
        luck.text = "LUCK: " + stat.GetValue(luckID).ToString("F4");
        VERSION.text = "VER:  " + stat.SystemVersion.ToString();
    }
}