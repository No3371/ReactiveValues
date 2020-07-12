public enum ModifierAction : byte
{
    SET = 1,
    ADD = 2,
    SUBTRACT = 3,
    MULTIPLY = 4,
    DEVIDE = 5,
    BeginGroup = 100,
    EndGroupADD = 101,
    EndGroupSUBTRACT = 102,
    EndGroupMULTIPLY = 103,
    EndGroupDEVIDE = 104,
    PlaceHolder = 255,
}
