
(Mod) STR * 1.05
- ATK = STR * 10
(Mod) ATK + 10
- DMG = ATK
(Mod) DMG * 1.05
- SPD = STR * 0.2 + DEX * 0.8
- LUCK = ((STR * 0.1 + DEX * 0.1) + SPD) * 0.5

> [MAKE] STR
    > [MOD] = 5
    > [MOD] * 1.05 = 5.25
------------------------------[VERSION]=0
VERSION(STR) = 1
> [MAKE] ATK
    > [MOD] + STR*10 = 52.5
    > [MOD] + 10 = 62.5
------------------------------[VERSION]=1
------------------------------[LAST_READ_VERSION] STR: 0 -> 1
VERSION(ATK) = 2
> [MAKE] DMG 
    > [MOD] = ATK = 62.5
    > [MOD] * 1.05 = 65.625
------------------------------[VERSION]=3
------------------------------[LAST_READ_VERSION] ATK: 0 -> 2
VERSION(DMG) = 3

> [MAKE] DEX
    > [MOD] = 1
------------------------------[VERSION]=4
VERSION(DEX) = 4
> [MAKE] SPD
    > [IN] 0
        > [MOD] + STR = 5.25
        > [MOD] * 0.2 = 1.05
    > [OUT] + 1.05 = 1.05
    > [IN] 1.05
        > [MOD] + DEX = 1
        > [MOD] * 0.8 = 0.8
    > [MOD] + 0.5 = 1.85
------------------------------[VERSION]=5
------------------------------[LAST_READ_VERSION] STR: 1 -> 4, DEX: 0 -> 4
VERSION(SPD) = 5
> [MAKE] LUCK
    > [IN] 0
        > [IN] 0
            > [MOD] + STR = 5.25
            > [MOD] * 0.1 = 0.525
        > [OUT] 0 + 0.525 = 0.525
        > [IN] 0.525
            > [MOD] + DEX = 1
            > [MOD] * 0.1 = 0.1
        > [OUT] 0.525 + 0.1 = 0.625
        > [MOD] 0.625 + SPD => 0.625 + 1.85 = 2.475
    > [OUT] 2.475
    > [MOD] 2.475 * 0.5 = 1.2375
------------------------------[VERSION]=6
------------------------------[LAST_READ_VERSION] STR: 4 -> 5, DEX: 4 -> 5, SPD: 0 -> 5
VERSION(SPD) = 6

> [>GET] LUCK
VERSION(STR) = 1, VERSION(ATK) = 2, VERSION(DMG) = 3, VERSION(DEX) = 4, VERSION(SPD) = 5, VERSION(LUCK) = 6
> [>GET] LUCK = 1.875
------------------------------[LAST_READ_VERSION] LUCK: 0 -> 6
> [>GET] LUCK
VERSION(STR) = 1, VERSION(ATK) = 2, VERSION(DMG) = 3, VERSION(DEX) = 4, VERSION(SPD) = 5, VERSION(LUCK) = 6
> [>GET] LUCK = 1.875
------------------------------[LAST_READ_VERSION] LUCK: 6
> [>GET] ATK
VERSION(STR) = 1, VERSION(ATK) = 2, VERSION(DMG) = 3, VERSION(DEX) = 4, VERSION(SPD) = 5, VERSION(LUCK) = 6
> [>GET] ATK = 65.625
------------------------------[LAST_READ_VERSION] ATK: 2 -> 6
> [>GET] ATK
VERSION(STR) = 1, VERSION(ATK) = 2, VERSION(DMG) = 3, VERSION(DEX) = 4, VERSION(SPD) = 5, VERSION(LUCK) = 6
> [>GET] ATK = 65.625
------------------------------[LAST_READ_VERSION] ATK: 6

> [SET] STR = 1
------------------------------[VERSION]=7
    <!-- > [MOD] STR * 1.05 = 1.05
        > [REACT] ATK = 1.05 * 10 = 10.5
        > [REACT] DMG = 10.5
            > [MOD] DMG * 1.05 = 11.025
        > [REACT] SPD = 1.05 * 0.5 + 1 * 0.5 = 1.025
        > [REACT] LUCK = ((0.105 + 0.1) + 1.025) * 0.5 = 0.615 -->

> [>GET] LUCK
VERSION(STR) = 7, VERSION(ATK) = 2, VERSION(DMG) = 3, VERSION(DEX) = 4, VERSION(SPD) = 5, VERSION(LUCK) = 6
    > [RECALC] ver(STR) > ver(LUCK)
        > [IN] 0
            > [IN] 0
                > [MOD] + STR = 1
                > [MOD] * 0.1 = 0.1
            > [OUT] 0 + 0.1 = 0.1
            > [IN] 0.1
                > [MOD] + DEX = 1
                > [MOD] * 0.1 = 0.1
            > [OUT] 0.1 + 0.1 = 0.2
            > [MOD] 0.2 + SPD => 0.2 + 3.125 = 3.325
        > [OUT] 3.325
        > [MOD] 3.325 * 0.5 = 1.6625
------------------------------[VERSION]=8
VERSION(STR) = 7, VERSION(ATK) = 2, VERSION(DMG) = 3, VERSION(DEX) = 4, VERSION(SPD) = 5, VERSION(LUCK) = 8
> [GET>] LUCK = 1.6625

> [SET] STR = 2
------------------------------[VERSION]=9
> [SET] DEX = 2
------------------------------[VERSION]=10

> [>GET] SPD
VERSION(STR) = 9, VERSION(ATK) = 2, VERSION(DMG) = 3, VERSION(DEX) = 10, VERSION(SPD) = 5, VERSION(LUCK) = 8
    > [RECALC] ver(STR) > ver(SPD), ver(DEX) > ver(SPD)
        > [IN] 0
            > [MOD] + STR = 2
            > [MOD] * 0.5 = 1
        > [OUT] + 1 = 1
        > [IN] 1
            > [MOD] + DEX = 2
            > [MOD] * 0.5 = 1
        > [OUT] 1 + 1 = 2
------------------------------[VERSION]=11
VERSION(STR) = 9, VERSION(ATK) = 2, VERSION(DMG) = 3, VERSION(DEX) = 10, VERSION(SPD) = 11, VERSION(LUCK) = 8
> [GET>] SPD = 2

> [>GET] DMG
VERSION(STR) = 9, VERSION(ATK) = 2, VERSION(DMG) = 3, VERSION(DEX) = 10, VERSION(SPD) = 11, VERSION(LUCK) = 8
    > [>GET] ATK
        > [RECALC] ver(STR) > ver(ATK)
            > [MOD] + STR = 2
            > [MOD] * 10 = 20
    > [GET>] ATK = 20
------------------------------[VERSION]=12
VERSION(STR) = 9, VERSION(ATK) = 12, VERSION(DMG) = 3, VERSION(DEX) = 10, VERSION(SPD) = 11, VERSION(LUCK) = 8
    > [RECALC] ver(ATK) > ver(DMG)
        > [MOD] = ATK = 20
        > [MOD] * 1.05 = 21
------------------------------[VERSION]=13
VERSION(STR) = 9, VERSION(ATK) = 12, VERSION(DMG) = 13, VERSION(DEX) = 10, VERSION(SPD) = 11, VERSION(LUCK) = 8
> [GET>] DMG = 21
