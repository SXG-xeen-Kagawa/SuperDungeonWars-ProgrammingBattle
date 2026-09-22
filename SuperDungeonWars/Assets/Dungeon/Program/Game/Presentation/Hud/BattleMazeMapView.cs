using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;




public class BattleMazeMapView : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BattlePrototypeGameController m_battleGameController;

    [SerializeField] private RectTransform m_squareArea;
    [SerializeField] private RectTransform m_cellLayer;
    [SerializeField] private RectTransform m_treasureMarkerLayer;
    [SerializeField] private RectTransform m_characterMarkerLayer;
    [SerializeField] private RectTransform m_fogLayer;

    [Header("Cell Colors")]
    [SerializeField]
    private Color m_blockedCellColor =
        new Color(0.12f, 0.12f, 0.12f, 1.00f);

    [SerializeField]
    private Color m_corridorCellColor =
        new Color(0.55f, 0.55f, 0.60f, 1.00f);

    [SerializeField]
    private Color m_roomCellColor =
        new Color(0.45f, 0.50f, 0.65f, 1.00f);

    [SerializeField]
    private Color m_centralHallCellColor =
        new Color(0.20f, 0.28f, 0.34f, 1.00f);

    [SerializeField]
    private Color m_outerWallColor =
        new Color(0.05f, 0.05f, 0.05f, 1.00f);



    [SerializeField]
    private Color m_unknownFogColor =
        new Color(0.08f, 0.11f, 0.16f, 0.96f);


    [Header("Flowing Fog")]
    [SerializeField] private Shader m_flowingFogShader;

    [SerializeField]
    private Color m_flowingFogBaseColor =
        new Color(0.015f, 0.045f, 0.070f, 1.00f);

    [SerializeField]
    private Color m_flowingFogCloudColor =
        new Color(0.060f, 0.220f, 0.310f, 1.00f);

    [SerializeField, Range(1.0f, 20.0f)]
    private float m_flowingFogNoiseScale = 7.0f;

    [SerializeField]
    private Vector2 m_flowingFogDirection =
        new Vector2(0.70f, 0.30f);

    [SerializeField, Range(0.01f, 1.00f)]
    private float m_flowingFogSpeed = 0.18f;

    [SerializeField, Range(0.10f, 3.00f)]
    private float m_flowingFogCloudContrast = 1.35f;


    private Image m_fogImage;
    private Material m_flowingFogMaterial;
    private Texture2D m_fogVisibilityMaskTexture;
    private bool m_isFogVisibilityMaskDirty;



    [Header("Marker Colors")]
    [SerializeField]
    private Color m_characterOutlineColor =
        new Color(0.05f, 0.05f, 0.05f, 1.00f);

    [SerializeField]
    private Color m_knockedOutCrossColor =
    new Color(0.12f, 0.12f, 0.12f, 1.00f);

    [SerializeField, Range(0.05f, 0.40f)]
    private float m_knockedOutCrossThicknessRatio = 0.16f;


    [Header("Treasure Sprites")]
    [SerializeField] private Sprite m_smallTreasureSprite;
    [SerializeField] private Sprite m_centralTreasureSprite;


    [SerializeField]
    private Color m_treasureOutlineColor =
        new Color(0.12f, 0.07f, 0.02f, 1.00f);

    [SerializeField]
    private Color m_smallTreasureColor =
        new Color(0.65f, 0.34f, 0.10f, 1.00f);

    [SerializeField]
    private Color m_centralTreasureColor =
        new Color(1.00f, 0.76f, 0.10f, 1.00f);


    [Header("Marker Position")]
    [SerializeField]
    private bool m_useWorldPositionForMarkers = true;


    [Header("Marker Size")]
    [SerializeField, Range(0.10f, 1.00f)]
    private float m_characterMarkerCellRatio = 0.56f;

    [SerializeField, Range(0.10f, 3.00f)]
    private float m_smallTreasureMarkerCellRatio = 0.46f;

    [SerializeField, Range(1.00f, 3.00f)]
    private float m_centralTreasureScale = 1.60f;

    [SerializeField, Range(0.02f, 0.40f)]
    private float m_outerWallCellRatio = 0.12f;


    private class CharacterMarkerVisual
    {
        public RectTransform m_rootRectTransform;
        public GameObject m_knockedOutCrossObject;
    }



    private readonly Dictionary<ComCharacterBase, CharacterMarkerVisual>
        m_characterMarkers =
            new Dictionary<ComCharacterBase, CharacterMarkerVisual>();

    private readonly Dictionary<TreasureChest, RectTransform>
        m_treasureMarkers =
            new Dictionary<TreasureChest, RectTransform>();

    private readonly List<KnownMapData> m_subscribedKnownMapData =
        new List<KnownMapData>();



    private MazeData m_displayedMazeData;

    private Sprite m_squareSprite;
    private Sprite m_circleSprite;
    private Texture2D m_squareTexture;
    private Texture2D m_circleTexture;




    private void Awake()
    {
        CreateRuntimeSprites();
        RefreshSquareArea();
    }

    private void LateUpdate()
    {
        RefreshSquareArea();

        if (m_battleGameController == null)
        {
            return;
        }

        MazeData mazeData =
            m_battleGameController.MazeData;

        if (mazeData != m_displayedMazeData)
        {
            Rebuild(mazeData);
        }

        UpdateCharacterMarkers();
        UpdateTreasureMarkers();

        ApplyFogVisibilityMask();
    }

    private void OnRectTransformDimensionsChange()
    {
        RefreshSquareArea();
    }

    private void OnDestroy()
    {
        UnsubscribeKnownMapData();
        ReleaseFlowingFogResources();
        ReleaseRuntimeSprites();
    }

    private void Rebuild(MazeData mazeData)
    {
        ClearView();

        m_displayedMazeData = mazeData;

        if (m_displayedMazeData == null)
        {
            return;
        }

        BuildMazeCells();
        BuildCharacterMarkers();
        BuildTreasureMarkers();

        BuildFlowingFog();
        SubscribeKnownMapData();

        RefreshAllFogCells();
        ApplyFogVisibilityMask();
    }

    private void ClearView()
    {
        UnsubscribeKnownMapData();

        ClearChildren(m_cellLayer);
        ClearChildren(m_treasureMarkerLayer);
        ClearChildren(m_characterMarkerLayer);
        ClearChildren(m_fogLayer);

        ReleaseFlowingFogResources();

        m_characterMarkers.Clear();
        m_treasureMarkers.Clear();
    }

    private void BuildMazeCells()
    {
        if (m_displayedMazeData == null
            || m_cellLayer == null)
        {
            return;
        }

        int width = m_displayedMazeData.m_width;
        int height = m_displayedMazeData.m_height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                MazeCellData cell =
                    m_displayedMazeData.GetCell(x, y);

                if (cell == null)
                {
                    continue;
                }

                Image cellImage = CreateImage(
                    $"Cell_{x}_{y}",
                    m_cellLayer,
                    m_squareSprite);

                RectTransform rectTransform =
                    cellImage.rectTransform;

                rectTransform.anchorMin = new Vector2(
                    (float)x / width,
                    (float)y / height);

                rectTransform.anchorMax = new Vector2(
                    (float)(x + 1) / width,
                    (float)(y + 1) / height);

                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;

                cellImage.color = GetCellColor(cell);
            }
        }

        CreateOuterWallImages();
    }



    private void SubscribeKnownMapData()
    {
        UnsubscribeKnownMapData();

        if (m_battleGameController == null)
        {
            return;
        }

        IReadOnlyList<ComPartyBase> parties =
            m_battleGameController.SpawnedParties;

        if (parties == null)
        {
            return;
        }

        for (int i = 0; i < parties.Count; i++)
        {
            ComPartyBase party = parties[i];

            if (party == null
                || party.GetKnownMapData() == null)
            {
                continue;
            }

            KnownMapData knownMapData =
                party.GetKnownMapData();

            if (m_subscribedKnownMapData.Contains(
                    knownMapData))
            {
                continue;
            }

            knownMapData.m_onKnownCellUpdated +=
                OnKnownCellUpdated;

            m_subscribedKnownMapData.Add(knownMapData);
        }
    }

    private void UnsubscribeKnownMapData()
    {
        for (int i = 0;
             i < m_subscribedKnownMapData.Count;
             i++)
        {
            KnownMapData knownMapData =
                m_subscribedKnownMapData[i];

            if (knownMapData == null)
            {
                continue;
            }

            knownMapData.m_onKnownCellUpdated -=
                OnKnownCellUpdated;
        }

        m_subscribedKnownMapData.Clear();
    }

    private void OnKnownCellUpdated(
        int x,
        int y,
        KnownCellData knownCellData)
    {
        RefreshFogCell(x, y);
    }




    private void BuildFlowingFog()
    {
        if (m_displayedMazeData == null
            || m_fogLayer == null)
        {
            return;
        }

        if (m_flowingFogShader == null)
        {
            Debug.LogError(
                "BattleMazeMapView: "
                + "m_flowingFogShader が未設定です。",
                this);

            return;
        }

        int width = m_displayedMazeData.m_width;
        int height = m_displayedMazeData.m_height;

        m_fogVisibilityMaskTexture = new Texture2D(
            width,
            height,
            TextureFormat.RGBA32,
            false);

        m_fogVisibilityMaskTexture.name =
            "MazeFogVisibilityMask";

        m_fogVisibilityMaskTexture.filterMode =
            FilterMode.Point;

        m_fogVisibilityMaskTexture.wrapMode =
            TextureWrapMode.Clamp;

        Color[] initialPixels =
            new Color[width * height];

        for (int i = 0; i < initialPixels.Length; i++)
        {
            initialPixels[i] = Color.black;
        }

        m_fogVisibilityMaskTexture.SetPixels(
            initialPixels);

        m_fogVisibilityMaskTexture.Apply(
            false,
            false);

        m_flowingFogMaterial = new Material(
            m_flowingFogShader);

        m_flowingFogMaterial.name =
            "MazeFlowingFogMaterial_Runtime";

        m_flowingFogMaterial.SetTexture(
            "_VisibilityMask",
            m_fogVisibilityMaskTexture);

        m_flowingFogMaterial.SetColor(
            "_FogBaseColor",
            m_flowingFogBaseColor);

        m_flowingFogMaterial.SetColor(
            "_FogCloudColor",
            m_flowingFogCloudColor);

        m_flowingFogMaterial.SetFloat(
            "_NoiseScale",
            m_flowingFogNoiseScale);

        m_flowingFogMaterial.SetVector(
            "_FlowDirection",
            new Vector4(
                m_flowingFogDirection.x,
                m_flowingFogDirection.y,
                0.0f,
                0.0f));

        m_flowingFogMaterial.SetFloat(
            "_FlowSpeed",
            m_flowingFogSpeed);

        m_flowingFogMaterial.SetFloat(
            "_CloudContrast",
            m_flowingFogCloudContrast);

        m_fogImage = CreateImage(
            "FlowingFog",
            m_fogLayer,
            m_squareSprite);

        RectTransform fogRectTransform =
            m_fogImage.rectTransform;

        fogRectTransform.anchorMin = Vector2.zero;
        fogRectTransform.anchorMax = Vector2.one;
        fogRectTransform.offsetMin = Vector2.zero;
        fogRectTransform.offsetMax = Vector2.zero;

        m_fogImage.color = Color.white;
        m_fogImage.material = m_flowingFogMaterial;
    }

    private void RefreshAllFogCells()
    {
        if (m_fogVisibilityMaskTexture == null
            || m_displayedMazeData == null)
        {
            return;
        }

        for (int y = 0;
             y < m_displayedMazeData.m_height;
             y++)
        {
            for (int x = 0;
                 x < m_displayedMazeData.m_width;
                 x++)
            {
                RefreshFogCell(x, y);
            }
        }
    }

    private void RefreshFogCell(
        int x,
        int y)
    {
        if (m_fogVisibilityMaskTexture == null
            || m_displayedMazeData == null
            || x < 0
            || y < 0
            || x >= m_displayedMazeData.m_width
            || y >= m_displayedMazeData.m_height)
        {
            return;
        }

        bool isObserved =
            IsObservedByAnyParty(x, y);

        m_fogVisibilityMaskTexture.SetPixel(
            x,
            y,
            isObserved
                ? Color.white
                : Color.black);

        m_isFogVisibilityMaskDirty = true;
    }

    private void ApplyFogVisibilityMask()
    {
        if (!m_isFogVisibilityMaskDirty
            || m_fogVisibilityMaskTexture == null)
        {
            return;
        }

        m_fogVisibilityMaskTexture.Apply(
            false,
            false);

        m_isFogVisibilityMaskDirty = false;
    }

    private void ReleaseFlowingFogResources()
    {
        if (m_fogImage != null)
        {
            m_fogImage.material = null;
            m_fogImage = null;
        }

        if (m_flowingFogMaterial != null)
        {
            Destroy(m_flowingFogMaterial);
            m_flowingFogMaterial = null;
        }

        if (m_fogVisibilityMaskTexture != null)
        {
            Destroy(m_fogVisibilityMaskTexture);
            m_fogVisibilityMaskTexture = null;
        }

        m_isFogVisibilityMaskDirty = false;
    }





    private bool IsObservedByAnyParty(
        int x,
        int y)
    {
        if (m_battleGameController == null)
        {
            return false;
        }

        int restrictedSystemTeamIndex =
            BattleDeveloperSpectatorSettings
                .GetRestrictedSystemTeamIndex();

        if (restrictedSystemTeamIndex >= 0)
        {
            return IsObservedByPartyIndex(
                restrictedSystemTeamIndex,
                x,
                y);
        }

        return IsObservedByAllPartiesOr(
            x,
            y);
    }


    private bool IsObservedByAllPartiesOr(
        int x,
        int y)
    {
        IReadOnlyList<ComPartyBase> parties =
            m_battleGameController.SpawnedParties;

        if (parties == null)
        {
            return false;
        }

        for (int i = 0; i < parties.Count; i++)
        {
            if (IsObservedByPartyIndex(i, x, y))
            {
                return true;
            }
        }

        return false;
    }


    private bool IsObservedByPartyIndex(
        int partyIndex,
        int x,
        int y)
    {
        IReadOnlyList<ComPartyBase> parties =
            m_battleGameController.SpawnedParties;

        if (parties == null
            || partyIndex < 0
            || partyIndex >= parties.Count)
        {
            return false;
        }

        ComPartyBase party = parties[partyIndex];

        return party != null
            && party.GetKnownMapData() != null
            && party.GetKnownMapData().IsObserved(x, y);
    }

    public void RefreshFogVisibilityByDeveloperSetting()
    {
        RefreshAllFogCells();
        ApplyFogVisibilityMask();
    }




    private Color GetCellColor(MazeCellData cell)
    {
        if (cell.IsBlockedCell())
        {
            return m_blockedCellColor;
        }

        if (cell.m_isCentralHall)
        {
            return m_centralHallCellColor;
        }

        if (cell.m_isRoom)
        {
            return m_roomCellColor;
        }

        return m_corridorCellColor;
    }

    private void CreateOuterWallImages()
    {
        if (m_displayedMazeData == null
            || m_cellLayer == null)
        {
            return;
        }

        float cellRatio =
            1.0f / Mathf.Max(
                m_displayedMazeData.m_width,
                m_displayedMazeData.m_height);

        float thickness =
            cellRatio * m_outerWallCellRatio;

        CreateOuterWallImage(
            "OuterWall_Left",
            new Vector2(0.0f, 0.0f),
            new Vector2(thickness, 1.0f));

        CreateOuterWallImage(
            "OuterWall_Right",
            new Vector2(1.0f - thickness, 0.0f),
            new Vector2(1.0f, 1.0f));

        CreateOuterWallImage(
            "OuterWall_Bottom",
            new Vector2(0.0f, 0.0f),
            new Vector2(1.0f, thickness));

        CreateOuterWallImage(
            "OuterWall_Top",
            new Vector2(0.0f, 1.0f - thickness),
            new Vector2(1.0f, 1.0f));
    }

    private void CreateOuterWallImage(
        string wallName,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        Image wallImage = CreateImage(
            wallName,
            m_cellLayer,
            m_squareSprite);

        RectTransform rectTransform =
            wallImage.rectTransform;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        wallImage.color = m_outerWallColor;
    }

    private void BuildCharacterMarkers()
    {
        if (m_battleGameController == null
            || m_characterMarkerLayer == null)
        {
            return;
        }

        IReadOnlyList<ComPartyBase> parties =
            m_battleGameController.SpawnedParties;

        if (parties == null)
        {
            return;
        }

        for (int teamIndex = 0;
             teamIndex < parties.Count;
             teamIndex++)
        {
            ComPartyBase party = parties[teamIndex];

            if (party == null)
            {
                continue;
            }

            Color teamColor =
                CharacterTeamColorConstants.GetTeamBaseColor(
                    teamIndex);

            int memberCount = party.MemberCount;

            for (int memberIndex = 0;
                 memberIndex < memberCount;
                 memberIndex++)
            {
                ComCharacterBase member = null;
                party.TryGetMember(memberIndex, out member);

                if (member == null)
                {
                    continue;
                }

                CharacterMarkerVisual markerVisual =
                    CreateCharacterMarker(
                        $"Character_Team{teamIndex}_Member{memberIndex}",
                        teamColor);

                m_characterMarkers.Add(member, markerVisual);
            }
        }
    }

    private CharacterMarkerVisual CreateCharacterMarker(
        string markerName,
        Color teamColor)
    {
        Image outlineImage = CreateImage(
            markerName,
            m_characterMarkerLayer,
            m_circleSprite);

        outlineImage.color = m_characterOutlineColor;

        Image fillImage = CreateImage(
            "Fill",
            outlineImage.rectTransform,
            m_circleSprite);

        fillImage.color = teamColor;

        RectTransform fillRectTransform =
            fillImage.rectTransform;

        fillRectTransform.anchorMin =
            new Vector2(0.18f, 0.18f);

        fillRectTransform.anchorMax =
            new Vector2(0.82f, 0.82f);

        fillRectTransform.offsetMin = Vector2.zero;
        fillRectTransform.offsetMax = Vector2.zero;

        GameObject knockedOutCrossObject =
            CreateKnockedOutCross(
                outlineImage.rectTransform);

        knockedOutCrossObject.SetActive(false);

        CharacterMarkerVisual result =
            new CharacterMarkerVisual();

        result.m_rootRectTransform =
            outlineImage.rectTransform;

        result.m_knockedOutCrossObject =
            knockedOutCrossObject;

        return result;
    }

    private GameObject CreateKnockedOutCross(
    RectTransform parent)
    {
        GameObject crossRoot = new GameObject(
            "KnockedOutCross",
            typeof(RectTransform));

        crossRoot.transform.SetParent(parent, false);

        RectTransform rootRectTransform =
            crossRoot.GetComponent<RectTransform>();

        rootRectTransform.anchorMin = Vector2.zero;
        rootRectTransform.anchorMax = Vector2.one;
        rootRectTransform.offsetMin = Vector2.zero;
        rootRectTransform.offsetMax = Vector2.zero;

        CreateKnockedOutCrossLine(
            rootRectTransform,
            "CrossLine_LeftToRight",
            45.0f);

        CreateKnockedOutCrossLine(
            rootRectTransform,
            "CrossLine_RightToLeft",
            -45.0f);

        return crossRoot;
    }

    private void CreateKnockedOutCrossLine(
        RectTransform parent,
        string objectName,
        float rotationZ)
    {
        Image lineImage = CreateImage(
            objectName,
            parent,
            m_squareSprite);

        lineImage.color = m_knockedOutCrossColor;

        RectTransform lineRectTransform =
            lineImage.rectTransform;

        lineRectTransform.anchorMin =
            new Vector2(0.50f, 0.50f);

        lineRectTransform.anchorMax =
            new Vector2(0.50f, 0.50f);

        lineRectTransform.pivot =
            new Vector2(0.50f, 0.50f);

        lineRectTransform.sizeDelta =
            new Vector2(
                1.15f,
                m_knockedOutCrossThicknessRatio);

        lineRectTransform.localRotation =
            Quaternion.Euler(0.0f, 0.0f, rotationZ);
    }


    private void BuildTreasureMarkers()
    {
        if (m_battleGameController == null
            || m_treasureMarkerLayer == null)
        {
            return;
        }

        IReadOnlyList<TreasureChest> treasureChests =
            m_battleGameController.SpawnedTreasureChests;

        if (treasureChests == null)
        {
            return;
        }

        for (int i = 0; i < treasureChests.Count; i++)
        {
            TreasureChest treasureChest =
                treasureChests[i];

            if (treasureChest == null)
            {
                continue;
            }

            bool isCentralChest =
                treasureChest.TreasureType
                == TreasureType.CentralChest;

            RectTransform marker =
                CreateTreasureMarker(
                    $"Treasure_{treasureChest.TreasureId}",
                    isCentralChest);

            m_treasureMarkers.Add(
                treasureChest,
                marker);
        }
    }

    private RectTransform CreateTreasureMarker(
        string markerName,
        bool isCentralChest)
    {
        Sprite treasureSprite = isCentralChest
            ? m_centralTreasureSprite
            : m_smallTreasureSprite;

        // 用意した宝箱Spriteが設定されている場合は、
        // そのままUI Imageとして表示する。
        if (treasureSprite != null)
        {
            Image treasureImage = CreateImage(
                markerName,
                m_treasureMarkerLayer,
                treasureSprite);

            treasureImage.color = Color.white;
            treasureImage.preserveAspect = true;

            return treasureImage.rectTransform;
        }

        // Sprite未設定時だけ、従来の簡易ひし形マーカーを使用する。
        Image outlineImage = CreateImage(
            markerName,
            m_treasureMarkerLayer,
            m_squareSprite);

        outlineImage.color = m_treasureOutlineColor;
        outlineImage.rectTransform.localRotation =
            Quaternion.Euler(0.0f, 0.0f, 45.0f);

        Image fillImage = CreateImage(
            "Fill",
            outlineImage.rectTransform,
            m_squareSprite);

        fillImage.color = isCentralChest
            ? m_centralTreasureColor
            : m_smallTreasureColor;

        RectTransform fillRectTransform =
            fillImage.rectTransform;

        fillRectTransform.anchorMin =
            new Vector2(0.22f, 0.22f);

        fillRectTransform.anchorMax =
            new Vector2(0.78f, 0.78f);

        fillRectTransform.offsetMin = Vector2.zero;
        fillRectTransform.offsetMax = Vector2.zero;

        return outlineImage.rectTransform;
    }

    private void UpdateCharacterMarkers()
    {
        if (m_displayedMazeData == null)
        {
            return;
        }

        foreach (
            KeyValuePair<ComCharacterBase, CharacterMarkerVisual>
            pair in m_characterMarkers)
        {
            ComCharacterBase character = pair.Key;
            CharacterMarkerVisual markerVisual = pair.Value;

            if (character == null
                || markerVisual == null
                || markerVisual.m_rootRectTransform == null)
            {
                continue;
            }

            RectTransform marker =
                markerVisual.m_rootRectTransform;

            bool isInsideMaze;

            if (m_useWorldPositionForMarkers)
            {
                isInsideMaze = TrySetMarkerToWorldPosition(
                    marker,
                    character.GetWorldPosition());
            }
            else
            {
                Vector2Int cellPosition;

                isInsideMaze =
                    m_displayedMazeData.TryWorldToCell(
                        character.GetWorldPosition(),
                        out cellPosition);

                if (isInsideMaze)
                {
                    SetMarkerToCell(marker, cellPosition);
                }
            }

            marker.gameObject.SetActive(isInsideMaze);

            if (!isInsideMaze)
            {
                continue;
            }

            SetMarkerSize(
                marker,
                m_characterMarkerCellRatio);

            RefreshKnockedOutMarker(
                markerVisual,
                character.IsKnockedOut());
        }
    }

    private void UpdateTreasureMarkers()
    {
        if (m_displayedMazeData == null)
        {
            return;
        }

        foreach (KeyValuePair<TreasureChest, RectTransform>
                 pair in m_treasureMarkers)
        {
            TreasureChest treasureChest = pair.Key;
            RectTransform marker = pair.Value;

            if (treasureChest == null || marker == null)
            {
                continue;
            }

            if (treasureChest.IsExported)
            {
                marker.gameObject.SetActive(false);
                continue;
            }

            bool isInsideMaze;

            if (m_useWorldPositionForMarkers)
            {
                // 所持・ドロップ・ノックバック後も、
                // 宝箱自身の実際のTransform位置を使う。
                isInsideMaze = TrySetMarkerToWorldPosition(
                    marker,
                    treasureChest.transform.position);
            }
            else
            {
                Vector2Int cellPosition;

                isInsideMaze =
                    m_displayedMazeData.TryWorldToCell(
                        treasureChest.transform.position,
                        out cellPosition);

                if (isInsideMaze)
                {
                    SetMarkerToCell(marker, cellPosition);
                }
            }

            marker.gameObject.SetActive(isInsideMaze);

            if (!isInsideMaze)
            {
                continue;
            }

            bool isCentralChest =
                treasureChest.TreasureType
                == TreasureType.CentralChest;

            float markerRatio =
                m_smallTreasureMarkerCellRatio;

            if (isCentralChest)
            {
                markerRatio *= m_centralTreasureScale;
            }

            SetMarkerSize(marker, markerRatio);
        }
    }

    private void SetMarkerToCell(
        RectTransform marker,
        Vector2Int cellPosition)
    {
        float normalizedX =
            (cellPosition.x + 0.5f)
            / m_displayedMazeData.m_width;

        float normalizedY =
            (cellPosition.y + 0.5f)
            / m_displayedMazeData.m_height;

        marker.anchorMin = new Vector2(
            normalizedX,
            normalizedY);

        marker.anchorMax = marker.anchorMin;
        marker.anchoredPosition = Vector2.zero;
    }


    private bool TrySetMarkerToWorldPosition(
    RectTransform marker,
    Vector3 worldPosition)
    {
        if (marker == null
            || m_displayedMazeData == null)
        {
            return false;
        }

        Vector2 normalizedPosition;

        if (!TryWorldPositionToNormalizedPosition(
            worldPosition,
            out normalizedPosition))
        {
            return false;
        }

        marker.anchorMin = normalizedPosition;
        marker.anchorMax = normalizedPosition;
        marker.anchoredPosition = Vector2.zero;

        return true;
    }

    public bool TryWorldPositionToNormalizedPosition(
        Vector3 worldPosition,
        out Vector2 normalizedPosition)
    {
        normalizedPosition = Vector2.zero;

        if (m_displayedMazeData == null
            || m_displayedMazeData.m_width <= 0
            || m_displayedMazeData.m_height <= 0)
        {
            return false;
        }

        Vector3 minimumCellWorld =
            m_displayedMazeData.CellToWorld(0, 0);

        Vector3 maximumCellWorld =
            m_displayedMazeData.CellToWorld(
                m_displayedMazeData.m_width - 1,
                m_displayedMazeData.m_height - 1);

        float halfCellSize =
            m_displayedMazeData.CellSize * 0.5f;

        float minimumWorldX =
            Mathf.Min(
                minimumCellWorld.x,
                maximumCellWorld.x)
            - halfCellSize;

        float maximumWorldX =
            Mathf.Max(
                minimumCellWorld.x,
                maximumCellWorld.x)
            + halfCellSize;

        float minimumWorldZ =
            Mathf.Min(
                minimumCellWorld.z,
                maximumCellWorld.z)
            - halfCellSize;

        float maximumWorldZ =
            Mathf.Max(
                minimumCellWorld.z,
                maximumCellWorld.z)
            + halfCellSize;

        if (worldPosition.x < minimumWorldX
            || worldPosition.x > maximumWorldX
            || worldPosition.z < minimumWorldZ
            || worldPosition.z > maximumWorldZ)
        {
            return false;
        }

        float normalizedX = Mathf.InverseLerp(
            minimumWorldX,
            maximumWorldX,
            worldPosition.x);

        float normalizedY = Mathf.InverseLerp(
            minimumWorldZ,
            maximumWorldZ,
            worldPosition.z);

        normalizedPosition = new Vector2(
            normalizedX,
            normalizedY);

        return true;
    }




    private void SetMarkerSize(
        RectTransform marker,
        float cellRatio)
    {
        if (m_squareArea == null
            || m_displayedMazeData == null)
        {
            return;
        }

        float cellWidth =
            m_squareArea.rect.width
            / m_displayedMazeData.m_width;

        float cellHeight =
            m_squareArea.rect.height
            / m_displayedMazeData.m_height;

        float markerSize =
            Mathf.Min(cellWidth, cellHeight)
            * cellRatio;

        marker.sizeDelta = new Vector2(
            markerSize,
            markerSize);
    }

    private void RefreshSquareArea()
    {
        if (m_squareArea == null)
        {
            return;
        }

        RectTransform rootRectTransform =
            transform as RectTransform;

        if (rootRectTransform == null)
        {
            return;
        }

        float squareSize = Mathf.Min(
            rootRectTransform.rect.width,
            rootRectTransform.rect.height);

        m_squareArea.anchorMin =
            new Vector2(0.5f, 0.5f);

        m_squareArea.anchorMax =
            new Vector2(0.5f, 0.5f);

        m_squareArea.pivot =
            new Vector2(0.5f, 0.5f);

        m_squareArea.sizeDelta =
            new Vector2(squareSize, squareSize);

        m_squareArea.anchoredPosition =
            Vector2.zero;
    }

    private Image CreateImage(
        string objectName,
        Transform parent,
        Sprite sprite)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;

        return image;
    }

    private void ClearChildren(RectTransform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void CreateRuntimeSprites()
    {
        if (m_squareSprite == null)
        {
            m_squareTexture = new Texture2D(
                2,
                2,
                TextureFormat.RGBA32,
                false);

            m_squareTexture.SetPixels(new[]
            {
                Color.white,
                Color.white,
                Color.white,
                Color.white,
            });

            m_squareTexture.Apply();

            m_squareSprite = Sprite.Create(
                m_squareTexture,
                new Rect(0.0f, 0.0f, 2.0f, 2.0f),
                new Vector2(0.5f, 0.5f));
        }

        if (m_circleSprite == null)
        {
            const int textureSize = 32;

            m_circleTexture = new Texture2D(
                textureSize,
                textureSize,
                TextureFormat.RGBA32,
                false);

            Color[] pixels =
                new Color[textureSize * textureSize];

            float radius = textureSize * 0.5f;
            Vector2 center = new Vector2(
                radius - 0.5f,
                radius - 0.5f);

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float distance = Vector2.Distance(
                        new Vector2(x, y),
                        center);

                    pixels[y * textureSize + x] =
                        distance <= radius
                            ? Color.white
                            : Color.clear;
                }
            }

            m_circleTexture.SetPixels(pixels);
            m_circleTexture.Apply();

            m_circleSprite = Sprite.Create(
                m_circleTexture,
                new Rect(
                    0.0f,
                    0.0f,
                    textureSize,
                    textureSize),
                new Vector2(0.5f, 0.5f));
        }
    }

    private void ReleaseRuntimeSprites()
    {
        if (m_squareSprite != null)
        {
            Destroy(m_squareSprite);
            m_squareSprite = null;
        }

        if (m_circleSprite != null)
        {
            Destroy(m_circleSprite);
            m_circleSprite = null;
        }

        if (m_squareTexture != null)
        {
            Destroy(m_squareTexture);
            m_squareTexture = null;
        }

        if (m_circleTexture != null)
        {
            Destroy(m_circleTexture);
            m_circleTexture = null;
        }
    }

    private void RefreshKnockedOutMarker(
        CharacterMarkerVisual markerVisual,
        bool isKnockedOut)
    {
        if (markerVisual == null
            || markerVisual.m_rootRectTransform == null
            || markerVisual.m_knockedOutCrossObject == null)
        {
            return;
        }

        GameObject crossObject =
            markerVisual.m_knockedOutCrossObject;

        if (crossObject.activeSelf != isKnockedOut)
        {
            crossObject.SetActive(isKnockedOut);
        }

        if (!isKnockedOut)
        {
            return;
        }

        RectTransform markerRectTransform =
            markerVisual.m_rootRectTransform;

        float markerSize = Mathf.Min(
            markerRectTransform.rect.width,
            markerRectTransform.rect.height);

        RectTransform crossRootRectTransform =
            crossObject.GetComponent<RectTransform>();

        if (crossRootRectTransform == null)
        {
            return;
        }

        for (int i = 0;
             i < crossRootRectTransform.childCount;
             i++)
        {
            RectTransform lineRectTransform =
                crossRootRectTransform.GetChild(i)
                    as RectTransform;

            if (lineRectTransform == null)
            {
                continue;
            }

            lineRectTransform.sizeDelta =
                new Vector2(
                    markerSize * 1.00f,
                    markerSize
                        * m_knockedOutCrossThicknessRatio);
        }
    }


    public bool TryGetWorldPositionOnMap(
        Vector3 worldPosition,
        out Vector3 mapWorldPosition)
    {
        mapWorldPosition = Vector3.zero;

        if (m_squareArea == null)
        {
            return false;
        }

        Vector2 normalizedPosition;

        if (!TryWorldPositionToNormalizedPosition(
                worldPosition,
                out normalizedPosition))
        {
            return false;
        }

        Rect squareRect = m_squareArea.rect;

        Vector3 localPosition = new Vector3(
            Mathf.Lerp(
                squareRect.xMin,
                squareRect.xMax,
                normalizedPosition.x),
            Mathf.Lerp(
                squareRect.yMin,
                squareRect.yMax,
                normalizedPosition.y),
            0.0f);

        mapWorldPosition =
            m_squareArea.TransformPoint(localPosition);

        return true;
    }


}