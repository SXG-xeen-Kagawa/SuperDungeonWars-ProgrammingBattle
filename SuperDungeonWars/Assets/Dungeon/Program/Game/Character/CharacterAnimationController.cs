using UnityEngine;

public class CharacterAnimationController : MonoBehaviour
{
    private static readonly int s_runSpeedParameterHash =
        Animator.StringToHash("RunSpeed");

    private static readonly int s_isCarryingParameterHash =
        Animator.StringToHash("IsCarrying");

    private static readonly int s_punchTriggerHash =
        Animator.StringToHash("Punch");

    private static readonly int s_lowKickTriggerHash =
        Animator.StringToHash("LowKick");

    private static readonly int s_isKnockedOutHash =
        Animator.StringToHash("IsKnockedOut");

    private static readonly int s_deliveryThrowTriggerHash =
        Animator.StringToHash("DeliveryThrow");

    [Header("Result")]

    [SerializeField]
    private string m_resultIdleStateName =
        "Base Layer.Result.Result Idle";

    [SerializeField]
    private string m_resultVictoryStateName =
        "Base Layer.Result.Result Victory";

    [SerializeField]
    private string m_resultDefeatStateName =
        "Base Layer.Result.Result Defeat";

    [SerializeField]
    private string m_resultTreasureReactStateName =
        "Base Layer.Result.Result Treasure React";

    [Header("Team Introduction")]

    [SerializeField]
    private string m_introductionIdleStateNamePrefix =
        "Base Layer.Intro.Idle";

    [SerializeField]
    [Min(1)]
    private int m_introductionIdleStateCount = 10;

    [SerializeField]
    private string m_normalLocomotionStateName =
        "Base Layer.Normal Locomotion";

    public delegate void DeliveryThrowReleaseHandler(
        ComCharacterBase character);

    public DeliveryThrowReleaseHandler m_onDeliveryThrowRelease;

    private Animator m_animator;
    private ComCharacterBase m_ownerCharacter;

    private Transform m_leftHand;
    private Transform m_rightHand;
    private Transform m_treasureCarryAnchor;

    private bool m_isResultPresentation;

    public Transform TreasureCarryAnchor
    {
        get
        {
            if (m_leftHand == null
                || m_rightHand == null
                || m_treasureCarryAnchor == null)
            {
                return null;
            }

            return m_treasureCarryAnchor;
        }
    }

    internal void BindCharacter(
        ComCharacterBase character)
    {
        if (character == null)
        {
            Debug.LogError(
                "CharacterAnimationController.BindCharacter: "
                + "character が null です。",
                this);

            return;
        }

        m_ownerCharacter = character;
    }

    private void Awake()
    {
        m_animator = GetComponent<Animator>();

        if (m_animator == null)
        {
            Debug.LogError(
                "CharacterAnimationController と同じGameObjectに Animator がありません。",
                this);

            return;
        }

        CreateTreasureCarryAnchor();
        CacheHumanoidHandBones();
    }

    private void Update()
    {
        if (m_animator == null || m_ownerCharacter == null)
        {
            return;
        }

        if (m_isResultPresentation)
        {
            m_animator.SetFloat(
                s_runSpeedParameterHash,
                0.0f);

            m_animator.SetBool(
                s_isCarryingParameterHash,
                false);

            m_animator.SetBool(
                s_isKnockedOutHash,
                false);

            return;
        }

        m_animator.SetFloat(
            s_runSpeedParameterHash,
            m_ownerCharacter.IsMoving() ? 1.0f : 0.0f);

        m_animator.SetBool(
            s_isCarryingParameterHash,
            m_ownerCharacter.HasTreasure());
    }

    private void LateUpdate()
    {
        UpdateTreasureCarryAnchor();
    }

    public void PlayPunch()
    {
        if (m_animator != null)
        {
            m_animator.SetTrigger(s_punchTriggerHash);
        }
    }

    public void PlayLowKick()
    {
        if (m_animator != null)
        {
            m_animator.SetTrigger(s_lowKickTriggerHash);
        }
    }

    public void PlayDeliveryThrow()
    {
        if (m_animator != null)
        {
            m_animator.SetTrigger(
                s_deliveryThrowTriggerHash);
        }
    }

    public void CancelDeliveryThrow()
    {
        if (m_animator != null)
        {
            m_animator.ResetTrigger(
                s_deliveryThrowTriggerHash);
        }
    }

    public void SetKnockedOut(bool isKnockedOut)
    {
        if (m_animator == null)
        {
            return;
        }

        m_animator.SetBool(
            s_isKnockedOutHash,
            isKnockedOut);
    }

    public void BeginResultPresentation()
    {
        m_isResultPresentation = true;

        if (m_animator == null)
        {
            return;
        }

        m_animator.SetFloat(
            s_runSpeedParameterHash,
            0.0f);

        m_animator.SetBool(
            s_isCarryingParameterHash,
            false);

        m_animator.SetBool(
            s_isKnockedOutHash,
            false);

        m_animator.ResetTrigger(s_punchTriggerHash);
        m_animator.ResetTrigger(s_lowKickTriggerHash);
        m_animator.ResetTrigger(s_deliveryThrowTriggerHash);
    }

    public void PlayResultIdle()
    {
        PlayResultState(m_resultIdleStateName);
    }

    public void PlayResultVictory()
    {
        PlayResultState(m_resultVictoryStateName);
    }

    public void PlayResultDefeat()
    {
        PlayResultState(m_resultDefeatStateName);
    }

