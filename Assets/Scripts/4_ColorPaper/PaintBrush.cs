using UnityEngine;

public class PaintBrush : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private float minMoveDistance = 0.08f;  // 最小移動距離才畫（越小越密集，筆觸重疊）
    [SerializeField] private float backwardOffset = 0.5f;   // 往角色後方偏移

    [Header("筆刷大小調整（根據角色能力）")]
    [SerializeField] private float minBrushMultiplier = 1.8f;   // 最快角色的筆刷倍率
    [SerializeField] private float maxBrushMultiplier = 3.0f;   // 最慢角色的筆刷倍率
    [SerializeField] private float minSpeed = 8f;   // 最慢角色速度
    [SerializeField] private float maxSpeed = 20f;  // 最快角色速度

    private PaintCanvas paintCanvas;
    private string playerColor;
    private Vector3 lastPaintPos;
    private bool isInitialized = false;
    private bool isPainting = false;
    private float characterBrushSize;  // 角色專屬筆刷大小
    private Texture2D characterBrushTexture;  // 角色專屬筆刷貼圖

    /// <summary>
    /// 初始化筆刷
    /// </summary>
    public void Initialize(PaintCanvas canvas, string color)
    {
        paintCanvas = canvas;
        playerColor = color;
        lastPaintPos = GetPaintPosition();

        // 根據角色速度計算筆刷大小
        CalculateBrushSize();

        isInitialized = true;
    }

    /// <summary>
    /// 根據角色速度計算筆刷大小（速度快=筆刷細，速度慢=筆刷粗）
    /// </summary>
    private void CalculateBrushSize()
    {
        PlayerController player = GetComponent<PlayerController>();
        float defaultSize = paintCanvas != null ? paintCanvas.GetDefaultBrushSize() : 0.05f;

        if (player != null)
        {
            // 速度越快，筆刷越細；速度越慢，筆刷越粗
            float speedNormalized = Mathf.InverseLerp(minSpeed, maxSpeed, player.moveSpeed);
            float multiplier = Mathf.Lerp(maxBrushMultiplier, minBrushMultiplier, speedNormalized);
            characterBrushSize = defaultSize * multiplier;

            // 取得角色專屬筆刷貼圖
            characterBrushTexture = player.brushTexture;

            string brushName = characterBrushTexture != null ? characterBrushTexture.name : "圓形";
            Debug.Log($"[PaintBrush] {player.playerName} 速度:{player.moveSpeed} 筆刷:{brushName} 大小:{characterBrushSize:F4}");
        }
        else
        {
            characterBrushSize = defaultSize;
            characterBrushTexture = null;
        }
    }

    /// <summary>
    /// 取得繪畫位置（角色位置 + 往後偏移）
    /// </summary>
    private Vector3 GetPaintPosition()
    {
        return transform.position - transform.forward * backwardOffset;
    }

    /// <summary>
    /// 開始畫圖
    /// </summary>
    public void StartPainting()
    {
        if (!isInitialized) return;
        isPainting = true;
        lastPaintPos = GetPaintPosition();

        // 畫第一個點（使用角色專屬筆刷大小和貼圖）
        paintCanvas.Paint(lastPaintPos, playerColor, characterBrushSize, characterBrushTexture);
    }

    /// <summary>
    /// 停止畫圖
    /// </summary>
    public void StopPainting()
    {
        isPainting = false;
    }

    void Update()
    {
        if (!isInitialized || !isPainting || paintCanvas == null) return;

        Vector3 currentPos = GetPaintPosition();
        float distance = Vector3.Distance(currentPos, lastPaintPos);

        // 移動超過最小距離才畫線
        if (distance >= minMoveDistance)
        {
            // 畫一條線從上一個位置到當前位置（使用角色專屬筆刷大小和貼圖）
            int segments = Mathf.CeilToInt(distance / minMoveDistance);
            paintCanvas.PaintLine(lastPaintPos, currentPos, playerColor, characterBrushSize, characterBrushTexture, segments);
            lastPaintPos = currentPos;
        }
    }
}
