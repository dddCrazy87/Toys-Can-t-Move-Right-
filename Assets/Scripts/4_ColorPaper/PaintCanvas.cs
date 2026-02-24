using UnityEngine;
using System.Collections.Generic;

public class PaintCanvas : MonoBehaviour
{
    [Header("畫布設定")]
    [SerializeField] private int textureSize = 2048;
    [SerializeField] private MeshRenderer canvasRenderer;  // 畫布平面的 Renderer

    [Header("畫布範圍（自動從 Quad 獲取）")]
    [SerializeField] private bool autoDetectBounds = true;  // 自動從 Quad 獲取範圍
    [SerializeField] private Vector2 canvasMin = new Vector2(-21.6f, -8.1f);  // X, Z 最小值（手動設定時使用）
    [SerializeField] private Vector2 canvasMax = new Vector2(17.6f, 19.5f);   // X, Z 最大值（手動設定時使用）

    [Header("筆刷設定")]
    [SerializeField] private float brushSize = 0.05f;  // 筆刷大小（UV 空間，0-1）
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

        // 自動從 Quad 獲取範圍
        if (autoDetectBounds && canvasRenderer != null)
        {
            DetectBoundsFromQuad();
        }

        InitializeCanvas();
    }

    /// <summary>
    /// 從 Quad 自動獲取畫布範圍
    /// </summary>
    void DetectBoundsFromQuad()
    {
        // 獲取 Quad 的四個角的世界座標
        MeshFilter meshFilter = canvasRenderer.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null) return;

        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;

        // 找出所有頂點的世界座標
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (Vector3 vertex in vertices)
        {
            Vector3 worldPos = canvasRenderer.transform.TransformPoint(vertex);
            minX = Mathf.Min(minX, worldPos.x);
            maxX = Mathf.Max(maxX, worldPos.x);
            minZ = Mathf.Min(minZ, worldPos.z);
            maxZ = Mathf.Max(maxZ, worldPos.z);
        }

        canvasMin = new Vector2(minX, minZ);
        canvasMax = new Vector2(maxX, maxZ);

        Debug.Log($"[PaintCanvas] 自動檢測畫布範圍: Min({canvasMin.x:F2}, {canvasMin.y:F2}) Max({canvasMax.x:F2}, {canvasMax.y:F2})");
    }

    void InitializeCanvas()
    {
        // 創建 Render Texture
        paintTexture = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32);
        paintTexture.filterMode = FilterMode.Bilinear;
        paintTexture.Create();

        // 清空為透明
        ClearCanvas();

        // 設定畫布材質（使用透明 Shader）
        if (canvasRenderer != null)
        {
            // 使用支援透明的 Unlit Shader
            canvasMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            canvasMaterial.mainTexture = paintTexture;

            // 設定為透明模式
            canvasMaterial.SetFloat("_Surface", 1); // 1 = Transparent
            canvasMaterial.SetFloat("_Blend", 0);   // 0 = Alpha
            canvasMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            canvasMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            canvasMaterial.SetInt("_ZWrite", 0);
            canvasMaterial.DisableKeyword("_ALPHATEST_ON");
            canvasMaterial.EnableKeyword("_ALPHABLEND_ON");
            canvasMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            canvasMaterial.renderQueue = 3000; // Transparent queue

            canvasRenderer.material = canvasMaterial;
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
        if (canvasRenderer != null)
        {
            // 將世界座標轉換為 Quad 的本地座標
            Vector3 localPos = canvasRenderer.transform.InverseTransformPoint(worldPos);

            // Quad 的本地座標範圍通常是 -0.5 到 0.5
            // 轉換為 UV (0 到 1)
            float u = localPos.x + 0.5f;
            float v = localPos.y + 0.5f;

            return new Vector2(u, v);
        }
        else
        {
            // Fallback：使用手動設定的範圍
            float u = Mathf.InverseLerp(canvasMin.x, canvasMax.x, worldPos.x);
            float v = Mathf.InverseLerp(canvasMin.y, canvasMax.y, worldPos.z);
            return new Vector2(u, v);
        }
    }

    /// <summary>
    /// 在指定位置畫圓（加入佇列，稍後批次處理）
    /// </summary>
    public void Paint(Vector3 worldPos, string playerColor)
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

        // 加入待處理列表（使用預設筆刷大小）
        pendingPaints.Add(new PaintCommand { uv = uv, color = color, brushSize = 0 });
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
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector3 pos = Vector3.Lerp(fromPos, toPos, t);
            Paint(pos, playerColor);
        }
    }

    /// <summary>
    /// 大範圍噴灑顏料（顏料罐爆炸效果）
    /// </summary>
    public void PaintExplosion(Vector3 center, string playerColor, float radius, float explosionBrushSize, int density)
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

            // 加入待處理列表（使用較大的筆刷）
            pendingPaints.Add(new PaintCommand { uv = uv, color = color, brushSize = randomSize });
        }

        // 中心點畫一個較大的圓
        Vector2 centerUV = WorldToUV(center);
        if (centerUV.x >= 0 && centerUV.x <= 1 && centerUV.y >= 0 && centerUV.y <= 1)
        {
            pendingPaints.Add(new PaintCommand { uv = centerUV, color = color, brushSize = explosionBrushSize * 1.5f });
        }
    }

    /// <summary>
    /// 設定筆刷大小
    /// </summary>
    public void SetBrushSize(float size)
    {
        brushSize = size;
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
