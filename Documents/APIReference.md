# APIリファレンス

Super Dungeon Wars Programming Battle では、参加者は以下の2つの基底クラスを継承して、パーティーAIとキャラクターAIを実装します。

- `ComPartyBase`：パーティー全体の状況を見て、4人の行動方針を判断するクラス
- `ComCharacterBase`：各キャラクター個別の行動を判断するクラス

`SXG_` で始まる `protected` メンバーは、継承先のクラスから利用できます。

> [!IMPORTANT]
> APIはゲームシステムの内部実体を直接公開しません。参加者プログラムには、読み取り専用の情報や命令用メソッドが提供されます。

## 基本的な実装方法

参加者は `ComPartyBase` を継承したパーティークラスと、`ComCharacterBase` を継承したキャラクタークラスを作成します。

パーティークラスでは、4人全体の役割分担や目標の決定を行います。

キャラクタークラスでは、それぞれのキャラクターが個別に行う移動、攻撃対象の選択、宝箱を拾うかどうかの判断などを実装します。

```csharp
using UnityEngine;

public class MyParty : ComPartyBase
{
    protected override void SXG_OnPartyThink()
    {
        for (int i = 0; i < SXG_GetMemberCount(); i++)
        {
            PartyMemberInfo memberInfo;

            if (!SXG_TryGetMemberInfo(i, out memberInfo))
            {
                continue;
            }

            // パーティー全体の方針に基づいて、
            // メンバーごとの移動目標を設定します。
        }
    }
}

public class MyCharacter : ComCharacterBase
{
    protected override void SXG_OnCharacterThink()
    {
        // 自分自身の状況に応じて、
        // 個別の移動目標を設定します。
    }
}
```

## パーティーAI：ComPartyBase

`ComPartyBase` は、パーティー全体の行動を制御するための基底クラスです。

4人のメンバーの状態を確認し、探索担当、宝箱の運搬担当、護衛担当などの役割分担を考えて、各メンバーへ移動命令を設定できます。

### SXG_OnPartyThink

```csharp
protected virtual void SXG_OnPartyThink()
```

パーティー単位の行動判断を行うため、定期的に呼ばれます。

このメソッドでは、既知マップ、視認中のキャラクター、視認中の宝箱、自パーティーのメンバー情報を確認できます。

また、各メンバーに対して移動目標や停止命令を設定できます。

`SXG_OnPartyThink()` は、同一フレーム内では各キャラクターの `SXG_OnCharacterThink()` より先に呼ばれます。

同じフレームにパーティー側とキャラクター側の両方が移動命令を設定した場合は、キャラクター側の命令が優先されます。

### SXG_GetKnownMap

```csharp
protected KnownMapView SXG_GetKnownMap()
```

自パーティーが現在までに観測した既知マップを取得します。

戻り値の `KnownMapView` は読み取り専用です。迷路全体ではなく、自パーティーが観測済みの情報だけを確認できます。

### SXG_GetVisibleCharacters

```csharp
protected IReadOnlyList<VisibleCharacterData> SXG_GetVisibleCharacters()
```

現在視認しているキャラクターの情報を取得します。

戻り値は読み取り専用のリストです。

### SXG_GetVisibleTreasures

```csharp
protected IReadOnlyList<VisibleTreasureData> SXG_GetVisibleTreasures()
```

現在視認している宝箱の情報を取得します。

戻り値は読み取り専用のリストです。

### SXG_TryGetReturnEntranceCell

```csharp
protected bool SXG_TryGetReturnEntranceCell(
    out Vector2Int returnEntranceCell)
```

自パーティーに対応する入口のセル座標を取得します。

取得に成功した場合は `true` を返し、`returnEntranceCell` にセル座標を設定します。

このメソッドが返すのは、自パーティーに対応する入口です。宝箱の持ち帰り先がその入口に限定されるわけではありません。宝箱は4チームのどの入口へ持ち帰っても、運んだチームの得点になります。


### SXG_GetMemberCount

```csharp
protected int SXG_GetMemberCount()
```

自パーティーに所属するメンバー数を取得します。

### SXG_TryGetMemberInfo

```csharp
protected bool SXG_TryGetMemberInfo(
    int memberIndex,
    out PartyMemberInfo memberInfo)
```

指定したメンバー番号の読み取り情報を取得します。

メンバー番号が有効な場合は `true` を返し、`memberInfo` に情報を設定します。取得できない場合は `false` を返します。

ゲーム内のキャラクター実体は取得できません。`PartyMemberInfo` を通じて、位置や状態を確認します。

### SXG_SetMemberMoveTargetCell

```csharp
protected bool SXG_SetMemberMoveTargetCell(
    int memberIndex,
    Vector2Int targetCell)
```

指定したメンバーに対し、セル座標を目標とする移動命令を設定します。

メンバーが見つかった場合は `true` を返します。実際の移動処理はゲームシステム側で行われます。

### SXG_SetMemberMoveTargetWorld

```csharp
protected bool SXG_SetMemberMoveTargetWorld(
    int memberIndex,
    Vector3 targetWorld)
```

