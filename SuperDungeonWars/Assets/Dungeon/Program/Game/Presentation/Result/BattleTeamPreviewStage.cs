using UnityEngine;

public sealed class BattleTeamPreviewStage : MonoBehaviour
{
    [SerializeField]
    private Camera m_previewCamera;

    [SerializeField]
    private Transform[] m_memberAnchors;

    public Camera PreviewCamera
    {
        get { return m_previewCamera; }
    }

    public Transform GetMemberAnchor(int memberIndex)
    {
        if (m_memberAnchors == null
            || memberIndex < 0
            || memberIndex >= m_memberAnchors.Length)
        {
            return null;
        }

        return m_memberAnchors[memberIndex];
    }
}