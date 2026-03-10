using System.Collections.Generic;
using UnityEngine;

public class ColorGrid : MonoBehaviour
{
    [Header("畫布範圍設定")]
    [SerializeField] private Vector2 gridMin = new Vector2(-32f, -29f);  // X, Z 最小值
    [SerializeField] private Vector2 gridMax = new Vector2(40f, 20f);    // X, Z 最大值
    [SerializeField] private float cellSize = 2f;                         // 每個格子的大小
    [SerializeField] private float gridHeight = -34.5f;                   // Y 高度

    [Header("顏色設定")]
    [SerializeField] private Color blueColor = new Color(0.2f, 0.4f, 1f, 1f);
    [SerializeField] private Color yellowColor = new Color(1f, 0.9f, 0.2f, 1f);
    [SerializeField] private Color greenColor = new Color(0.2f, 0.8f, 0.3f, 1f);
    [SerializeField] private Color redColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Header("視覺設定")]
    [SerializeField] private GameObject cellPrefab;  // 可選：自訂格子預製體
    [SerializeField] private float cellHeightOffset = 0.01f;  // 格子高度偏移，避免 Z-fighting
    [SerializeField] private bool hideGridVisuals = true;  // 隱藏格子視覺（使用 Trail 時開啟）

    private int gridWidth;
    private int gridDepth;
    private int[,] gridOwnership;  // -1 = 無人, 0~3 = 玩家索引
    private GameObject[,] cellObjects;
    private Material[,] cellMaterials;

    private bool isColoringEnabled = false;
    private GameManager gameManager;
    private Dictionary<string, Color> colorMapping;

    void Awake()
    {
        // 計算格子數量
        gridWidth = Mathf.CeilToInt((gridMax.x - gridMin.x) / cellSize);
        gridDepth = Mathf.CeilToInt((gridMax.y - gridMin.y) / cellSize);

        gridOwnership = new int[gridWidth, gridDepth];
        cellObjects = new GameObject[gridWidth, gridDepth];
        cellMaterials = new Material[gridWidth, gridDepth];

        // 初始化為無人擁有
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridDepth; z++)
            {
                gridOwnership[x, z] = -1;
            }
        }

        // 顏色對應
        colorMapping = new Dictionary<string, Color>
        {
            { "blue", blueColor },
            { "yellow", yellowColor },
            { "green", greenColor },
            { "red", redColor }
        };

        // 創建格子視覺物件
        CreateGridCells();
    }

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
    }

    void CreateGridCells()
    {
        // 如果隱藏格子視覺，就不創建格子物件（只保留計分邏輯）
        if (hideGridVisuals) return;

        // 創建一個父物件來整理格子
        GameObject gridParent = new GameObject("ColorGridCells");
        gridParent.transform.SetParent(transform);

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridDepth; z++)
            {
                Vector3 worldPos = GridToWorld(x, z);

                GameObject cell;
                if (cellPrefab != null)
                {
                    cell = Instantiate(cellPrefab, worldPos, Quaternion.identity, gridParent.transform);
                }
                else
                {
                    // 創建預設的平面格子
                    cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    cell.transform.position = worldPos;
                    cell.transform.rotation = Quaternion.Euler(90f, 0f, 0f);  // 讓 Quad 朝上
                    cell.transform.localScale = new Vector3(cellSize, cellSize, 1f);
                    cell.transform.SetParent(gridParent.transform);

                    // 移除碰撞器（不需要）
                    Destroy(cell.GetComponent<Collider>());
                }

                // 設定材質
                Renderer renderer = cell.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // 關閉陰影
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;

                    // 創建獨立材質實例（使用 Unlit 避免光照影響）
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    mat.color = new Color(1f, 1f, 1f, 0f);  // 初始透明
                    renderer.material = mat;
                    cellMaterials[x, z] = mat;
                }

                cell.name = $"Cell_{x}_{z}";
                cellObjects[x, z] = cell;
                cell.SetActive(false);  // 初始隱藏，填色時才顯示
            }
        }
    }

    Vector3 GridToWorld(int gridX, int gridZ)
    {
        float worldX = gridMin.x + (gridX + 0.5f) * cellSize;
        float worldZ = gridMin.y + (gridZ + 0.5f) * cellSize;  // gridMin.y 實際是 Z 軸
        return new Vector3(worldX, gridHeight + cellHeightOffset, worldZ);
    }

    (int x, int z) WorldToGrid(Vector3 worldPos)
    {
        int gridX = Mathf.FloorToInt((worldPos.x - gridMin.x) / cellSize);
        int gridZ = Mathf.FloorToInt((worldPos.z - gridMin.y) / cellSize);
        return (gridX, gridZ);
    }

    bool IsValidGrid(int x, int z)
    {
        return x >= 0 && x < gridWidth && z >= 0 && z < gridDepth;
    }

    public void EnableColoring()
    {
        isColoringEnabled = true;
    }

    public void DisableColoring()
    {
        isColoringEnabled = false;
    }

    void Update()
    {
        if (!isColoringEnabled || gameManager == null) return;

        // 遍歷所有玩家，檢查他們的位置並填色
        foreach (var kvp in gameManager.playerControllers)
        {
            int playerIndex = kvp.Key;
            PlayerController player = kvp.Value;

            if (player == null) continue;

            // 筆畫位置往角色後方偏移
            Vector3 playerPos = player.transform.position - player.transform.forward * 0.5f;
            PaintAtPosition(playerPos, playerIndex, player.playerColor);
        }
    }

    void PaintAtPosition(Vector3 worldPos, int playerIndex, string playerColor)
    {
        var (gridX, gridZ) = WorldToGrid(worldPos);

        if (!IsValidGrid(gridX, gridZ)) return;

        // 檢查是否已經是這個玩家的顏色
        if (gridOwnership[gridX, gridZ] == playerIndex) return;

        // 填色！
        gridOwnership[gridX, gridZ] = playerIndex;

        // 更新視覺（如果沒有隱藏格子）
        if (!hideGridVisuals && cellObjects[gridX, gridZ] != null)
        {
            cellObjects[gridX, gridZ].SetActive(true);

            if (cellMaterials[gridX, gridZ] != null && colorMapping.TryGetValue(playerColor, out Color color))
            {
                cellMaterials[gridX, gridZ].color = color;
            }
        }
    }

    /// <summary>
    /// 擦除指定位置的格子（清除所有權）
    /// </summary>
    public void EraseAtPosition(Vector3 worldPos)
    {
        var (gridX, gridZ) = WorldToGrid(worldPos);

        if (!IsValidGrid(gridX, gridZ)) return;

        // 已經是空的就不用擦
        if (gridOwnership[gridX, gridZ] == -1) return;

        // 清除所有權
        gridOwnership[gridX, gridZ] = -1;

        // 更新視覺（如果沒有隱藏格子）
        if (!hideGridVisuals && cellObjects[gridX, gridZ] != null)
        {
            cellObjects[gridX, gridZ].SetActive(false);
        }
    }

    /// <summary>
    /// 取得每個玩家的分數（絕對覆蓋率，基於整張畫布）
    /// </summary>
    public Dictionary<int, int> GetPlayerScores()
    {
        Dictionary<int, int> cellCounts = new Dictionary<int, int>();
        int totalCells = gridWidth * gridDepth;  // 整張畫布的格子數

        // 計算每個玩家的格子數
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridDepth; z++)
            {
                int owner = gridOwnership[x, z];
                if (owner >= 0)
                {
                    if (!cellCounts.ContainsKey(owner))
                        cellCounts[owner] = 0;
                    cellCounts[owner]++;
                }
            }
        }

        // 轉換成絕對覆蓋率（基於整張畫布）
        Dictionary<int, int> scores = new Dictionary<int, int>();
        foreach (var kvp in cellCounts)
        {
            // 四捨五入到整數百分比
            scores[kvp.Key] = Mathf.RoundToInt((float)kvp.Value / totalCells * 100f);
        }

        return scores;
    }

    /// <summary>
    /// 取得總覆蓋率（所有玩家加總）
    /// </summary>
    public int GetTotalCoverage()
    {
        int totalCells = gridWidth * gridDepth;
        int ownedCells = 0;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridDepth; z++)
            {
                if (gridOwnership[x, z] >= 0)
                    ownedCells++;
            }
        }

        return Mathf.RoundToInt((float)ownedCells / totalCells * 100f);
    }

    /// <summary>
    /// 取得總格子數
    /// </summary>
    public int GetTotalCells()
    {
        return gridWidth * gridDepth;
    }
}