指定したメンバーに対し、ワールド座標を目標とする移動命令を設定します。

メンバーが見つかった場合は `true` を返します。実際の移動処理はゲームシステム側で行われます。

### SXG_StopMember

```csharp
protected void SXG_StopMember(int memberIndex)
```

指定したメンバーの移動命令を停止へ変更します。

無効なメンバー番号を指定した場合、このメソッドは何も行いません。

### SXG_StopAllMembers

```csharp
protected void SXG_StopAllMembers()
```

自パーティーの全メンバーの移動命令を停止へ変更します。

## キャラクターAI：ComCharacterBase

`ComCharacterBase` は、各キャラクター個別の行動を制御するための基底クラスです。

各キャラクターは、自身の位置、移動状態、ノックアウト状態、宝箱の所持状態を確認できます。

また、自身の移動目標の設定、攻撃対象の選択、宝箱を拾うかどうかの判断を実装できます。

### 状態プロパティ

以下のプロパティは、キャラクター自身の情報を取得します。

| プロパティ | 型 | 内容 |
| --- | --- | --- |
| `SXG_CharacterId` | `int` | バトル内で一意なキャラクターIDです。 |
| `SXG_MemberIndex` | `int` | 自パーティー内でのメンバー番号です。 |
| `SXG_CurrentCell` | `Vector2Int` | 現在いる論理セル座標です。 |
| `SXG_WorldPosition` | `Vector3` | 現在のワールド座標です。 |
| `SXG_IsMoving` | `bool` | 現在、移動目標を持って移動中かを示します。 |
| `SXG_IsKnockedOut` | `bool` | 現在ノックアウト中かを示します。 |
| `SXG_IsActionLocked` | `bool` | 攻撃や起き上がりなどにより、行動がロックされているかを示します。 |
| `SXG_HasTreasure` | `bool` | 現在、宝箱を所持しているかを示します。 |

### SXG_GetKnownMap

```csharp
protected KnownMapView SXG_GetKnownMap()
```

自パーティーが現在までに観測した既知マップを取得します。

パーティーAIから取得する既知マップと同じく、自パーティーが観測済みの情報だけを確認できます。

### SXG_SetMoveTargetCell

```csharp
protected bool SXG_SetMoveTargetCell(
    Vector2Int targetCell)
```

自身に対して、セル座標を目標とする移動命令を設定します。

キャラクターの実行準備ができている場合は `true` を返します。実際の移動処理はゲームシステム側で行われます。

### SXG_SetMoveTargetWorld

```csharp
protected bool SXG_SetMoveTargetWorld(
    Vector3 targetWorld)
```

自身に対して、ワールド座標を目標とする移動命令を設定します。

キャラクターの実行準備ができている場合は `true` を返します。実際の移動処理はゲームシステム側で行われます。

### SXG_Stop

```csharp
protected void SXG_Stop()
```

自身の移動命令を停止へ変更します。

### SXG_OnCharacterThink

```csharp
protected virtual void SXG_OnCharacterThink()
```

キャラクター個別の行動判断を行うため、定期的に呼ばれます。

パーティー側の `SXG_OnPartyThink()` より後に呼ばれます。

同じフレームにパーティー側とキャラクター側の両方が移動命令を設定した場合は、このメソッドで設定したキャラクター個別の命令が優先されます。

### SXG_OnCellReached

```csharp
protected virtual void SXG_OnCellReached(
    Vector2Int cellPosition)
```

自身が新しいセルへ到達したときに呼ばれます。

呼ばれた時点で、既知マップは最新の状態に更新されています。

`cellPosition` には、到達したセル座標が渡されます。

### SXG_ChooseAttackTargetIndex

```csharp
protected virtual int SXG_ChooseAttackTargetIndex(
    IReadOnlyList<VisibleCharacterData> attackableCharacters)
```

攻撃可能な相手の候補から、攻撃する対象を選択します。

引数の `attackableCharacters` には、攻撃可能なキャラクターだけが候補として渡されます。

戻り値には、攻撃対象のキャラクターIDではなく、`attackableCharacters` 内の要素番号を返してください。

攻撃しない場合は負の値を返します。通常は `-1` を返してください。

```csharp
protected override int SXG_ChooseAttackTargetIndex(
    IReadOnlyList<VisibleCharacterData> attackableCharacters)
{
    if (attackableCharacters.Count == 0)
    {
        return -1;
    }

    return 0;
}
```

### SXG_ShouldPickUpTreasure

```csharp
protected virtual bool SXG_ShouldPickUpTreasure(
    TreasureType treasureType)
```

宝箱を取得するかどうかを判断します。

取得する場合は `true`、取得しない場合は `false` を返します。

引数の `treasureType` には、対象となる宝箱の種類が渡されます。

| 値 | 内容 |
| --- | --- |
| `TreasureType.SmallChest` | 一般の宝箱です。 |
| `TreasureType.CentralChest` | 中央の部屋に配置される金の宝箱です。 |

### SXG_OnTreasureExported

```csharp
protected virtual void SXG_OnTreasureExported(
    TreasureType treasureType)
```

