#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public sealed class BattleDeveloperSpectatorWindow
    : EditorWindow
{
    private static readonly string[] s_modeLabels =
    {
        "全チーム統合表示",
        "チーム1のみ",
        "チーム2のみ",
        "チーム3のみ",
        "チーム4のみ",
    };

    [MenuItem("Tools/プロバト/開発用観戦表示")]
    private static void OpenWindow()
    {
        BattleDeveloperSpectatorWindow window =
            GetWindow<BattleDeveloperSpectatorWindow>();

        window.titleContent =
            new GUIContent("開発用観戦表示");

        window.minSize =
            new Vector2(360.0f, 220.0f);

        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8.0f);

        EditorGUILayout.LabelField(
            "開発用観戦表示",
            EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "この設定は EditorPrefs に保存されます。\n"
            + "ビルド版では設定値にかかわらず、"
            + "常に「全チーム統合表示」と通常の自動観戦になります。",
            MessageType.Info);

        EditorGUILayout.Space(8.0f);

        BattleDeveloperSpectatorMode currentMode =
            BattleDeveloperSpectatorSettings.GetMode();

        int selectedModeIndex =
            EditorGUILayout.Popup(
                "観戦モード",
                (int)currentMode,
                s_modeLabels);

        BattleDeveloperSpectatorMode selectedMode =
            (BattleDeveloperSpectatorMode)selectedModeIndex;

        if (selectedMode != currentMode)
        {
            BattleDeveloperSpectatorSettings.SetMode(
                selectedMode);

            RefreshBattleSpectatorViews();
        }

        EditorGUILayout.Space(12.0f);

        DrawModeDescription(
            BattleDeveloperSpectatorSettings.GetMode());

        EditorGUILayout.Space(12.0f);

        if (GUILayout.Button("現在の設定を再適用"))
        {
            RefreshBattleSpectatorViews();
        }
    }

    private static void DrawModeDescription(
        BattleDeveloperSpectatorMode mode)
    {
        string description;

        switch (mode)
        {
            case BattleDeveloperSpectatorMode.Team1Only:
                {
                    description =
                        "チーム1だけを開発用観戦対象にします。\n"
                        + "左上ミニマップはチーム1の既知情報だけを表示し、\n"
                        + "右上3Dカメラの自動フォーカス候補も"
                        + "チーム1だけに制限されます。";

                    break;
                }

            default:
                {
                    description =
                        "イベント配信用の標準表示です。\n"
                        + "左上ミニマップは全チームの既知情報を統合して表示し、\n"
                        + "右上3Dカメラは全チームを対象に自動観戦します。";

                    break;
                }
        }

        EditorGUILayout.HelpBox(
            description,
            MessageType.None);
    }

    private static void RefreshBattleSpectatorViews()
    {
        BattleMazeMapView[] mazeMapViews =
            FindObjectsByType<BattleMazeMapView>(
                FindObjectsSortMode.None);

        for (int i = 0;
             i < mazeMapViews.Length;
             i++)
        {
            BattleMazeMapView mazeMapView =
                mazeMapViews[i];

            if (mazeMapView == null)
            {
                continue;
            }

            mazeMapView
                .RefreshFogVisibilityByDeveloperSetting();
        }

        BattlePrototypeGameController[] gameControllers =
            FindObjectsByType<BattlePrototypeGameController>(
                FindObjectsSortMode.None);

        for (int i = 0;
             i < gameControllers.Length;
             i++)
        {
            BattlePrototypeGameController gameController =
                gameControllers[i];

            if (gameController == null)
            {
                continue;
            }

            gameController
                .RefreshSpectatorFocusByDeveloperSetting();
        }

        SceneView.RepaintAll();
    }
}

#endif