    public void PlayResultTreasureReact()
    {
        PlayResultState(
            m_resultTreasureReactStateName);
    }

    public void EndResultPresentation()
    {
        m_isResultPresentation = false;
    }

    public void BeginTeamPreviewPresentation()
    {
        BeginResultPresentation();
    }

    public void PlayTeamPreviewIdle()
    {
        PlayResultIdle();
    }

    public void PlayTeamPreviewIntroductionRandomIdle()
    {
        if (m_animator == null)
        {
            return;
        }

        int idleStateCount =
            Mathf.Max(
                1,
                m_introductionIdleStateCount);

        int idleStateIndex =
            Random.Range(
                0,
                idleStateCount);

        string stateName =
            m_introductionIdleStateNamePrefix
            + idleStateIndex.ToString("00");

        int stateHash =
            Animator.StringToHash(
                stateName);

        if (!m_animator.HasState(
                0,
                stateHash))
        {
            Debug.LogWarning(
                "CharacterAnimationController: "
                + "紹介用Idleステートが見つかりません。 state="
                + stateName,
                this);

            return;
        }

        float normalizedTime =
            Random.value;

        m_animator.Play(
            stateHash,
            0,
            normalizedTime);
    }

    public void PlayTeamPreviewVictory()
    {
        PlayResultVictory();
    }

    public void PlayTeamPreviewDefeat()
    {
        PlayResultDefeat();
    }

    public void PlayTeamPreviewTreasureReact()
    {
        PlayResultTreasureReact();
    }

    public void EndTeamPreviewPresentation()
    {
        EndResultPresentation();

        PlayNormalLocomotionState();
    }

    private void PlayResultState(string stateName)
    {
        if (m_animator == null
            || string.IsNullOrEmpty(stateName))
        {
            return;
        }

        int stateHash = Animator.StringToHash(stateName);

        if (!m_animator.HasState(0, stateHash))
        {
            Debug.LogWarning(
                "CharacterAnimationController: "
                + "Result用ステートが見つかりません。 state="
                + stateName,
                this);

            return;
        }

        m_animator.Play(
            stateHash,
            0,
            0.0f);
    }

    public void OnDeliveryThrowRelease()
    {
        UpdateTreasureCarryAnchor();

        if (m_ownerCharacter == null)
        {
            return;
        }

        if (m_onDeliveryThrowRelease != null)
        {
            m_onDeliveryThrowRelease(
                m_ownerCharacter);
        }
    }

    private void CreateTreasureCarryAnchor()
    {
        GameObject anchorObject =
            new GameObject("TreasureCarryAnchor");

        m_treasureCarryAnchor =
            anchorObject.transform;

        m_treasureCarryAnchor.SetParent(
            transform,
            false);

        m_treasureCarryAnchor.localPosition =
            Vector3.zero;

        m_treasureCarryAnchor.localRotation =
            Quaternion.identity;

        m_treasureCarryAnchor.localScale =
            Vector3.one;
    }

    private void CacheHumanoidHandBones()
    {
        if (m_animator == null || !m_animator.isHuman)
        {
            Debug.LogWarning(
                "CharacterAnimationController: Humanoid Animator ではないため、"
                + "宝箱を手ボーンへ追従させられません。",
                this);

            return;
        }

        m_leftHand = m_animator.GetBoneTransform(
            HumanBodyBones.LeftHand);

        m_rightHand = m_animator.GetBoneTransform(
            HumanBodyBones.RightHand);

        if (m_leftHand == null || m_rightHand == null)
        {
            Debug.LogWarning(
                "CharacterAnimationController: LeftHand または RightHand を取得できません。",
                this);
        }
    }

    private void UpdateTreasureCarryAnchor()
    {
        if (m_leftHand == null
            || m_rightHand == null
            || m_treasureCarryAnchor == null)
        {
            return;
        }

        Vector3 leftHandPosition =
            m_leftHand.position;

        Vector3 rightHandPosition =
            m_rightHand.position;

        Vector3 handCenterPosition =
            (leftHandPosition + rightHandPosition) * 0.5f;

        Vector3 rightDirection =
            rightHandPosition - leftHandPosition;

        if (rightDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        rightDirection.Normalize();

        Vector3 upDirection = transform.up;

        Vector3 forwardDirection =
            Vector3.Cross(
                upDirection,
                rightDirection);

        if (Vector3.Dot(
                forwardDirection,
                transform.forward) < 0.0f)
        {
            forwardDirection = -forwardDirection;
        }

        if (forwardDirection.sqrMagnitude <= 0.0001f)
        {
            forwardDirection = transform.forward;
        }

        m_treasureCarryAnchor.position =
            handCenterPosition;

        m_treasureCarryAnchor.rotation =
            Quaternion.LookRotation(
                forwardDirection.normalized,
                upDirection);
    }

    private void PlayNormalLocomotionState()
    {
        if (m_animator == null
            || string.IsNullOrEmpty(
                m_normalLocomotionStateName))
        {
            return;
        }

        int stateHash =
            Animator.StringToHash(
                m_normalLocomotionStateName);

        if (!m_animator.HasState(
                0,
                stateHash))
        {
            Debug.LogWarning(
                "CharacterAnimationController: "
                + "通常移動用ステートが見つかりません。 state="
                + m_normalLocomotionStateName,
                this);

            return;
        }

        m_animator.Play(
            stateHash,
            0,
            0.0f);
    }
}