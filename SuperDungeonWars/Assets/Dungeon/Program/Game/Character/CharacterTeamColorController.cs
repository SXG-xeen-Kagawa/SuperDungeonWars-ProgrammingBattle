using UnityEngine;

public class CharacterTeamColorController : MonoBehaviour
{
    private static readonly int s_baseColorPropertyId =
        Shader.PropertyToID("_BaseColor");

    private static readonly int s_colorPropertyId =
        Shader.PropertyToID("_Color");

    private SkinnedMeshRenderer m_skinnedMeshRenderer;
    private MaterialPropertyBlock m_materialPropertyBlock;

    private void Awake()
    {
        EnsureComponents();
    }

    // BattlePrototypeGameControllerだけが呼ぶ表示システム用API。
    // ComCharacterBaseやComPartyBaseはチーム番号を保持しない。
    public void Initialize(int teamIndex, int memberIndex)
    {
        if (EnsureComponents() == false)
        {
            return;
        }

        Color characterColor =
            CharacterTeamColorConstants.GetCharacterColor(
                teamIndex,
                memberIndex);

        m_skinnedMeshRenderer.GetPropertyBlock(
            m_materialPropertyBlock);

        // URP/Lit用。
        m_materialPropertyBlock.SetColor(
            s_baseColorPropertyId,
            characterColor);

        // 将来、別Shaderへ差し替えた際の互換用。
        m_materialPropertyBlock.SetColor(
            s_colorPropertyId,
            characterColor);

        m_skinnedMeshRenderer.SetPropertyBlock(
            m_materialPropertyBlock);
    }

    private bool EnsureComponents()
    {
        if (m_skinnedMeshRenderer == null)
        {
            m_skinnedMeshRenderer =
                GetComponent<SkinnedMeshRenderer>();
        }

        if (m_skinnedMeshRenderer == null)
        {
            Debug.LogError(
                "CharacterTeamColorController と同じGameObjectに "
                + "SkinnedMeshRenderer がありません。",
                this);

            return false;
        }

        if (m_materialPropertyBlock == null)
        {
            m_materialPropertyBlock =
                new MaterialPropertyBlock();
        }

        return true;
    }
}