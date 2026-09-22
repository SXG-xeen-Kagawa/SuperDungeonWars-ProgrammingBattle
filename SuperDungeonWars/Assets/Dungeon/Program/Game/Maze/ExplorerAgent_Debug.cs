using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class ExplorerAgent : MonoBehaviour, ICharacterRuntime
{
    [SerializeField] private bool m_showMoveTargetGizmo = true;
    [SerializeField] private float m_moveTargetGizmoRadius = 0.12f;

    [SerializeField] private bool m_showPartyMemberIndexGizmo = true;

    [SerializeField]
    private bool m_showCharacterStateGizmo = true;

    [SerializeField]
    private bool m_showWorldPositionInGizmo = false;

    [SerializeField]
    private Vector3 m_partyMemberIndexGizmoOffset =
        new Vector3(0.0f, 0.9f, 0.0f);

    private void OnDrawGizmos()
    {
        Vector3 currentPosition = WorldPosition;

        DrawMoveTargetGizmo(currentPosition);
        DrawPartyMemberIndexGizmo(currentPosition);
    }

    private void DrawMoveTargetGizmo(
        Vector3 currentPosition)
    {
        if (m_showMoveTargetGizmo == false)
        {
            return;
        }

        Vector3 gizmoCurrentPosition = currentPosition;
        Vector3 targetPosition = m_targetWorldPosition;

        gizmoCurrentPosition.y += 0.05f;
        targetPosition.y = gizmoCurrentPosition.y;

        if (m_hasMoveTarget)
        {
            Gizmos.color = Color.yellow;

            Gizmos.DrawLine(
                gizmoCurrentPosition,
                targetPosition);

            Gizmos.color = Color.red;

            Gizmos.DrawSphere(
                targetPosition,
                m_moveTargetGizmoRadius);

            Gizmos.color = new Color(
                1.0f,
                0.5f,
                0.0f,
                0.9f);

            Gizmos.DrawWireSphere(
                targetPosition,
                m_moveTargetGizmoRadius * 1.8f);

            return;
        }

        Gizmos.color = new Color(
            0.5f,
            0.5f,
            0.5f,
            0.5f);

        Gizmos.DrawWireSphere(
            gizmoCurrentPosition,
            m_moveTargetGizmoRadius);
    }

    private void DrawPartyMemberIndexGizmo(
        Vector3 currentPosition)
    {
#if UNITY_EDITOR
        if (m_showPartyMemberIndexGizmo == false)
        {
            return;
        }

        ComCharacterBase character = GetCharacter();

        bool isKnockedOut =
            character != null &&
            character.IsKnockedOut();

        bool isActionLocked =
            character != null &&
            character.IsActionLocked();

        Vector3 labelPosition =
            currentPosition +
            m_partyMemberIndexGizmoOffset;

        GUIStyle labelStyle =
            new GUIStyle(EditorStyles.boldLabel);

        labelStyle.fontSize = 14;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.normal.textColor =
            GetCharacterStateGizmoColor(
                isKnockedOut,
                isActionLocked);

        string memberText = m_partyMemberIndex >= 0
            ? $"Member {m_partyMemberIndex}"
            : "Member ?";

        string labelText = memberText;

        if (m_showCharacterStateGizmo)
        {
            labelText +=
                $"\nKO={isKnockedOut}  Lock={isActionLocked}" +
                $"\nPaused={m_isRagdollPaused}  Move={m_hasMoveTarget}" +
                $"\nCell={m_currentCell}";

            if (m_hasMoveTarget)
            {
                if (m_isCellTarget)
                {
                    labelText +=
                        $"\nTargetCell={m_targetCellPosition}";
                }
                else
                {
                    labelText += "\nTarget=World";
                }
            }

            labelText += $"\nID={GetInstanceID()}";
        }

        if (m_showWorldPositionInGizmo)
        {
            labelText +=
                $"\nPos=({currentPosition.x:F1}, " +
                $"{currentPosition.y:F1}, " +
                $"{currentPosition.z:F1})";
        }

        Handles.Label(
            labelPosition,
            labelText,
            labelStyle);
#endif
    }

    private Color GetCharacterStateGizmoColor(
        bool isKnockedOut,
        bool isActionLocked)
    {
        if (m_isRagdollPaused &&
            isKnockedOut == false)
        {
            return new Color(
                1.0f,
                0.25f,
                0.05f,
                1.0f);
        }

        if (isKnockedOut &&
            m_isRagdollPaused)
        {
            return new Color(
                1.0f,
                0.2f,
                1.0f,
                1.0f);
        }

        if (isKnockedOut)
        {
            return Color.red;
        }

        if (isActionLocked)
        {
            return Color.yellow;
        }

        if (m_hasMoveTarget)
        {
            return Color.cyan;
        }

        return new Color(
            0.75f,
            0.75f,
            0.75f,
            1.0f);
    }
}