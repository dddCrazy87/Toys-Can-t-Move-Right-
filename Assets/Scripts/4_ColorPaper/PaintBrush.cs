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

    [Header("橡皮擦模式")]
    [SerializeField] private float eraserDuration = 5f;  // 橡皮擦持續時間
    [SerializeField] private GameObject eraserEffectPrefab;  // 橡皮擦特效 Prefab（可選）
    [SerializeField] private string eraserAnimatorTrigger = "Eraser";  // Animator 觸發器名稱（可選）

    [Header("橡皮擦音效")]
    [SerializeField] private AudioClip erasingSound;  // 擦除時的音效（會循環播放）
    [SerializeField] [Range(0f, 1f)] private float erasingSoundVolume = 0.5f;

    private PaintCanvas paintCanvas;
    private ColorGrid colorGrid;  // 用於擦除時同步扣分
    private string playerColor;
    private Vector3 lastPaintPos;
    private bool isInitialized = false;
    private bool isPainting = false;
    private float characterBrushSize;  // 角色專屬筆刷大小
    private Texture2D characterBrushTexture;  // 角色專屬筆刷貼圖

    // 橡皮擦狀態
    private bool isEraserMode = false;
    private float eraserTimeRemaining = 0f;
    private GameObject activeEraserEffect;
    private Animator playerAnimator;
    private AudioSource erasingAudioSource;

    /// <summary>
    /// 初始化筆刷
    /// </summary>
    public void Initialize(PaintCanvas canvas, string color)
    {
        paintCanvas = canvas;
        playerColor = color;
        lastPaintPos = GetPaintPosition();

        // 取得 ColorGrid（用於橡皮擦扣分）
        colorGrid = FindFirstObjectByType<ColorGrid>();

        // 根據角色速度計算筆刷大小
        CalculateBrushSize();

        // 快取 Animator（用於橡皮擦動畫）
        playerAnimator = GetComponentInChildren<Animator>();

        isInitialized = true;
    }

    /// <summary>
    /// 啟動橡皮擦模式
    /// </summary>
    public void ActivateEraserMode()
    {
        ActivateEraserMode(eraserDuration);
    }

    /// <summary>
    /// 啟動橡皮擦模式（自訂時間）
    /// </summary>
    public void ActivateEraserMode(float duration)
    {
        isEraserMode = true;
        eraserTimeRemaining = duration;

        // 重設位置追蹤，避免第一次擦除跳太遠
        lastPaintPos = GetPaintPosition();

        // 啟動視覺效果
        StartEraserVisualEffect();

        Debug.Log($"[PaintBrush] 橡皮擦模式啟動！持續 {duration} 秒");
    }

    /// <summary>
    /// 是否在橡皮擦模式
    /// </summary>
    public bool IsInEraserMode()
    {
        return isEraserMode;
    }

    /// <summary>
    /// 取得橡皮擦剩餘時間
    /// </summary>
    public float GetEraserTimeRemaining()
    {
        return eraserTimeRemaining;
    }

    /// <summary>
    /// 啟動橡皮擦視覺效果
    /// </summary>
    private void StartEraserVisualEffect()
    {
        // 生成特效
        if (eraserEffectPrefab != null)
        {
            activeEraserEffect = Instantiate(eraserEffectPrefab, transform);
            activeEraserEffect.transform.localPosition = Vector3.zero;
        }

        // 觸發 Animator（如果有設定）
        if (playerAnimator != null && !string.IsNullOrEmpty(eraserAnimatorTrigger))
        {
            playerAnimator.SetBool(eraserAnimatorTrigger, true);
        }

        // 播放擦除音效（循環）
        if (erasingSound != null)
        {
            if (erasingAudioSource == null)
            {
                erasingAudioSource = gameObject.AddComponent<AudioSource>();
                erasingAudioSource.spatialBlend = 0f;  // 2D 音效
            }
            erasingAudioSource.clip = erasingSound;
            erasingAudioSource.volume = erasingSoundVolume;
            erasingAudioSource.loop = true;
            erasingAudioSource.Play();
        }
    }

    /// <summary>
    /// 停止橡皮擦視覺效果
    /// </summary>
    private void StopEraserVisualEffect()
    {
        // 移除特效
        if (activeEraserEffect != null)
        {
            Destroy(activeEraserEffect);
            activeEraserEffect = null;
        }

        // 停止 Animator
        if (playerAnimator != null && !string.IsNullOrEmpty(eraserAnimatorTrigger))
        {
            playerAnimator.SetBool(eraserAnimatorTrigger, false);
        }

        // 停止擦除音效
        if (erasingAudioSource != null && erasingAudioSource.isPlaying)
        {
            erasingAudioSource.Stop();
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
    /// 開始畫圖
    /// </summary>
    public void StartPainting()
    {
        if (!isInitialized) return;
        isPainting = true;
        lastPaintPos = GetPaintPosition();

        // 畫第一個點（根據模式決定畫或擦）
        if (isEraserMode)
        {
            paintCanvas.Erase(lastPaintPos, characterBrushSize, characterBrushTexture);
            // 同步擦除 ColorGrid
            if (colorGrid != null)
            {
                colorGrid.EraseAtPosition(lastPaintPos);
            }
        }
        else
        {
            paintCanvas.Paint(lastPaintPos, playerColor, characterBrushSize, characterBrushTexture);
        }
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
        // 更新橡皮擦計時器
        if (isEraserMode)
        {
            eraserTimeRemaining -= Time.deltaTime;
            if (eraserTimeRemaining <= 0f)
            {
                isEraserMode = false;
                eraserTimeRemaining = 0f;
                StopEraserVisualEffect();
                Debug.Log("[PaintBrush] 橡皮擦模式結束");
            }
        }

        if (!isInitialized || paintCanvas == null)
        {
            if (isEraserMode) Debug.LogWarning($"[PaintBrush] 擦除失敗：isInitialized={isInitialized}, paintCanvas={paintCanvas}");
            return;
        }

        // 橡皮擦模式時強制執行，一般模式需要 isPainting
        if (!isEraserMode && !isPainting) return;

        Vector3 currentPos = GetPaintPosition();
        float distance = Vector3.Distance(currentPos, lastPaintPos);

        // Debug: 橡皮擦模式時顯示狀態
        if (isEraserMode && distance >= minMoveDistance)
        {
            Debug.Log($"[PaintBrush] 擦除移動距離: {distance:F2} >= {minMoveDistance}");
        }

        // 移動超過最小距離才畫線/擦線
        if (distance >= minMoveDistance)
        {
            int segments = Mathf.CeilToInt(distance / minMoveDistance);

            if (isEraserMode)
            {
                // 擦除模式 - 同時擦除視覺和計分
                paintCanvas.EraseLine(lastPaintPos, currentPos, characterBrushSize, characterBrushTexture, segments);

                // 同步擦除 ColorGrid 的格子所有權（扣分）
                if (colorGrid != null)
                {
                    for (int i = 0; i <= segments; i++)
                    {
                        float t = (float)i / segments;
                        Vector3 pos = Vector3.Lerp(lastPaintPos, currentPos, t);
                        colorGrid.EraseAtPosition(pos);
                    }
                }
            }
            else
            {
                // 一般畫圖模式
                paintCanvas.PaintLine(lastPaintPos, currentPos, playerColor, characterBrushSize, characterBrushTexture, segments);
            }

            lastPaintPos = currentPos;
        }
    }
}