自身が所持していた宝箱を入口へ搬出したときに呼ばれます。

引数の `treasureType` には、搬出した宝箱の種類が渡されます。

## 既知マップ：KnownMapView

`KnownMapView` は、自パーティーが観測した迷路情報を読み取るためのクラスです。

未観測のセルについては、通路や部屋などの情報を取得できません。

### プロパティ

| プロパティ | 型 | 内容 |
| --- | --- | --- |
| `Width` | `int` | 既知マップの幅です。 |
| `Height` | `int` | 既知マップの高さです。 |

### IsInside

```csharp
public bool IsInside(Vector2Int cellPosition)
```

指定したセル座標がマップ範囲内にあるかを返します。

### TryGetCell

```csharp
public bool TryGetCell(
    Vector2Int cellPosition,
    out KnownCellInfo cellInfo)
```

指定したセルの読み取り情報を取得します。

セル座標がマップ範囲内であり、情報を取得できる場合は `true` を返します。

### IsObserved

```csharp
public bool IsObserved(Vector2Int cellPosition)
```

指定したセルが観測済みかを返します。

範囲外のセルは `false` です。

### IsWalkable

```csharp
public bool IsWalkable(Vector2Int cellPosition)
```

指定したセルが観測済みであり、通行可能かを返します。

範囲外または未観測のセルは `false` です。

### CanMove

```csharp
public bool CanMove(
    Vector2Int from,
    Vector2Int to)
```

既知情報だけを使って、指定した2セル間を移動可能かを返します。

移動元と移動先は上下左右に隣接している必要があります。また、両方のセルが観測済みかつ通行可能であり、両方から見て通路が開通している必要があります。

### TryGetOpenNeighbor

```csharp
public bool TryGetOpenNeighbor(
    Vector2Int from,
    Vector2Int direction,
    out Vector2Int neighbor)
```

指定セルから、指定方向へ開通している隣接セルを取得します。

`direction` には、上下左右のいずれかを表す1セル分の方向ベクトルを指定します。

出発セルは観測済みである必要がありますが、隣接セルは未観測でも取得できます。

## セル情報：KnownCellInfo

`KnownCellInfo` は、既知マップ上の1セルに関する読み取り情報です。

| プロパティ | 型 | 内容 |
| --- | --- | --- |
| `CellPosition` | `Vector2Int` | セル座標です。 |
| `IsObserved` | `bool` | 視界により観測済みかを示します。 |
| `IsRoom` | `bool` | 部屋に属するセルかを示します。未観測セルでは `false` です。 |
| `IsCentralHall` | `bool` | 中央ホールに属するセルかを示します。未観測セルでは `false` です。 |
| `RoomId` | `int` | 部屋IDです。部屋ではないセル、または未観測セルでは `-1` です。 |
| `KnownOpenDirections` | `MazeDirection` | 観測済みの開通方向です。未観測セルでは `MazeDirection.None` です。 |
| `IsWalkable` | `bool` | 観測済みかつ通行可能なセルかを示します。 |

### IsOpen

```csharp
public bool IsOpen(MazeDirection direction)
```

指定方向へ開通していることが既知であるかを返します。

## パーティーメンバー情報：PartyMemberInfo

`PartyMemberInfo` は、パーティーAIが各メンバーの状態を確認するための読み取り専用クラスです。

| プロパティ | 型 | 内容 |
| --- | --- | --- |
| `CharacterId` | `int` | バトル内で一意なキャラクターIDです。 |
| `MemberIndex` | `int` | パーティー内のメンバー番号です。 |
| `CurrentCell` | `Vector2Int` | 現在いる論理セル座標です。 |
| `WorldPosition` | `Vector3` | 現在のワールド座標です。 |
| `IsMoving` | `bool` | 移動目標を持って移動中かを示します。 |
| `IsKnockedOut` | `bool` | ノックアウト中かを示します。 |
| `IsActionLocked` | `bool` | 攻撃や起き上がりなどにより、行動がロックされているかを示します。 |
| `HasTreasure` | `bool` | 宝箱を所持しているかを示します。 |

## 実装時の注意

- パーティー全体の基本方針は `SXG_OnPartyThink()` に実装してください。
- キャラクター固有の判断は `SXG_OnCharacterThink()` に実装してください。
- 同一フレームでは、キャラクター側の移動命令がパーティー側の移動命令より優先されます。
- 既知マップには、自パーティーが観測した情報だけが反映されます。未観測領域の通路や地形を前提にした判断はできません。
- `SXG_ChooseAttackTargetIndex()` の戻り値はキャラクターIDではなく、候補リスト内の添字です。
- ゲーム内キャラクターや宝箱の内部実体を直接操作することはできません。
- 移動命令を設定しても、壁、移動不能状態、ノックアウト状態などにより、直ちに移動が完了するとは限りません。

## 関連資料

- ゲームの基本ルールは、[ゲームルール](GameRules.md) を参照してください。
- Unityプロジェクトの導入方法は、[はじめに](GettingStarted.md) を参照してください。
- 提出に関する案内は、[提出ガイド](SubmissionGuide.md) を参照してください。