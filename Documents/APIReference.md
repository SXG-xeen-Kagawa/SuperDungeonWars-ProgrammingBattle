# APIリファレンス

スーパーダンジョンウォーズでは、参加者は次の2つの基底クラスを継承して、パーティーAIとキャラクターAIを実装します。

| 基底クラス | 役割 |
| --- | --- |
| `ComPartyBase` | パーティー全体の状況を見て、4人の行動方針や役割分担を判断します。 |
| `ComCharacterBase` | 各キャラクターの移動、攻撃対象の選択、宝箱を拾うかどうかなどを判断します。 |

`SXG_` で始まる `protected` メンバーは、これらを継承した参加者用クラスから利用できます。

> [!IMPORTANT]
> APIは、ゲームシステムの内部実体を直接公開しません。
> 参加者プログラムでは、提供された読み取り用の情報と命令用メソッドを使用してください。

## 基本的な実装方法

「挑戦者作成」で生成されたPartyスクリプトとCharacterスクリプトを編集し、行動判断を実装します。作成手順は、[はじめに](GettingStarted.md) を参照してください。

パーティークラスでは、探索担当、宝箱の運搬担当、護衛担当など、4人全体の役割分担や目標を決定します。

キャラクタークラスでは、自身の状態に応じた移動や、攻撃対象の選択、宝箱を拾うかどうかの判断を実装します。

以下は、行動判断を記述する場所を示す例です。このコードだけで探索や宝箱の搬出が行われるわけではありません。

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
        // 自分自身の状態に応じて、
        // 個別の移動目標を設定します。
    }
}
```

`IReadOnlyList<T>` をコード内で使用する場合は、次の名前空間も読み込んでください。

```csharp
using System.Collections.Generic;
```

> [!IMPORTANT]
> 同一フレームでは、パーティーの `SXG_OnPartyThink()` が先に、各キャラクターの `SXG_OnCharacterThink()` が後に呼ばれます。
>
> 両方から同じキャラクターへ移動命令を設定した場合は、後から設定されるキャラクター側の命令が優先されます。パーティー側で決めた移動目標を、キャラクター側で意図せず上書きしないようにしてください。

## パーティーAI：ComPartyBase

`ComPartyBase` は、パーティー全体の行動を制御するための基底クラスです。

既知マップ、視認中のキャラクターや宝箱、自パーティーのメンバー情報を確認し、各メンバーへ移動命令や停止命令を設定できます。

### SXG_OnPartyThink

```csharp
protected virtual void SXG_OnPartyThink()
```

パーティー単位の行動判断を行うため、定期的に呼ばれます。

継承先で `override` し、パーティー全体の方針や各メンバーの目標を決定してください。

同一フレームでは、各キャラクターの `SXG_OnCharacterThink()` より先に呼ばれます。

### SXG_GetKnownMap

```csharp
protected KnownMapView SXG_GetKnownMap()
```

自パーティーが現在までに観測した既知マップを取得します。

戻り値の `KnownMapView` は読み取り専用です。迷路全体の情報ではなく、自パーティーが観測した情報を確認できます。

詳細は、[既知マップ：KnownMapView](#既知マップknownmapview) を参照してください。

### SXG_GetVisibleCharacters

```csharp
protected IReadOnlyList<VisibleCharacterData> SXG_GetVisibleCharacters()
```

現在視認しているキャラクターの情報を取得します。

戻り値は `VisibleCharacterData` を要素とする読み取り専用のリストです。

このリストは視認情報です。キャラクターの内部実体を取得したり、直接操作したりするためのものではありません。

### SXG_GetVisibleTreasures

```csharp
protected IReadOnlyList<VisibleTreasureData> SXG_GetVisibleTreasures()
```

現在視認している宝箱の情報を取得します。

戻り値は `VisibleTreasureData` を要素とする読み取り専用のリストです。

宝箱を発見しただけでは得点になりません。宝箱を拾い、入口へ搬出する必要があります。

### SXG_TryGetReturnEntranceCell

```csharp
protected bool SXG_TryGetReturnEntranceCell(
    out Vector2Int returnEntranceCell)
