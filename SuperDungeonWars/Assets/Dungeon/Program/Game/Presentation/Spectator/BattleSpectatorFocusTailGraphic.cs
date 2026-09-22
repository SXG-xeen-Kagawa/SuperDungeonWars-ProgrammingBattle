using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 観戦枠とミニマップ上の注目位置を結ぶ三角形を描画するUI Graphic。
/// </summary>
public sealed class BattleSpectatorFocusTailGraphic
    : MaskableGraphic
{
    private Vector2 m_baseTopPosition;
    private Vector2 m_baseBottomPosition;
    private Vector2 m_tipPosition;
    private bool m_hasTriangle;

    protected override void Awake()
    {
        base.Awake();

        raycastTarget = false;
    }

    /// <summary>
    /// 三角形の3頂点と色を設定する。
    /// 各座標は、このGraphicのRectTransformローカル座標。
    /// </summary>
    public void SetTriangle(
        Vector2 baseTopPosition,
        Vector2 baseBottomPosition,
        Vector2 tipPosition,
        Color triangleColor)
    {
        const float positionChangeThreshold = 0.01f;

        bool hasPositionChanged =
            (m_baseTopPosition - baseTopPosition).sqrMagnitude
                > positionChangeThreshold
            || (m_baseBottomPosition - baseBottomPosition).sqrMagnitude
                > positionChangeThreshold
            || (m_tipPosition - tipPosition).sqrMagnitude
                > positionChangeThreshold;

        bool hasColorChanged = color != triangleColor;

        if (m_hasTriangle
            && !hasPositionChanged
            && !hasColorChanged)
        {
            return;
        }

        m_baseTopPosition = baseTopPosition;
        m_baseBottomPosition = baseBottomPosition;
        m_tipPosition = tipPosition;

        color = triangleColor;
        m_hasTriangle = true;

        SetVerticesDirty();
    }

    /// <summary>
    /// 三角形を非表示にする。
    /// </summary>
    public void ClearTriangle()
    {
        if (!m_hasTriangle)
        {
            return;
        }

        m_hasTriangle = false;

        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(
        VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        if (!m_hasTriangle)
        {
            return;
        }

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        vertex.uv0 = Vector2.zero;

        vertex.position = m_baseTopPosition;
        vertexHelper.AddVert(vertex);

        vertex.position = m_baseBottomPosition;
        vertexHelper.AddVert(vertex);

        vertex.position = m_tipPosition;
        vertexHelper.AddVert(vertex);

        vertexHelper.AddTriangle(0, 1, 2);
    }
}