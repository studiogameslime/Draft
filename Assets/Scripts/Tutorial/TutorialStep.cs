public enum TutorialStep
{
    // Battle phase – Round 1
    TapCell1,
    ExplainSouls,
    TapUnit1,
    TapCell2,
    TapUnit2,
    TapStartBattle,
    ExplainCapacity,
    WaitForRound1,

    // Battle phase – Round 2
    TapCell3,
    TapUnit3,
    TapStartBattle2,
    WaitForVictory,
    TapBackToHome,

    // Home screen phase
    TapMissions,
    CloseMissions,
    TapStoreTab,
    TapCollectionTab,
    TapBattleTab,
    TapPlay,

    Complete
}

public enum TutorialAction
{
    CellSelected,
    UnitPlaced,
    BattleStarted,
    RoundWon,
    LevelWon,
    BackToHome,
    MissionsOpened,
    MissionsClosed,
    PageChanged,
    PlayPressed,
    TapToContinue
}
