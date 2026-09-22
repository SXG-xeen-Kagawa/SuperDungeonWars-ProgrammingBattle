using UnityEngine;

public static class TreasurePresentationConstants
{
    // 左右の手の中間に置かれる TreasureCarryAnchor を基準にした微調整値。
    // 宝箱のモデル原点と手の位置がずれる場合は、ここだけ調整する。
    public static readonly Vector3 s_carryLocalPosition =
        new Vector3(0.0f, 0.0f, 0.1f);

    // 宝箱の正面・上下がモデル都合で合わない場合だけ調整する。
    public static readonly Quaternion s_carryLocalRotation =
        Quaternion.identity;
}