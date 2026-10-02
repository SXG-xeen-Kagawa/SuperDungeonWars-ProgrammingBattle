using UnityEngine;

/// <summary>
/// ゲーム側から利用するサウンドAPI。
/// 素材がない環境でも、呼び出しを削除せず利用できます。
/// </summary>
public static class DungeonSound
{
    public static bool IsInitialized =>
        DungeonSoundManager.Instance != null &&
        DungeonSoundManager.Instance.IsInitialized;

    public static bool IsAvailable =>
        DungeonSoundManager.Instance != null &&
        DungeonSoundManager.Instance.IsAvailable;

    /// <summary>
    /// カメラ位置に関係なくSEを再生します。
    /// システム音、通知音、リザルト演出音などに使用します。
    /// </summary>
    public static void PlaySe(SeId id, float volume = 1f)
    {
        DungeonSoundManager.Instance?.PlaySe(id, volume);
    }

    /// <summary>
    /// 発生位置が指定カメラの描画範囲内にある場合だけSEを再生します。
    /// 判定は再生要求時のみ行い、再生後のカメラ移動には追従しません。
    /// 再生そのものは2D音声です。
    /// </summary>
    public static void PlaySeIfVisible(
        SeId id,
        Vector3 worldPosition,
        Camera camera = null,
        float volume = 1f)
    {
        DungeonSoundManager.Instance?.PlaySeIfVisible(
            id,
            worldPosition,
            camera,
            volume);
    }

    public static void PlayBgm(
        BgmId id,
        SameBgmBehaviour sameBgmBehaviour = SameBgmBehaviour.KeepPlaying,
        float crossFadeSeconds = -1f)
    {
        DungeonSoundManager.Instance?.PlayBgm(
            id,
            sameBgmBehaviour,
            crossFadeSeconds);
    }

    public static void StopBgm(float fadeSeconds = -1f)
    {
        DungeonSoundManager.Instance?.StopBgm(fadeSeconds);
    }

    public static void StopAllSe()
    {
        DungeonSoundManager.Instance?.StopAllSe();
    }

    public static void SetSeVolume(float volume)
    {
        DungeonSoundManager.Instance?.SetSeVolume(volume);
    }

    public static void SetBgmVolume(float volume)
    {
        DungeonSoundManager.Instance?.SetBgmVolume(volume);
    }
}