```

自パーティーの入口に対応する搬出口セルを取得します。

取得に成功した場合は `true` を返し、`returnEntranceCell` にセル座標を設定します。戻り値を確認してから、取得した座標を使用してください。

> [!NOTE]
> このメソッドで取得するのは、自パーティーの入口です。
> 宝箱の搬出先が、その入口だけに限定されるわけではありません。
> ゲームルール上は、4チームのどの入口へ搬出しても、運んだチームの得点になります。

### SXG_GetMemberCount

```csharp
protected int SXG_GetMemberCount()
```

自パーティーに所属するメンバー数を取得します。

メンバーを順番に処理する場合は、この戻り値を使って繰り返し処理を行い、`SXG_TryGetMemberInfo()` の成功を確認してください。

### SXG_TryGetMemberInfo

```csharp
protected bool SXG_TryGetMemberInfo(
    int memberIndex,
    out PartyMemberInfo memberInfo)
```

指定したメンバー番号の読み取り情報を取得します。

メンバー番号が有効で、情報を取得できた場合は `true` を返し、`memberInfo` に情報を設定します。取得できない場合は `false` を返します。

`memberIndex` に指定するのは、パーティー内のメンバー番号です。バトル内で一意な `CharacterId` とは区別してください。

ゲーム内のキャラクター実体は取得できません。`PartyMemberInfo` を通じて、位置や状態を確認します。

詳細は、[パーティーメンバー情報：PartyMemberInfo](#パーティーメンバー情報partymemberinfo) を参照してください。

### SXG_SetMemberMoveTargetCell

```csharp
protected bool SXG_SetMemberMoveTargetCell(
    int memberIndex,
    Vector2Int targetCell)
```

指定したメンバーに対し、セル座標を目標とする移動命令を設定します。

| 引数 | 内容 |
| --- | --- |
| `memberIndex` | 命令を設定するメンバー番号です。 |
| `targetCell` | 移動目標となる論理セル座標です。 |

メンバーが見つかった場合は `true` を返します。実際の移動処理はゲームシステム側で行われます。

戻り値が `true` でも、目標への到着が完了したことを意味しません。

### SXG_SetMemberMoveTargetWorld

```csharp
protected bool SXG_SetMemberMoveTargetWorld(
    int memberIndex,
    Vector3 targetWorld)
```

指定したメンバーに対し、ワールド座標を目標とする移動命令を設定します。

| 引数 | 内容 |
| --- | --- |
| `memberIndex` | 命令を設定するメンバー番号です。 |
| `targetWorld` | 移動目標となるワールド座標です。 |

メンバーが見つかった場合は `true` を返します。実際の移動処理はゲームシステム側で行われます。

戻り値が `true` でも、目標への到着が完了したことを意味しません。

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

自身の位置、移動状態、ノックアウト状態、宝箱の所持状態を確認できます。また、自身の移動目標の設定、攻撃対象の選択、宝箱を拾うかどうかの判断を実装できます。

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

`SXG_CharacterId` と `SXG_MemberIndex` は、用途が異なります。メンバー番号を受け取るパーティーAPIには、`SXG_MemberIndex` に対応する番号を使用してください。

ゲームにはHPや死亡の概念はありません。攻撃を受けると吹っ飛ばされ、一定時間行動できなくなった後に起き上がります。

また、宝箱を運搬している間は移動速度が低下し、攻撃できません。

### SXG_GetKnownMap

```csharp
protected KnownMapView SXG_GetKnownMap()
```

自パーティーが現在までに観測した既知マップを取得します。

パーティーAIから取得する既知マップと同じく、自パーティーが観測した情報を確認できます。

### SXG_SetMoveTargetCell

```csharp
protected bool SXG_SetMoveTargetCell(
    Vector2Int targetCell)
```

自身に対して、セル座標を目標とする移動命令を設定します。

キャラクターの実行準備ができている場合は `true` を返します。実際の移動処理はゲームシステム側で行われます。

戻り値は、目的地への到着結果ではありません。壁や行動不能状態などにより、直ちに移動が完了するとは限りません。

### SXG_SetMoveTargetWorld

```csharp
protected bool SXG_SetMoveTargetWorld(
    Vector3 targetWorld)
