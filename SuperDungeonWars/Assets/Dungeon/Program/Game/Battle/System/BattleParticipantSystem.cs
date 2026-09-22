using System.Collections.Generic;
using UnityEngine;

public class BattleParticipantSystem
{
    private readonly Dictionary<ExplorerAgent, ComCharacterBase>
        m_characterByAgent =
            new Dictionary<ExplorerAgent, ComCharacterBase>();

    private readonly Dictionary<ComCharacterBase, ExplorerSensor>
        m_sensorByCharacter =
            new Dictionary<ComCharacterBase, ExplorerSensor>();

    private readonly List<ComPartyBase> m_parties =
        new List<ComPartyBase>();

    private MazeData m_mazeData;
    private BattleTreasureSystem m_battleTreasureSystem;

    private BattleGameRuleData m_battleGameRuleData;

    public void Initialize(
        MazeData mazeData,
        IReadOnlyList<ComPartyBase> parties,
        BattleTreasureSystem battleTreasureSystem,
        BattleGameRuleData battleGameRuleData)
    {
        Shutdown();

        m_mazeData = mazeData;
        m_battleTreasureSystem = battleTreasureSystem;
        m_battleGameRuleData = battleGameRuleData;

        if (m_mazeData == null || parties == null)
        {
            return;
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

            m_parties.Add(party);

            int memberCount = party.MemberCount;

            for (int memberIndex = 0;
                 memberIndex < memberCount;
                 memberIndex++)
            {
                ComCharacterBase member;

                if (!party.TryGetMember(
                    memberIndex,
                    out member))
                {
                    continue;
                }

                RegisterCharacter(party, member);
            }
        }
    }

    public void Shutdown()
    {
        foreach (
            KeyValuePair<ExplorerAgent, ComCharacterBase> pair
            in m_characterByAgent)
        {
            ExplorerAgent agent = pair.Key;

            if (agent != null)
            {
                agent.m_onCellReached -= OnAgentCellReached;
            }
        }

        m_characterByAgent.Clear();
        m_sensorByCharacter.Clear();
        m_parties.Clear();

        m_mazeData = null;
        m_battleTreasureSystem = null;
        m_battleGameRuleData = null;
    }

    public void UpdateSystem()
    {
        // 参加者の思考は先に全員分を実行する。
        // 命令の適用は後段でまとめて行うため、
        // パーティー順による即時実行差を作らない。
        ThinkParticipants();

        // セル到達時だけでなく、搬出可能セル内で宝箱を拾った場合や、
        // 入口到達時に行動ロック中だった場合にも搬出を再試行する。
        // ApplyOrder() より前に実行することで、
        // 搬出開始時の行動ロックを同フレームの命令適用へ反映する。
        TryExportTreasuresAtEntrance();

        // SetOrder() によって蓄積された命令を、
        // システム側で実際の ExplorerAgent へ反映する。
        ApplyParticipantOrders();
    }

    private void RegisterCharacter(
        ComPartyBase party,
        ComCharacterBase character)
    {
        if (party == null || character == null)
        {
            return;
        }

        ExplorerAgent agent = character.GetExplorerAgent() as ExplorerAgent;

        if (agent == null)
        {
            Debug.LogError(
                "BattleParticipantSystem: "
                + "ExplorerAgent がないキャラクターは"
                + "登録できません。",
                character);

            return;
        }

        if (m_characterByAgent.ContainsKey(agent))
        {
            Debug.LogError(
                "BattleParticipantSystem: "
                + "同一 ExplorerAgent が重複登録されています。",
                character);

            return;
        }

        m_characterByAgent.Add(agent, character);

        ExplorerSensor sensor =
            new ExplorerSensor(
                m_mazeData,
                party.GetKnownMapData());

        sensor.ObserveFromCell(character.GetCurrentCell());

        m_sensorByCharacter.Add(character, sensor);

        agent.m_onCellReached += OnAgentCellReached;
    }

    private void ThinkParticipants()
    {
        for (int partyIndex = 0;
             partyIndex < m_parties.Count;
             partyIndex++)
        {
            ComPartyBase party = m_parties[partyIndex];

            if (party == null)
            {
                continue;
            }

            // ComPartyBase.Think() は参加者の判断入口であり、
            // ゲームルール処理は含めない。
            party.Think();

            int memberCount = party.MemberCount;

            for (int memberIndex = 0;
                 memberIndex < memberCount;
                 memberIndex++)
            {
                ComCharacterBase member;

                if (!party.TryGetMember(
                    memberIndex,
                    out member))
                {
                    continue;
                }

                // ComCharacterBase.Think() も参加者の判断入口。
                // ノックアウト・移動停止などの必須処理はここへ置かない。
                member.Think();
            }
        }
    }

    private void TryExportTreasuresAtEntrance()
    {
        if (m_battleTreasureSystem == null)
        {
            return;
        }

        foreach (
            KeyValuePair<ExplorerAgent, ComCharacterBase> pair
            in m_characterByAgent)
        {
            ComCharacterBase character = pair.Value;

            if (character == null || !character.HasTreasure())
            {
                continue;
            }

            // 搬出セルか、行動可能か、搬出予約済みかなどの判定は
            // BattleTreasureSystem 側へ一元化する。
            m_battleTreasureSystem.TryExportTreasure(
                character);
        }
    }

    private void ApplyParticipantOrders()
    {
        foreach (
            KeyValuePair<ExplorerAgent, ComCharacterBase> pair
            in m_characterByAgent)
        {
            ExplorerAgent agent = pair.Key;
            ComCharacterBase character = pair.Value;

            if (agent == null || character == null)
            {
                continue;
            }

            ApplyMoveSpeed(
                agent,
                character);

            // ノックアウト中の停止は、
            // ComCharacterBase の Think() ではなく
            // ApplyOrder() 側で保証される。
            character.ApplyOrder();
        }
    }

    private void OnAgentCellReached(
        ExplorerAgent agent,
        Vector2Int cellPosition)
    {
        if (agent == null)
        {
            return;
        }

        ComCharacterBase character;

        if (!m_characterByAgent.TryGetValue(
            agent,
            out character))
        {
            return;
        }

        ExplorerSensor sensor;

        if (m_sensorByCharacter.TryGetValue(
            character,
            out sensor)
            && sensor != null)
        {
            // 参加者への通知より先に既知マップを更新する。
            // これにより OnCellReached 内で新しい観測結果を読める。
            sensor.ObserveFromCell(cellPosition);
        }

        if (m_battleTreasureSystem != null)
        {
            m_battleTreasureSystem.TryExportTreasure(
                character);
        }

        // 参加者への通知。ここにはゲームルールを置かない。
        character.NotifyCellReached(cellPosition);
    }

    private void ApplyMoveSpeed(
        ExplorerAgent agent,
        ComCharacterBase character)
    {
        if (agent == null
            || character == null
            || m_battleGameRuleData == null)
        {
            return;
        }

        float moveSpeed = character.HasTreasure()
            ? m_battleGameRuleData.CarryingTreasureMoveSpeed
            : m_battleGameRuleData.NormalMoveSpeed;

        agent.SetMoveSpeed(moveSpeed);
    }
}