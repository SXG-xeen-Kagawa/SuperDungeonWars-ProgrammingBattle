using UnityEngine;

public static class CharacterTeamColorConstants
{
    /*
    private static readonly Color[] s_teamBaseColors =
    {
        // オレンジ：視認性の高い暖色。
        new Color(1.00f, 0.34f, 0.05f, 1.00f),

        // エメラルドグリーン：現行より少し落ち着かせる。
        new Color(0.05f, 0.78f, 0.30f, 1.00f),

        // コーラルレッド：紫・マゼンタへ寄せない赤系。
        new Color(1.00f, 0.16f, 0.18f, 1.00f),

        // スカイブルー：濃すぎず、背景から分離しやすい青。
        new Color(0.05f, 0.48f, 1.00f, 1.00f),
    };
    */
    private static readonly Color[] s_teamBaseColors =
    {
        // チーム1：オレンジ
        new Color(1.00f, 0.34f, 0.05f, 1.00f),

        // チーム2：エメラルドグリーン
        new Color(0.05f, 0.78f, 0.30f, 1.00f),

        // チーム3：ゴールドイエロー
        // オレンジ・赤とは明確に異なる明るい黄系。
        new Color(1.00f, 0.82f, 0.08f, 1.00f),

        // チーム4：スカイブルー
        new Color(0.05f, 0.48f, 1.00f, 1.00f),
    };

    // 同チーム4人の識別用。大きく変えすぎず、同一チーム感を残す。
    private static readonly float[] s_memberBrightnessMultipliers =
    {
        1.05f,
        1.00f,
        0.93f,
        0.86f,
    };

    public static Color GetCharacterColor(int teamIndex, int memberIndex)
    {
        int normalizedTeamIndex =
            Mathf.Abs(teamIndex) % s_teamBaseColors.Length;

        int normalizedMemberIndex =
            Mathf.Abs(memberIndex) % s_memberBrightnessMultipliers.Length;

        Color baseColor = s_teamBaseColors[normalizedTeamIndex];
        float brightnessMultiplier =
            s_memberBrightnessMultipliers[normalizedMemberIndex];

        return new Color(
            Mathf.Clamp01(baseColor.r * brightnessMultiplier),
            Mathf.Clamp01(baseColor.g * brightnessMultiplier),
            Mathf.Clamp01(baseColor.b * brightnessMultiplier),
            1.0f);
    }


    public static Color GetTeamBaseColor(
        int systemTeamIndex)
    {
        if (systemTeamIndex < 0
            || systemTeamIndex >= s_teamBaseColors.Length)
        {
            return Color.white;
        }

        return s_teamBaseColors[systemTeamIndex];
    }

}