using UnityEngine;
using UnityEngine.InputSystem;

public partial class BattlePrototypeGameController : MonoBehaviour
{
    // -1 は、まだデバッグ表示チームを選択していない状態。
    private int m_debugSelectedPartyIndex = -1;

    public KnownMapData GetDebugKnownMapData()
    {
        if (m_debugSelectedPartyIndex < 0 ||
            m_debugSelectedPartyIndex >= m_spawnedParties.Count ||
            m_spawnedParties[m_debugSelectedPartyIndex] == null)
        {
            return null;
        }

        return m_spawnedParties[
            m_debugSelectedPartyIndex].GetKnownMapData();
    }

    public Vector2Int[] GetDebugCurrentCells()
    {
        if (m_debugSelectedPartyIndex < 0 ||
            m_debugSelectedPartyIndex >= m_spawnedParties.Count ||
            m_spawnedParties[m_debugSelectedPartyIndex] == null)
        {
            return new Vector2Int[0];
        }

        return m_spawnedParties[
            m_debugSelectedPartyIndex].GetMemberCurrentCells();
    }

    private void HandleDebugKnownMapSelection(
        Keyboard keyboard)
    {
        if (keyboard == null)
        {
            return;
        }

        // Num0 ～ Num3 を Team0 ～ Team3 に対応させる。
        // 通常の数字キー digit0Key ～ digit3Key は使用しない。
        if (keyboard.numpad0Key.wasPressedThisFrame)
        {
            SetDebugSelectedPartyIndex(0);
            return;
        }

        if (keyboard.numpad1Key.wasPressedThisFrame)
        {
            SetDebugSelectedPartyIndex(1);
            return;
        }

        if (keyboard.numpad2Key.wasPressedThisFrame)
        {
            SetDebugSelectedPartyIndex(2);
            return;
        }

        if (keyboard.numpad3Key.wasPressedThisFrame)
        {
            SetDebugSelectedPartyIndex(3);
        }
    }

    private void SetDebugSelectedPartyIndex(
        int partyIndex)
    {
        if (partyIndex < 0 ||
            partyIndex >= m_spawnedParties.Count ||
            m_spawnedParties[partyIndex] == null)
        {
            return;
        }

        if (m_debugSelectedPartyIndex == partyIndex)
        {
            // 同じチームを再選択した場合でも、
            // デバッグ表示データを確実に再バインド・再描画する。
            if (m_knownMapDebugTextView != null)
            {
                string className = m_spawnedParties[partyIndex].GetType().Name;

                m_knownMapDebugTextView.BindKnownMapData(
                    m_spawnedParties[partyIndex].GetKnownMapData(), className);

                m_knownMapDebugTextView.RefreshView();
            }

            return;
        }

        m_debugSelectedPartyIndex = partyIndex;

        if (m_knownMapDebugTextView != null)
        {
            string className = m_spawnedParties[partyIndex].GetType().Name;

            m_knownMapDebugTextView.BindKnownMapData(
                m_spawnedParties[m_debugSelectedPartyIndex].GetKnownMapData(), className);

            m_knownMapDebugTextView.RefreshView();
        }

        Debug.Log(
            $"[BattlePrototypeGameController] " +
            $"KnownMap debug view changed to Team " +
            $"{m_debugSelectedPartyIndex}.");
    }

    public int GetDebugSelectedPartyIndex()
    {
        return m_debugSelectedPartyIndex;
    }
}