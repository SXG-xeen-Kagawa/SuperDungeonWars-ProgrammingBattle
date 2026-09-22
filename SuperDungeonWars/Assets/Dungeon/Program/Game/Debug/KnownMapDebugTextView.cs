using System.Text;
using TMPro;
using UnityEngine;

public class KnownMapDebugTextView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_textMeshProUGUI;

    private BattlePrototypeGameController m_battleGameController;
    private KnownMapData m_knownMapData;
    private StringBuilder m_stringBuilder = new StringBuilder();
    private string m_teamName;


    private void OnDestroy()
    {
        if (m_knownMapData != null)
        {
            m_knownMapData.m_onKnownMapUpdated -= OnKnownMapUpdated;
        }
    }


    public void BindBattleGameController(BattlePrototypeGameController battleGameController)
    {
        m_battleGameController = battleGameController;
    }

    public void BindKnownMapData(KnownMapData knownMapData, string teamName)
    {
        if (m_knownMapData != null)
        {
            m_knownMapData.m_onKnownMapUpdated -= OnKnownMapUpdated;
        }

        m_knownMapData = knownMapData;
        m_teamName = teamName;

        if (m_knownMapData != null)
        {
            m_knownMapData.m_onKnownMapUpdated += OnKnownMapUpdated;
        }
    }

    public void RefreshView()
    {
        RefreshText();
    }

    private void OnKnownMapUpdated()
    {
        RefreshText();
    }

    private void RefreshText()
    {
        if (m_textMeshProUGUI == null)
        {
            return;
        }

        if (m_knownMapData == null)
        {
            m_textMeshProUGUI.text =
                "[Known Map Debug]\n" +
                "チーム情報なし\n" +
                "Num1～Num4：チーム切替";

            return;
        }

        Vector2Int singleCurrentCell = Vector2Int.zero;
        bool hasSingleCurrentCell = false;
        Vector2Int[] partyCurrentCells = null;
        int selectedPartyIndex = -1;

        if (m_battleGameController != null)
        {
            partyCurrentCells =
                m_battleGameController.GetDebugCurrentCells();

            selectedPartyIndex =
                m_battleGameController.GetDebugSelectedPartyIndex();
        }

        m_stringBuilder.Clear();

        m_stringBuilder.Append("[Known Map Debug] ");

        if (selectedPartyIndex >= 0)
        {
            m_stringBuilder.Append("Team ");
            m_stringBuilder.Append(selectedPartyIndex);
            m_stringBuilder.AppendFormat("({0})", m_teamName);
        }
        else
        {
            m_stringBuilder.Append("Team ?");
        }

        m_stringBuilder.AppendLine();
        m_stringBuilder.AppendLine("Num1～Num4：チーム切替");
        m_stringBuilder.AppendLine(
            "０～３：選択チームの Member 位置　" +
            "Ｇ：ゴール　Ｒ：部屋　・：通路　■：未観測／壁");

        for (int y = m_knownMapData.m_height - 1; y >= 0; y--)
        {
            for (int x = 0; x < m_knownMapData.m_width; x++)
            {
                Vector2Int cellPos = new Vector2Int(x, y);

                char cellChar = GetCellChar(
                    cellPos,
                    hasSingleCurrentCell,
                    singleCurrentCell,
                    partyCurrentCells);

                m_stringBuilder.Append(cellChar);
            }

            m_stringBuilder.AppendLine();
        }

        m_textMeshProUGUI.text = m_stringBuilder.ToString();
    }


    private readonly char[] m_partyName = new[] { '０', '１', '２', '３', '４', '５', '６', '７', '８', '９' };

    private char GetCellChar(
        Vector2Int cellPos,
        bool hasSingleCurrentCell,
        Vector2Int singleCurrentCell,
        Vector2Int[] partyCurrentCells)
    {
        if (partyCurrentCells != null)
        {
            for (int i = 0; i < partyCurrentCells.Length; i++)
            {
                if (partyCurrentCells[i] == cellPos)
                {
                    return m_partyName[(i % 10)];
                }
            }
        }

        if (hasSingleCurrentCell && singleCurrentCell == cellPos)
        {
            return '＠';
        }

        KnownCellData cell = m_knownMapData.GetCell(cellPos.x, cellPos.y);
        if (cell == null || cell.m_isObserved == false)
        {
            return '■';
        }

        if (cell.m_isCentralHall)
        {
            return 'Ｃ';
        }

        KnownCellViewType viewType = cell.GetViewType();
        if (viewType == KnownCellViewType.Blocked)
        {
            return '■';
        }

        if (viewType == KnownCellViewType.Room)
        {
            return 'Ｒ';
        }

        return '・';
    }
}