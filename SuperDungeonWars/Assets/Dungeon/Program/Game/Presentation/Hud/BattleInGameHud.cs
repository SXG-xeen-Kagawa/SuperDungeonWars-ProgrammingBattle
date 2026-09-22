using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BattleInGameHud : MonoBehaviour
{
    [SerializeField]
    private BattlePrototypeGameController m_battleGameController;

    [SerializeField] private TextMeshProUGUI m_remainingTimeText;

    [SerializeField]
    private Transform m_teamCardsRoot;

    [SerializeField]
    private BattleInGameHudTeamCard m_teamCardPrefab;


    [Header("Remaining Time Color Effect")]
    [SerializeField]
    private Color m_normalRemainingTimeColor =
    new Color(1.00f, 1.00f, 1.00f, 1.00f);

    [SerializeField]
    private Color m_cautionRemainingTimeColor =
        new Color(1.00f, 0.84f, 0.16f, 1.00f);

    [SerializeField]
    private Color m_warningRemainingTimeColor =
        new Color(1.00f, 0.48f, 0.10f, 1.00f);

    [SerializeField]
    private Color m_criticalRemainingTimeColor =
        new Color(1.00f, 0.16f, 0.12f, 1.00f);

    [SerializeField]
    private Color m_timeUpFlashColor =
        new Color(1.00f, 0.92f, 0.92f, 1.00f);

    [SerializeField, Range(1.0f, 10.0f)]
    private float m_criticalFlashSpeed = 5.0f;

    [SerializeField, Range(1.0f, 10.0f)]
    private float m_timeUpFlashSpeed = 7.0f;

    [SerializeField, Range(0.5f, 10.0f)]
    private float m_timeUpFlashDuration = 5.0f;


    [SerializeField]
    private TextMeshProUGUI m_centralChestStatusText;

    [SerializeField]
    private Color m_centralChestStatusStarColor =
        new Color(0.93f, 0.70f, 0.24f, 1.00f); // #EDB33D

    [SerializeField]
    private Color m_centralChestStatusBaseColor =
        new Color(0.84f, 0.88f, 0.94f, 1.00f); // #D6E0F0

    [SerializeField]
    private Color m_centralChestStatusUnsecuredColor =
        new Color(0.74f, 0.78f, 0.85f, 1.00f); // #BDC7D9

    private enum CentralChestStatusKind
    {
        None,
        Unsecured,
        Carrying,
        Exported,
    }

    private CentralChestStatusKind m_displayedCentralChestStatus =
        CentralChestStatusKind.None;

    private TreasureChest m_displayedCentralChest;

    private int m_displayedCentralChestPartyIndex = -1;



    private float m_timeUpStartedUnscaledTime = -1.0f;



    private readonly List<BattleInGameHudTeamCard> m_teamCards =
        new List<BattleInGameHudTeamCard>();

    private bool m_isPresentationVisible = true;



    private void Awake()
    {
        if (m_battleGameController == null)
        {
            m_battleGameController =
                FindFirstObjectByType<
                    BattlePrototypeGameController>();
        }
    }

    public void SetPresentationVisible(
        bool isVisible)
    {
        m_isPresentationVisible = isVisible;

        if (m_teamCardsRoot != null)
        {
            m_teamCardsRoot.gameObject.SetActive(isVisible);
        }

        if (m_remainingTimeText != null)
        {
            m_remainingTimeText.gameObject.SetActive(isVisible);
        }

        if (m_centralChestStatusText != null)
        {
            m_centralChestStatusText.gameObject.SetActive(isVisible);
        }
    }



    private void Update()
    {
        RefreshView();
    }

    private void OnDestroy()
    {
        ClearTeamCards();
    }

    private void RefreshView()
    {
        if (!m_isPresentationVisible)
        {
            return;
        }

        if (m_battleGameController == null)
        {
            return;
        }

        RefreshRemainingTime();
        RefreshCentralChestStatus();
        RefreshTeamCards();
    }

    private void RefreshRemainingTime()
    {
        if (m_remainingTimeText == null)
        {
            return;
        }

        float remainingSeconds = Mathf.Max(
            0.0f,
            m_battleGameController.RemainingBattleSeconds);

        int totalSeconds =
            Mathf.CeilToInt(remainingSeconds);

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        m_remainingTimeText.text =
            minutes.ToString("00")
            + ":"
            + seconds.ToString("00");

        RefreshRemainingTimeColor(remainingSeconds);
    }

    private void RefreshRemainingTimeColor(
        float remainingSeconds)
    {
        if (m_remainingTimeText == null)
        {
            return;
        }

        if (remainingSeconds <= 0.0f)
        {
            if (m_timeUpStartedUnscaledTime < 0.0f)
            {
                m_timeUpStartedUnscaledTime =
                    Time.unscaledTime;
            }

            float elapsedSeconds =
                Time.unscaledTime
                - m_timeUpStartedUnscaledTime;

            if (elapsedSeconds
                < m_timeUpFlashDuration)
            {
                float flashRate = GetFlashRate(
                    m_timeUpFlashSpeed);

                m_remainingTimeText.color =
                    Color.Lerp(
                        m_criticalRemainingTimeColor,
                        m_timeUpFlashColor,
                        flashRate);
            }
            else
            {
                m_remainingTimeText.color =
                    m_criticalRemainingTimeColor;
            }

            return;
        }

        // 次の試合開始時には、タイムアップ演出の計測をリセットする。
        m_timeUpStartedUnscaledTime = -1.0f;

        if (remainingSeconds <= 10.0f)
        {
            float flashRate = GetFlashRate(
                m_criticalFlashSpeed);

            m_remainingTimeText.color =
                Color.Lerp(
                    m_criticalRemainingTimeColor,
                    m_warningRemainingTimeColor,
                    flashRate);

            return;
        }

        if (remainingSeconds <= 30.0f)
        {
            m_remainingTimeText.color =
                m_warningRemainingTimeColor;

            return;
        }

        if (remainingSeconds <= 60.0f)
        {
            m_remainingTimeText.color =
                m_cautionRemainingTimeColor;

            return;
        }

        m_remainingTimeText.color =
            m_normalRemainingTimeColor;
    }

    private float GetFlashRate(float flashSpeed)
    {
        float wave =
            Mathf.PingPong(
                Time.unscaledTime * flashSpeed,
                1.0f);

        return wave * wave * (3.0f - 2.0f * wave);
    }



    private void RefreshTeamCards()
    {
        int partyCount =
            m_battleGameController.SpawnedPartyCount;

        EnsureTeamCardCount(partyCount);

        for (int teamIndex = 0;
             teamIndex < m_teamCards.Count;
             teamIndex++)
        {
            BattleInGameHudTeamCard teamCard =
                m_teamCards[teamIndex];

            if (teamCard == null)
            {
                continue;
            }

            ComPartyBase party =
                m_battleGameController
                    .SpawnedParties[teamIndex];

            teamCard.Refresh(party, teamIndex);
        }
    }

    private void EnsureTeamCardCount(int requiredCount)
    {
        if (requiredCount < 0)
        {
            requiredCount = 0;
        }

        while (m_teamCards.Count > requiredCount)
        {
            int lastIndex = m_teamCards.Count - 1;

            BattleInGameHudTeamCard teamCard =
                m_teamCards[lastIndex];

            m_teamCards.RemoveAt(lastIndex);

            if (teamCard != null)
            {
                Destroy(teamCard.gameObject);
            }
        }

        while (m_teamCards.Count < requiredCount)
        {
            BattleInGameHudTeamCard teamCard =
                CreateTeamCard();

            if (teamCard == null)
            {
                break;
            }

            m_teamCards.Add(teamCard);
        }
    }

    private BattleInGameHudTeamCard CreateTeamCard()
    {
        if (m_teamCardsRoot == null)
        {
            Debug.LogError(
                "BattleInGameHud: "
                + "m_teamCardsRoot が未設定です。",
                this);

            return null;
        }

        if (m_teamCardPrefab == null)
        {
            Debug.LogError(
                "BattleInGameHud: "
                + "m_teamCardPrefab が未設定です。",
                this);

            return null;
        }

        BattleInGameHudTeamCard teamCard = Instantiate(
            m_teamCardPrefab,
            m_teamCardsRoot);

        teamCard.gameObject.SetActive(true);

        return teamCard;
    }

    private void ClearTeamCards()
    {
        for (int i = 0; i < m_teamCards.Count; i++)
        {
            BattleInGameHudTeamCard teamCard =
                m_teamCards[i];

            if (teamCard != null)
            {
                Destroy(teamCard.gameObject);
            }
        }

        m_teamCards.Clear();
    }

    private void RefreshCentralChestStatus()
    {
        if (m_centralChestStatusText == null)
        {
            return;
        }

        TreasureChest centralChest = FindCentralChest();

        CentralChestStatusKind statusKind =
            CentralChestStatusKind.Unsecured;

        int partyIndex = -1;

        if (centralChest == null)
        {
            statusKind = CentralChestStatusKind.None;
        }
        else if (centralChest.IsExported)
        {
            statusKind = CentralChestStatusKind.Exported;

            partyIndex = FindExportedTreasurePartyIndex(
                centralChest);
        }
        else if (centralChest.IsPickedUp
            && centralChest.OwnerCharacter != null)
        {
            statusKind = CentralChestStatusKind.Carrying;

            partyIndex = FindOwnerPartyIndex(
                centralChest.OwnerCharacter);
        }

        bool isSameDisplay =
            m_displayedCentralChest == centralChest
            && m_displayedCentralChestStatus == statusKind
            && m_displayedCentralChestPartyIndex == partyIndex;

        if (isSameDisplay)
        {
            return;
        }

        m_displayedCentralChest = centralChest;
        m_displayedCentralChestStatus = statusKind;
        m_displayedCentralChestPartyIndex = partyIndex;

        ApplyCentralChestStatusText(
            statusKind,
            partyIndex);
    }

    private void ApplyCentralChestStatusText(
        CentralChestStatusKind statusKind,
        int partyIndex)
    {
        if (m_centralChestStatusText == null)
        {
            return;
        }

        string starColorCode =
            ColorUtility.ToHtmlStringRGB(
                m_centralChestStatusStarColor);

        string baseColorCode =
            ColorUtility.ToHtmlStringRGB(
                m_centralChestStatusBaseColor);

        string unsecuredColorCode =
            ColorUtility.ToHtmlStringRGB(
                m_centralChestStatusUnsecuredColor);

        if (statusKind == CentralChestStatusKind.None)
        {
            m_centralChestStatusText.text =
                "<color=#"
                + unsecuredColorCode
                + ">★ 中央宝箱：未出現</color>";

            return;
        }

        if (statusKind == CentralChestStatusKind.Unsecured)
        {
            m_centralChestStatusText.text =
                "<color=#"
                + starColorCode
                + ">★</color>"
                + "<color=#"
                + unsecuredColorCode
                + "> 中央宝箱：未確保</color>";

            return;
        }

        ComPartyBase party = GetPartyByIndex(partyIndex);

        string partyName = GetPartyDisplayName(party);
        string partyColorCode = GetPartyColorCode(partyIndex);

        string statusText = statusKind
            == CentralChestStatusKind.Exported
            ? " が搬出済み"
            : " が搬送中";

        m_centralChestStatusText.text =
            "<color=#"
            + starColorCode
            + ">★</color>"
            + "<color=#"
            + baseColorCode
            + "> 中央宝箱：</color>"
            + "<color=#"
            + partyColorCode
            + ">"
            + partyName
            + "</color>"
            + "<color=#"
            + baseColorCode
            + ">"
            + statusText
            + "</color>";
    }


    private TreasureChest FindCentralChest()
    {
        IReadOnlyList<TreasureChest> treasureChests =
            m_battleGameController.SpawnedTreasureChests;

        if (treasureChests == null)
        {
            return null;
        }

        for (int i = 0; i < treasureChests.Count; i++)
        {
            TreasureChest treasureChest =
                treasureChests[i];

            if (treasureChest == null)
            {
                continue;
            }

            if (treasureChest.TreasureType
                == TreasureType.CentralChest)
            {
                return treasureChest;
            }
        }

        return null;
    }


    private int FindOwnerPartyIndex(
        ComCharacterBase ownerCharacter)
    {
        if (ownerCharacter == null)
        {
            return -1;
        }

        IReadOnlyList<ComPartyBase> parties =
            m_battleGameController.SpawnedParties;

        if (parties == null)
        {
            return -1;
        }

        for (int partyIndex = 0;
             partyIndex < parties.Count;
             partyIndex++)
        {
            ComPartyBase party = parties[partyIndex];

            if (party == null)
            {
                continue;
            }

            int memberCount = party.MemberCount;

            for (int memberIndex = 0;
                 memberIndex < memberCount;
                 memberIndex++)
            {
                ComCharacterBase member;

                bool hasMember = party.TryGetMember(memberIndex, out member);

                if (hasMember && member == ownerCharacter)
                {
                    return partyIndex;
                }
            }
        }

        return -1;
    }

    private int FindExportedTreasurePartyIndex(
        TreasureChest treasureChest)
    {
        if (treasureChest == null)
        {
            return -1;
        }

        IReadOnlyList<ComPartyBase> parties =
            m_battleGameController.SpawnedParties;

        if (parties == null)
        {
            return -1;
        }

        for (int partyIndex = 0;
             partyIndex < parties.Count;
             partyIndex++)
        {
            ComPartyBase party = parties[partyIndex];

            if (party == null
                || party.GetExportedTreasureChests() == null)
            {
                continue;
            }

            IReadOnlyList<ITreasureRuntime> exportedTreasures =
                party.GetExportedTreasureChests();

            for (int treasureIndex = 0;
                 treasureIndex < exportedTreasures.Count;
                 treasureIndex++)
            {
                TreasureChest exportedTreasure =
                    exportedTreasures[treasureIndex]
                    as TreasureChest;

                if (exportedTreasure == treasureChest)
                {
                    return partyIndex;
                }
            }
        }

        return -1;
    }


    private ComPartyBase GetPartyByIndex(int partyIndex)
    {
        IReadOnlyList<ComPartyBase> parties =
            m_battleGameController.SpawnedParties;

        if (parties == null
            || partyIndex < 0
            || partyIndex >= parties.Count)
        {
            return null;
        }

        return parties[partyIndex];
    }

    private string GetPartyColorCode(int partyIndex)
    {
        if (partyIndex < 0)
        {
            return "FFFFFF";
        }

        Color teamColor =
            CharacterTeamColorConstants.GetTeamBaseColor(
                partyIndex);

        return ColorUtility.ToHtmlStringRGB(teamColor);
    }

    private string GetPartyDisplayName(
        ComPartyBase party)
    {
        if (party == null)
        {
            return "不明なチーム";
        }

        return party.TeamDisplayName;
    }




}