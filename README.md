# ReactiveValues
An experimental implmentation of system of relative values that auto get recalculated based on defined dependencies.


## ValueSystem, Value, Modifier, Formula, FormulaBind
- **ValueSystem**: represent a system of related *Values*.
- **Value**: contains a set of modifiers and a base value
- **Modifier**: composed of a action and A) a value if it's a fixed value modifier or B) a reference to a *Formula*/*Value*
- **Formula**: a *Value* object (so it can be reference by other *Values*) shared by all systems. *ValueSystems* passes in parameters to fill the formmula. The core purpose is to save memory space from duplicated Value/Modifier Sets in a lot of similar/identical *ValueSystems*, in other words, a *Value* belongs to individual *ValueSystem*, *Formulas* are shared by all systems. It's a bit more stateful then a Ordered Modifiers Preset, a set of Ordered Action.
    Unlike *Value*, everytime it's used, the result is not cached.
    It's useful to save memory space when a lot of *ValueSystems* share a complex rule, e.g. 100000 players shares `LUCK = (ATK + DEX + INT + CON + CHARM) * 0.1`.
  - EX1: `ATK = STR * 10` is a *Formula* which needs 1 parameter to run.
```
  - Value: {
    BaseValue: 0
    Modifiers: [
      { SET, p:0 }
      { MULTIPLY, 10}
    ]
```
  - EX2: `DMG = 1 + ATK + DMG_BONUS` is a *Formula* which needs 2 parameter to run.
``` 
  - Value: {
    BaseValue: 1
    Modifiers: [
      { ADD, p:0 }
      { ADD, p:1 }
    ]
  }
```
- **FormulaBinding**: binding a Value to a formula. It's the bridge between *ValueSystem* and *Formula*. It's reference object and can be shared by *ValueSystems*.
- **RootValue**: *ValueSystems* will keep track of *Values* that does not reference to other *Values*. This is important because this value is the input of the system, the actual state. All *Values* except *RootValues* depends on *RootValues*.

## Design Samples
If you have more then one players/characters/statsOwners in a RPG game, you use this framework to maintain their stats, every one of them is a independent *ValuesSystem*. However they may share *Formulas*, for example, all players use `ATK = STR * 10` to calculate  their attacking power, so instead of spamming this *Value* in every *ValueSystem*:
```
  - ATK: {
    BaseValue: 0
    Modifiers: [
      { SET, ref:STR }
      { MULTIPLY, 10}
    ]
  }
```
you create the *Formula*:
``` 
  - Formula[0]: {
    BaseValue: 0
    Modifiers: [
      { SET, p:0 }
      { MULTIPLY, 10}
    ]
  }
```
make the ATK use the *Formula*:
```
  - ATK: {
    BaseValue: 0
    Modifiers: [
      { SET, ref:0, isFormula }
    ]
  }
```
now make the binding:
```
  ValueSystem {
    Bindings: [
      [0]: FormulaBinding {
        Paramters: [
          { Reference = ref:STR, float Value }...
        ]
      }
    ]
  }
```
So now when the *ValueSystem* is calculating ATK, it looks for the Formula[0], feed it with the system's STR, and have the result as ATK.

## Usage Guideline
Although all the *ValueSystems*, *Values*, *Modifiers* is totally serializable and can be easily saved, it is not recommended to save states in this way, for example, save your game  character stats by save the whole *ValueSystem*. You have to be aware about that the system is the **relationships** of values, not the values itself; It's the model, not the data. If you save a *ValueSystem*, you are saving the **relationship**, not the values! This is something I keep struggling and being confused about when designing the system.

During project lifecycle, it's possible that you needs to change existing value rules, unless you are sure you'll never change the value relationships, you have to make sure that you can easily migrates between **relationship** changes.

For example, a update of your game change the rule from `ATK = STR * 10` to `ATK = (STR + DEX) * 5`, and maybe some more changes, if the way you are saving the stats of your players is save the whole systems, maybe you'll be in big trouble because you have to modify every player's existing *ValueSystem* to the new structure, all manually.

It's much better to just save the *RootValues* and recreate the *ValueSystem* every time, in this way, what takes to change rules is just the modification of the code you generate the *ValueSystem*. You just fill in the *RootV

