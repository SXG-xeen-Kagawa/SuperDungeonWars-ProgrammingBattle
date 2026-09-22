#if UNITY_EDITOR
using UnityEditor;
#endif

public enum BattleDeveloperSpectatorMode
{
    AllTeamsOr = 0,
    Team1Only = 1,
    Team2Only = 2,
    Team3Only = 3,
    Team4Only = 4,
}

public static class BattleDeveloperSpectatorSettings
{
#if UNITY_EDITOR
    public const string EditorPrefsModeKey =
        "MazeExplore.BattleDeveloperSpectator.Mode";
#endif

    public static BattleDeveloperSpectatorMode GetMode()
    {
#if UNITY_EDITOR
        int savedValue =
            EditorPrefs.GetInt(
                EditorPrefsModeKey,
                (int)BattleDeveloperSpectatorMode.AllTeamsOr);

        switch (savedValue)
        {
            case (int)BattleDeveloperSpectatorMode.Team1Only:
                return BattleDeveloperSpectatorMode.Team1Only;

            case (int)BattleDeveloperSpectatorMode.Team2Only:
                return BattleDeveloperSpectatorMode.Team2Only;

            case (int)BattleDeveloperSpectatorMode.Team3Only:
                return BattleDeveloperSpectatorMode.Team3Only;

            case (int)BattleDeveloperSpectatorMode.Team4Only:
                return BattleDeveloperSpectatorMode.Team4Only;

            default:
                return BattleDeveloperSpectatorMode.AllTeamsOr;
        }
#else
        return BattleDeveloperSpectatorMode.AllTeamsOr;
#endif
    }

#if UNITY_EDITOR
    public static void SetMode(
        BattleDeveloperSpectatorMode mode)
    {
        EditorPrefs.SetInt(
            EditorPrefsModeKey,
            (int)mode);
    }
#endif

    public static int GetRestrictedSystemTeamIndex()
    {
        switch (GetMode())
        {
            case BattleDeveloperSpectatorMode.Team1Only:
                return 0;

            case BattleDeveloperSpectatorMode.Team2Only:
                return 1;

            case BattleDeveloperSpectatorMode.Team3Only:
                return 2;

            case BattleDeveloperSpectatorMode.Team4Only:
                return 3;

            default:
                return -1;
        }
    }

}