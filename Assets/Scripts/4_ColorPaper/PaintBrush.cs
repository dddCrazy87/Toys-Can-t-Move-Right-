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
    private bool isPainting = false;  // 由按壓狀態控制
    private float characterBrushSize;
    private Texture2D characterBrushTexture;

    private PlayerController playerController;
    private PaintEnergy paintEnergy;  // 能量系統參照

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

        // 取得 PlayerController 並訂閱按壓事件
        playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.OnPressStateChanged += OnPressStateChanged;
        }

        // 取得或添加能量系統
        paintEnergy = GetComponent<PaintEnergy>();
        if (paintEnergy == null)
        {
            paintEnergy = gameObject.AddComponent<PaintEnergy>();
        }

        isInitialized = true;
        Debug.Log($"[PaintBrush] 初始化完成：{playerController?.playerName}");
    }

    void OnDestroy()
    {
        // 取消訂閱
        if (playerController != null)
        {
            playerController.OnPressStateChanged -= OnPressStateChanged;
        }
    }

    /// <summary>
    /// 按壓狀態變化回調
    /// </summary>
    private void OnPressStateChanged(bool isPressed)
    {
        if (isPressed)
        {
            StartPainting();
        }
        else
        {
            StopPainting();
        }
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
    /// 開始畫圖（由按壓事件觸發）
    /// </summary>
    public void StartPainting()
    {
        if (!isInitialized) return;

        // 檢查能量是否足夠
        if (paintEnergy != null && !paintEnergy.HasEnergy())
        {
            Debug.Log("[PaintBrush] 顏料不足，無法繪製");
            return;
        }

        isPainting = true;
        lastPaintPos = GetPaintPosition();

        // 畫第一個點
        paintCanvas.Paint(lastPaintPos, playerColor, characterBrushSize, characterBrushTexture);
    }

    /// <summary>
    /// 停止畫圖（由按壓事件觸發）
    /// </summary>
    public void StopPainting()
    {
        isPainting = false;
    }

    void Update()
    {
        if (!isInitialized || paintCanvas == null) return;
        if (!isPainting) return;

        // 檢查能量，若耗盡則停止繪製
        if (paintEnergy != null)
        {
            if (!paintEnergy.HasEnergy())
            {
                StopPainting();
                return;
            }
            // 消耗能量
            paintEnergy.ConsumeEnergy(Time.deltaTime);
        }

        Vector3 currentPos = GetPaintPosition();
        float distance = Vector3.Distance(currentPos, lastPaintPos);

        // 移動超過最小距離才畫線
        if (distance >= minMoveDistance)
        {
            int segments = Mathf.CeilToInt(distance / minMoveDistance);
            paintCanvas.PaintLine(lastPaintPos, currentPos, playerColor, characterBrushSize, characterBrushTexture, segments);
            lastPaintPos = currentPos;
        }
    }

    /// <summary>
    /// 補充顏料（由道具呼叫）
    /// </summary>
    public void RefillEnergy(float amount)
    {
        if (paintEnergy != null)
        {
            paintEnergy.AddEnergy(amount);
        }
    }

    /// <summary>
    /// 取得能量系統（供 UI 使用）
    /// </summary>
    public PaintEnergy GetPaintEnergy()
    {
        return paintEnergy;
    }
}