```

自身に対して、ワールド座標を目標とする移動命令を設定します。

キャラクターの実行準備ができている場合は `true` を返します。実際の移動処理はゲームシステム側で行われます。

戻り値は、目的地への到着結果ではありません。セル座標を使用する `SXG_SetMoveTargetCell()` と区別してください。

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

継承先で `override` し、自身の状態に応じた判断を実装してください。

同一フレームでは、パーティー側の `SXG_OnPartyThink()` より後に呼ばれます。両方が移動命令を設定した場合は、キャラクター側の命令が優先されます。

### SXG_OnCellReached

```csharp
protected virtual void SXG_OnCellReached(
    Vector2Int cellPosition)
```

自身が新しいセルへ到達したときに呼ばれます。

呼ばれた時点で、既知マップは最新の状態に更新されています。

`cellPosition` には、到達したセル座標が渡されます。到達後の既知情報を使って、次の移動目標を判断する際に利用できます。

### SXG_ChooseAttackTargetIndex

```csharp
protected virtual int SXG_ChooseAttackTargetIndex(
    IReadOnlyList<VisibleCharacterData> attackableCharacters)
```

攻撃可能な相手の候補から、攻撃する対象を選択します。

引数の `attackableCharacters` には、攻撃可能なキャラクターだけが候補として渡されます。

戻り値には、`attackableCharacters` 内の要素番号を返してください。攻撃しない場合は負の値を返します。通常は `-1` を使用してください。

> [!IMPORTANT]
> 戻り値はキャラクターIDではなく、渡された候補リスト内の添字です。
> 攻撃する場合は、`0` 以上、`attackableCharacters.Count` 未満の値を返してください。

以下は、候補があれば先頭の相手を選び、候補がなければ攻撃しない例です。

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

この例で `IReadOnlyList<T>` を使用するため、スクリプトの先頭に次の記述が必要です。

```csharp
using System.Collections.Generic;
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
| `TreasureType.SmallChest` | 一般宝箱です。 |
| `TreasureType.CentralChest` | 中央の部屋に配置される金の宝箱です。 |

このメソッドは、宝箱を拾うかどうかを判断するためのものです。宝箱を発見したり、拾ったりしただけでは得点になりません。

### SXG_OnTreasureExported

```csharp
protected virtual void SXG_OnTreasureExported(
    TreasureType treasureType)
```

自身が所持していた宝箱を入口へ搬出したときに呼ばれます。

引数の `treasureType` には、搬出した宝箱の種類が渡されます。

搬出後の役割変更や、次の探索目標の判断などに利用できます。

## 既知マップ：KnownMapView

`KnownMapView` は、自パーティーが観測した迷路情報を読み取るためのクラスです。

未観測のセルについては、通路や部屋などの詳細情報を取得できません。

マップ範囲内にあること、観測済みであること、通行可能であることは、それぞれ異なる条件です。目的に合ったメソッドを使用してください。

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

`true` であっても、そのセルが観測済み、または通行可能であることを意味しません。

### TryGetCell

```csharp
public bool TryGetCell(
    Vector2Int cellPosition,
    out KnownCellInfo cellInfo)
```

指定したセルの読み取り情報を取得します。

セル座標がマップ範囲内であり、情報を取得できる場合は `true` を返します。

戻り値を確認してから `cellInfo` を使用してください。また、情報を取得できたことと、そのセルが観測済みであることは区別してください。観測状態は `cellInfo.IsObserved` で確認できます。

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

未観測セルに対する `false` は、そのセルが壁であると判明したことを意味しません。

### CanMove

```csharp
public bool CanMove(
    Vector2Int from,
    Vector2Int to)
```

既知情報だけを使って、指定した2セル間を移動可能かを返します。

移動可能と判定されるには、次の条件を満たす必要があります。