## The benefits
You may thinks... 'Why should I use this? Why can't I just do ATK = 3 * 10 when I change STR to 3?'
Sure, `ATK = STR * 10` is simple. `ATK = (STR + DEX) * 5` is not worser. How about:

> DMG = (((ATK + BASE_ATK_MODIFIER) * ATK_MULTIPLIER + ATK_MODIFIER) * ATK_TO_DMG_RATIO + DMG_MODIFIER) * FINAL_DMG_MULTIPLIER

Leme try to convert this to C#:

```CSharp
float newDmg;
newDmg = atk + baseAtkModifier;
newDmg = newDmg * atkMultiplier + atkModifier;
newDmg = newDmg * atkToDmgRatio + dmgModifier;
newDmg *= finalDmgMultiplier; 
```

- If it's a MMO, how much calculation do you need when you have 10000 players?
- When do you run this recalculation? You cache `DMG` so you will not always recalculate it, but you have to do it when any of [`ATK`, `BASE_ATK_MODIFIER`, `ATK_MULTIPLIER`, `ATK_MODIFIER`, `ATK_TO_DMG_RATIO`, `DMG_MODIFIER`, `FINAL_DMG_MULTIPLIER`] is changed, right? How do you know that, these dependencies are changed so you have to recalculate?
- So to know when you want to push the change to `ATK`, so all values depends on it will recalculate, how do you know what values depends on it? What values to recalcualte?
Do You write:
    ```CSharp
    atk += 10;
    RecalculateDMG();
    ```
    or 
    ```CSharp
    atkModifier += 10;
    RecalculateDMG();
    ```
    here, there, everywhere? Right, you should improve it a bit:
    ```CSharp
    void SetATK(float value)
    {
      atk = value;
      RecalculateDMG();
    }

    void SetBaseAtkModifier(float value) {...}
    void SetAtkMultiplier(float value) {...}
    void SetAtkModifier(float value) {...}
    void SetAtkToDmgRatio(float value) {...}
    void SetDmgModifier(float value) {...}
    void SetFinalDmgMultiplier(float value) {...}

    void SetDMG(float value)
    {
      dmg = value;
      RecalculateOtherStuffDependsOnDMG();
    }

    void RecalculateDMG()
    {
      float newDmg;
      newDmg = atk + baseAtkModifier;
      newDmg = newDmg * atkMultiplier + atkModifier;
      newDmg = newDmg * atkToDmgRatio + dmgModifier;
      newDmg *= finalDmgMultiplier; 
      SetDMG(dmg);
    }

    void main ()
    {
      SetATK(10);
      SetBaseAtkModifier(1); // From a passive skill: Base ATK + 1
      SetAtkMultiplier(2); // From a weapon have 200% power
      SetAtkModifier(10); // From ATK+ potion
      SetAtkToDmgRatio(1); // Default value
      SetDmgModifier(-100); // From a debuff that weakening you
      SetFinalDmgMultiplier(2); // From a game mode that everybody deals double damage
    }
    ```
Several observations:
- This is a very simple system, not enough for a RPG, how many codes you'll have to write and keep maintaining for a complex 4X e.g. Stellaris
- The example is a very simple 1 level structure. What if there're other values depends on `DMG`? What if all these values `DMG` depends on depends on somethings else? What if deep inside the `RecalculateOtherStuffDependsOnDMG()` you unintentionally modified `ATK`... you never know before you encounter StackOverflowException.
- `SetAtkToDmgRatio(1); // Default value` does not actually change the value, but the recalculation will occurs if you do not check the equality. Do you want to add a if to before all the Set()?
- All the calculation before the last Set() is wasted, you only needs the latest DMG... Do you want to come back to the old ways so:
    ```CSharp
    atk = 10;
    baseAtkModifier = 1;
    atkMultiplier = 2;
    atkModifier = 10;
    atkToDmgRatio = 1;
    dmgModifier = -100;
    finalDmgMultiplier = 2;
    ```
    whenver any of the lines appears in your codebase you place a `RecalculateDMG();`... Sounds cool.

You may have realized that, in this way, the model of all these values exist in your head, or on some designer documentation, it's all your labor to maintain the implementation of the model. Modern problems require modern solutions. It's time to introduce a automatic system to take over the burden.
