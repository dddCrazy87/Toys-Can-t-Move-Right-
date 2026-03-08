using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class PaintCanvas : MonoBehaviour
{
    [Header("畫布設定")]
    [SerializeField] private int textureWidth = 2048;  // 貼圖寬度（會根據畫布比例自動計算高度）
    [SerializeField] private bool autoCalculateHeight = true;  // 自動根據畫布比例計算高度
    [SerializeField] private DecalProjector decalProjector;  // Decal 投影器

    [Header("畫布範圍（從 Decal Projector 自動獲取）")]
    [SerializeField] private Vector2 canvasMin = new Vector2(-21.6f, -8.1f);  // X, Z 最小值
    [SerializeField] private Vector2 canvasMax = new Vector2(17.6f, 19.5f);   // X, Z 最大值

    // 實際的貼圖尺寸
    private int actualTextureWidth;
    private int actualTextureHeight;

    [Header("筆刷設定")]
    [SerializeField] private float brushSize = 0.12f;  // 筆刷大小（UV 空間，0-1）
    [SerializeField] private Material paintMaterial;   // 畫圓的材質

    [Header("顏色設定")]
    [SerializeField] private Color blueColor = new Color(0.2f, 0.4f, 1f, 1f);
    [SerializeField] private Color yellowColor = new Color(1f, 0.9f, 0.2f, 1f);
    [SerializeField] private Color greenColor = new Color(0.2f, 0.8f, 0.3f, 1f);
    [SerializeField] private Color redColor = new Color(1f, 0.3f, 0.3f, 1f);

    private RenderTexture paintTexture;
    private Material canvasMaterial;
    private Dictionary<string, Color> colorMapping;

    // 批次處理用的列表
    private List<PaintCommand> pendingPaints = new List<PaintCommand>();

    private struct PaintCommand
    {
        public Vector2 uv;
        public Color color;
        public float brushSize;  // 0 表示使用預設大小
        public Texture2D brushTexture;  // null 表示使用圓形筆刷
    }

    void Awake()
    {
        // 顏色對應
        colorMapping = new Dictionary<string, Color>
        {
            { "blue", blueColor },
            { "yellow", yellowColor },
            { "green", greenColor },
            { "red", redColor }
        };

        // 從 Decal Projector 獲取範圍
        if (decalProjector != null)
        {
            DetectBoundsFromDecal();
        }

        InitializeCanvas();
    }

    /// <summary>
    /// 從 Decal Projector 獲取畫布範圍
    /// </summary>
    void DetectBoundsFromDecal()
    {
        Vector3 pos = decalProjector.transform.position;
        float halfWidth = decalProjector.size.x / 2f;
        float halfHeight = decalProjector.size.y / 2f;

        canvasMin = new Vector2(pos.x - halfWidth, pos.z - halfHeight);
        canvasMax = new Vector2(pos.x + halfWidth, pos.z + halfHeight);

        Debug.Log($"[PaintCanvas] 從 Decal 檢測畫布範圍: Min({canvasMin.x:F2}, {canvasMin.y:F2}) Max({canvasMax.x:F2}, {canvasMax.y:F2})");
    }

    /// <summary>
    /// 根據畫布比例計算貼圖尺寸
    /// </summary>
    void CalculateTextureDimensions()
    {
        actualTextureWidth = textureWidth;

        if (autoCalculateHeight)
        {
            // 計算畫布的寬高
            float canvasWidth = canvasMax.x - canvasMin.x;
            float canvasHeight = canvasMax.y - canvasMin.y;

            // 根據比例計算貼圖高度
            float aspectRatio = canvasHeight / canvasWidth;
            actualTextureHeight = Mathf.RoundToInt(textureWidth * aspectRatio);

            // 確保是 4 的倍數（GPU 友善）
            actualTextureHeight = (actualTextureHeight / 4) * 4;

            Debug.Log($"[PaintCanvas] 畫布比例: {canvasWidth:F1} x {canvasHeight:F1} (比例 {aspectRatio:F2})");
        }
        else
        {
            actualTextureHeight = textureWidth;  // 正方形
        }
    }

    void InitializeCanvas()
    {
        // 計算貼圖尺寸（根據畫布比例）
        CalculateTextureDimensions();

        // 創建 Render Texture（使用計算後的尺寸）
        paintTexture = new RenderTexture(actualTextureWidth, actualTextureHeight, 0, RenderTextureFormat.ARGB32);
        paintTexture.filterMode = FilterMode.Bilinear;
        paintTexture.Create();

        Debug.Log($"[PaintCanvas] RenderTexture 尺寸: {actualTextureWidth} x {actualTextureHeight}");

        // 設定筆刷材質的長寬比（確保圓形不變形）
        if (paintMaterial != null)
        {
            float aspectRatio = (float)actualTextureWidth / actualTextureHeight;
            paintMaterial.SetFloat("_AspectRatio", aspectRatio);
            Debug.Log($"[PaintCanvas] 筆刷長寬比: {aspectRatio:F2}");
        }

        // 清空為透明
        ClearCanvas();

        // 設定 Decal 材質
        if (decalProjector != null)
        {
            // 使用 Decal Shader
            Shader decalShader = Shader.Find("Shader Graphs/Decal");
            if (decalShader == null)
            {
                Debug.LogError("[PaintCanvas] 找不到 Shader Graphs/Decal！請確認 URP Decal 已啟用。");
                return;
            }

            canvasMaterial = new Material(decalShader);
            canvasMaterial.SetTexture("Base_Map", paintTexture);

            // 設定較低的 Render Queue，讓顏料顯示在底圖線稿下面
            canvasMaterial.renderQueue = 2900;

            decalProjector.material = canvasMaterial;
            Debug.Log("[PaintCanvas] Decal 材質初始化成功");
        }
        else
        {
            Debug.LogWarning("[PaintCanvas] Decal Projector 未設定！");
        }
    }

    /// <summary>
    /// 清空畫布
    /// </summary>
    public void ClearCanvas()
    {
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = paintTexture;
        GL.Clear(true, true, new Color(0, 0, 0, 0));
        RenderTexture.active = currentRT;
    }

    /// <summary>
    /// 世界座標轉換為 UV 座標
    /// </summary>
    public Vector2 WorldToUV(Vector3 worldPos)
    {
        // 使用畫布範圍計算 UV
        float u = Mathf.InverseLerp(canvasMin.x, canvasMax.x, worldPos.x);
        float v = Mathf.InverseLerp(canvasMin.y, canvasMax.y, worldPos.z);
        return new Vector2(u, v);
    }

    /// <summary>
    /// 在指定位置畫圓（加入佇列，稀後批次處理）
    /// </summary>
    public void Paint(Vector3 worldPos, string playerColor)
    {
        Paint(worldPos, playerColor, 0, null);  // 使用預設筆刷大小，無貼圖
    }

    /// <summary>
    /// 在指定位置畫圓（自訂筆刷大小）
    /// </summary>
    public void Paint(Vector3 worldPos, string playerColor, float customBrushSize)
    {
        Paint(worldPos, playerColor, customBrushSize, null);  // 無貼圖
    }

    /// <summary>
    /// 在指定位置畫圓（自訂筆刷大小 + 筆刷貼圖）
    /// </summary>
    public void Paint(Vector3 worldPos, string playerColor, float customBrushSize, Texture2D brushTex)
    {
        if (paintMaterial == null || paintTexture == null) return;

        Vector2 uv = WorldToUV(worldPos);

        // 檢查是否在畫布範圍內
        if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) return;

        // 取得顏色
        if (!colorMapping.TryGetValue(playerColor, out Color color))
        {
            color = Color.white;
        }

        // 加入待處理列表
        pendingPaints.Add(new PaintCommand { uv = uv, color = color, brushSize = customBrushSize, brushTexture = brushTex });
    }

    /// <summary>
    /// 取得預設筆刷大小
    /// </summary>
    public float GetDefaultBrushSize()
    {
        return brushSize;
    }

    void LateUpdate()
    {
        // 批次處理所有待畫的點
        if (pendingPaints.Count == 0) return;

        // 只做一次 Blit，畫所有點
        RenderTexture tempRT = RenderTexture.GetTemporary(paintTexture.width, paintTexture.height);

        foreach (var cmd in pendingPaints)
        {
            paintMaterial.SetVector("_BrushPos", new Vector4(cmd.uv.x, cmd.uv.y, 0, 0));
            // 使用自訂筆刷大小，若為 0 則使用預設值
            float size = cmd.brushSize > 0 ? cmd.brushSize : brushSize;
            paintMaterial.SetFloat("_BrushSize", size);
            paintMaterial.SetColor("_BrushColor", cmd.color);

            // 設定筆刷貼圖
            if (cmd.brushTexture != null)
            {
                paintMaterial.SetTexture("_BrushTex", cmd.brushTexture);
                paintMaterial.SetFloat("_UseBrushTexture", 1f);
            }
            else
            {
                paintMaterial.SetFloat("_UseBrushTexture", 0f);
            }

            Graphics.Blit(paintTexture, tempRT);
            Graphics.Blit(tempRT, paintTexture, paintMaterial);
        }

        RenderTexture.ReleaseTemporary(tempRT);
        pendingPaints.Clear();
    }

    /// <summary>
    /// 連續畫線（從上一個位置到當前位置）
    /// </summary>
    public void PaintLine(Vector3 fromPos, Vector3 toPos, string playerColor, int segments = 5)
    {
        PaintLine(fromPos, toPos, playerColor, 0, null, segments);  // 使用預設筆刷大小
    }

    /// <summary>
    /// 連續畫線（自訂筆刷大小）
    /// </summary>
    public void PaintLine(Vector3 fromPos, Vector3 toPos, string playerColor, float customBrushSize, int segments = 5)
    {
        PaintLine(fromPos, toPos, playerColor, customBrushSize, null, segments);
    }

    /// <summary>
    /// 連續畫線（自訂筆刷大小 + 筆刷貼圖）
    /// </summary>
    public void PaintLine(Vector3 fromPos, Vector3 toPos, string playerColor, float customBrushSize, Texture2D brushTex, int segments = 5)
    {
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector3 pos = Vector3.Lerp(fromPos, toPos, t);
            Paint(pos, playerColor, customBrushSize, brushTex);
        }
    }

    /// <summary>
    /// 大範圍噴灑顏料（顏料罐爆炸效果）- 使用圓形筆刷
    /// </summary>
    public void PaintExplosion(Vector3 center, string playerColor, float radius, float explosionBrushSize, int density)
    {
        PaintExplosion(center, playerColor, radius, explosionBrushSize, density, null);
    }

    /// <summary>
    /// 大範圍噴灑顏料（顏料罐爆炸效果）- 使用自訂筆刷貼圖
    /// </summary>
    public void PaintExplosion(Vector3 center, string playerColor, float radius, float explosionBrushSize, int density, Texture2D brushTex)
    {
        if (paintMaterial == null || paintTexture == null) return;

        // 取得顏色
        if (!colorMapping.TryGetValue(playerColor, out Color color))
        {
            color = Color.white;
        }

        // 在圓形範圍內隨機噴灑多個點
        for (int i = 0; i < density; i++)
        {
            // 隨機角度和距離（使用平方根讓分布更均勻）
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Mathf.Sqrt(Random.Range(0f, 1f)) * radius;

            // 計算世界座標
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * distance,
                0f,
                Mathf.Sin(angle) * distance
            );
            Vector3 worldPos = center + offset;

            // 轉換為 UV
            Vector2 uv = WorldToUV(worldPos);

            // 檢查是否在畫布範圍內
            if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) continue;

            // 隨機變化筆刷大小，製造更自然的效果
            float randomSize = explosionBrushSize * Random.Range(0.7f, 1.3f);

            // 加入待處理列表（使用角色的筆刷貼圖）
            pendingPaints.Add(new PaintCommand { uv = uv, color = color, brushSize = randomSize, brushTexture = brushTex });
        }

        // 中心點畫一個較大的圓
        Vector2 centerUV = WorldToUV(center);
        if (centerUV.x >= 0 && centerUV.x <= 1 && centerUV.y >= 0 && centerUV.y <= 1)
        {
            pendingPaints.Add(new PaintCommand { uv = centerUV, color = color, brushSize = explosionBrushSize * 1.5f, brushTexture = brushTex });
        }
    }

    /// <summary>
    /// 設定筆刷大小
    /// </summary>
    public void SetBrushSize(float size)
    {
        brushSize = size;
    }

    /// <summary>
    /// 取得實際貼圖尺寸（用於製作底圖）
    /// </summary>
    public Vector2Int GetTextureDimensions()
    {
        return new Vector2Int(actualTextureWidth, actualTextureHeight);
    }

    void OnDestroy()
    {
        if (paintTexture != null)
        {
            paintTexture.Release();
            Destroy(paintTexture);
        }
        if (canvasMaterial != null)
        {
            Destroy(canvasMaterial);
        }
    }
}