- 移動元と移動先が、上下左右に隣接している必要があります。
- 移動元のセルが、観測済みかつ通行可能である必要があります。
- 移動先のセルが、観測済みかつ通行可能である必要があります。
- 両方のセルから見て、互いの方向へ通路が開通している必要があります。

このメソッドは、隣接する2セル間の接続を確認するためのものです。離れた目的地までの経路全体を判定するものではありません。

### TryGetOpenNeighbor

```csharp
public bool TryGetOpenNeighbor(
    Vector2Int from,
    Vector2Int direction,
    out Vector2Int neighbor)
```

指定セルから、指定方向へ開通している隣接セルを取得します。

`direction` には、上下左右のいずれかを表す1セル分の方向ベクトルを指定します。

```csharp
Vector2Int.up
Vector2Int.down
Vector2Int.left
Vector2Int.right
```

出発セルは観測済みである必要がありますが、隣接セルは未観測でも取得できます。

> [!NOTE]
> `CanMove()` は、移動元と移動先の両方が観測済みであることを必要とします。
>
> 一方、`TryGetOpenNeighbor()` は、観測済みの出発セルから開通している方向を調べるため、未観測の隣接セルも取得できます。
> ただし、取得した隣接セルの部屋情報や、その先の通路まで分かるわけではありません。

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

未観測セルの `IsRoom` が `false` であっても、「部屋ではない」と判明したことを意味しません。まず `IsObserved` を確認してください。

### IsOpen

```csharp
public bool IsOpen(MazeDirection direction)
```

指定方向へ開通していることが既知であるかを返します。

引数には `MazeDirection` 型の方向を指定します。`KnownMapView.TryGetOpenNeighbor()` が受け取る `Vector2Int` 型の方向ベクトルとは異なるため、混同しないようにしてください。

## パーティーメンバー情報：PartyMemberInfo

`PartyMemberInfo` は、パーティーAIが各メンバーの状態を確認するための読み取り専用クラスです。

`SXG_TryGetMemberInfo()` で取得します。

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

この情報を直接書き換えてキャラクターを操作するのではなく、移動や停止には `ComPartyBase` の命令用メソッドを使用してください。

## 実装時の注意

- パーティー全体の基本方針や役割分担は、`SXG_OnPartyThink()` に実装してください。
- キャラクター固有の判断は、`SXG_OnCharacterThink()` に実装してください。
- 同一フレームでは、キャラクター側の移動命令がパーティー側の移動命令より優先されます。
- 既知マップには、自パーティーが観測した情報だけが反映されます。未観測領域の通路や地形を前提にした判断はできません。
- `Try` で始まるメソッドでは、戻り値を確認してから `out` 引数で取得した情報を使用してください。
- キャラクターID、パーティー内のメンバー番号、攻撃候補リスト内の添字は、それぞれ異なります。APIが求める値を確認してください。
- `SXG_ChooseAttackTargetIndex()` の戻り値は、キャラクターIDではなく、渡された候補リスト内の添字です。
- セル座標には `Vector2Int`、ワールド座標には `Vector3` を使用します。移動命令の引数を混同しないようにしてください。
- 移動命令の戻り値が `true` でも、目的地への到着を意味しません。壁、移動不能状態、ノックアウト状態などにより、直ちに移動が完了するとは限りません。
- 宝箱は、発見や所持だけでは得点になりません。入口への搬出が必要です。
- 宝箱の搬出先は、自パーティーの入口に限定されません。4チームのどの入口へ搬出しても、運んだチームの得点になります。
- ゲーム内キャラクターや宝箱の内部実体を直接操作せず、提供された読み取り情報と命令APIを使用してください。
- 想定外の内部情報の取得や変更、ゲーム進行への妨害、他参加者のデータに影響を与える処理は禁止です。

## 関連資料

ゲームの基本ルールは、[ゲームルール](GameRules.md) を参照してください。

Unityプロジェクトの導入方法と参加者データの作成手順は、[はじめに](GettingStarted.md) を参照してください。

提出用ZIPの作成と提出方法は、[提出ガイド](SubmissionGuide.md) を参照してください。