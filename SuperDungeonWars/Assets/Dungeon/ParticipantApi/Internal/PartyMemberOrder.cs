using UnityEngine;

public enum PartyMemberOrderType
{
    None,
    MoveToCell,
    MoveToWorld,
    Stop,
}

public struct PartyMemberOrder
{
    public PartyMemberOrderType m_orderType;
    public Vector2Int m_targetCell;
    public Vector3 m_targetWorld;

    public static PartyMemberOrder CreateMoveToCell(Vector2Int targetCell)
    {
        PartyMemberOrder order = new PartyMemberOrder();
        order.m_orderType = PartyMemberOrderType.MoveToCell;
        order.m_targetCell = targetCell;
        return order;
    }

    public static PartyMemberOrder CreateMoveToWorld(Vector3 targetWorld)
    {
        PartyMemberOrder order = new PartyMemberOrder();
        order.m_orderType = PartyMemberOrderType.MoveToWorld;
        order.m_targetWorld = targetWorld;
        return order;
    }

    public static PartyMemberOrder CreateStop()
    {
        PartyMemberOrder order = new PartyMemberOrder();
        order.m_orderType = PartyMemberOrderType.Stop;
        return order;
    }
}