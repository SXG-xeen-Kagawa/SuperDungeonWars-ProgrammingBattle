/// <summary>
/// SE識別子。
/// ScriptableObjectに保存されるため、既存の数値は変更しないでください。
/// </summary>
public enum SeId
{
    None = 0,

    // ゲーム：システム・通知音
    Countdown = 10,
    BattleStart = 20,
    BattleEnd = 90,
    TimeWarning = 130,

    // ゲーム：画面内の現場音
    AttackHit = 30,
    TreasurePickUp = 50,
    TreasureThrow = 110,
    WallImpact = 120,

    // ゲーム：納品完了の通知音
    TreasureExport = 60,

    // リザルト：演出音
    TreasureOpen = 70,
    Coin = 80,
    WinnerJingle = 140,

    // 共通：画面切り替え演出
    ScreenTransition = 150,

}

/// <summary>
/// BGM識別子。
/// ScriptableObjectに保存されるため、既存の数値は変更しないでください。
/// </summary>
public enum BgmId
{
    None = 0,
    TeamIntroduction = 10,
    Battle = 20,
    Result = 30,
}

/// <summary>
/// 同じBGMを要求した場合の動作。
/// </summary>
public enum SameBgmBehaviour
{
    KeepPlaying = 0,
    RestartWithCrossFade = 1,
}