using UnityEngine;

public class PaintBrush : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private float minMoveDistance = 0.3f;  // 最小移動距離才畫（越大越省性能）
    [SerializeField] private float backwardOffset = 0.5f;   // 往角色後方偏移

    private PaintCanvas paintCanvas;
    private string playerColor;
    private Vector3 lastPaintPos;
    private bool isInitialized = false;
    private bool isPainting = false;

    /// <summary>
    /// 初始化筆刷
    /// </summary>
    public void Initialize(PaintCanvas canvas, string color)
    {
        paintCanvas = canvas;
        playerColor = color;
        lastPaintPos = GetPaintPosition();
        isInitialized = true;
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

        // 畫第一個點
        paintCanvas.Paint(lastPaintPos, playerColor);
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
            // 畫一條線從上一個位置到當前位置
            int segments = Mathf.CeilToInt(distance / minMoveDistance);
            paintCanvas.PaintLine(lastPaintPos, currentPos, playerColor, segments);
            lastPaintPos = currentPos;
        }
    }
}
