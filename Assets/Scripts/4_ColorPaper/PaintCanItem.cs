using UnityEngine;
using System.Collections;
using DG.Tweening;

public class PaintCanItem : MonoBehaviour
{
    [Header("爆炸設定")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float explosionBrushSize = 0.2f;
    [SerializeField] private int paintDensity = 30;
    [SerializeField] [Range(0f, 1f)] private float explosionOpacity = 1f;  // 爆炸顏料透明度（1 = 完全不透明）

    [Header("角色力量影響（根據 bounceForce）")]
    [SerializeField] private float minPowerMultiplier = 0.6f;   // 最弱角色的爆炸倍率
    [SerializeField] private float maxPowerMultiplier = 1.5f;   // 最強角色的爆炸倍率
    [SerializeField] private float minBounceForce = 25f;   // 最弱角色的 bounceForce
    [SerializeField] private float maxBounceForce = 100f;  // 最強角色的 bounceForce

    [Header("顏色設定")]
    [SerializeField] private int materialIndex = 1;
    [SerializeField] private float colorChangeInterval = 0.4f;

    private Renderer targetRenderer;

    [Header("四種材質球")]
    [SerializeField] private Material blueMaterial;
    [SerializeField] private Material greenMaterial;
    [SerializeField] private Material yellowMaterial;
    [SerializeField] private Material redMaterial;

    [Header("顏色對照（用於爆炸效果）")]
    [SerializeField] private Color blueColor = new Color(0.3f, 0.45f, 0.9f, 1f);
    [SerializeField] private Color greenColor = new Color(0.45f, 0.8f, 0.5f, 1f);
    [SerializeField] private Color yellowColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color redColor = new Color(0.95f, 0.36f, 0.37f, 1f);

    [Header("視覺效果")]
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private float effectDuration = 1.5f;

    [Header("音效")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] [Range(0f, 2f)] private float soundVolume = 1.5f;  // 音量倍率

    [Header("閒置動畫")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.3f;
    [SerializeField] private float rotateSpeed = 50f;

    private PaintCanvas paintCanvas;
    private Vector3 startPosition;
    private bool isCollected = false;
    private int spawnPointIndex = -1;
    private int currentColorIndex = 0;
    private float colorTimer = 0f;

    private Material[] materials;
    private Color[] colors;

    void Start()
    {
        startPosition = transform.position;
        paintCanvas = FindFirstObjectByType<PaintCanvas>();

        // 動態取得 Renderer（避免 Prefab 參照問題）
        targetRenderer = GetComponentInChildren<Renderer>();

        // 初始化材質和顏色陣列（順序對應：blue, green, yellow, red）
        materials = new Material[] { blueMaterial, greenMaterial, yellowMaterial, redMaterial };
        colors = new Color[] { blueColor, greenColor, yellowColor, redColor };

        if (targetRenderer != null)
        {
            SetMaterial(0);
            Debug.Log($"[PaintCanItem] 初始化成功，位置: {transform.position}");
        }
        else
        {
            Debug.LogWarning("[PaintCanItem] 找不到 Renderer！");
        }
    }

    void Update()
    {
        if (isCollected) return;

        // 上下浮動動畫
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);

        // 旋轉動畫
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);

        // 材質循環
        colorTimer += Time.deltaTime;
        if (colorTimer >= colorChangeInterval)
        {
            colorTimer = 0f;
            currentColorIndex = (currentColorIndex + 1) % materials.Length;
            SetMaterial(currentColorIndex);
        }
    }

    void SetMaterial(int index)
    {
        if (targetRenderer == null)
        {
            Debug.LogError("[PaintCanItem] targetRenderer is null!");
            return;
        }
        if (materials == null || index >= materials.Length)
        {
            Debug.LogError($"[PaintCanItem] materials array issue! materials={materials}, index={index}");
            return;
        }
        if (materials[index] == null)
        {
            Debug.LogError($"[PaintCanItem] materials[{index}] is null! 請在 Inspector 設定材質球");
            return;
        }

        // 直接換材質球（用 .materials 才能在執行時對實例生效）
        Material[] mats = targetRenderer.materials;

        // 檢查 materialIndex 是否超出範圍
        if (materialIndex >= mats.Length)
        {
            Debug.LogWarning($"[PaintCanItem] materialIndex ({materialIndex}) 超出模型材質數量 ({mats.Length})，改用索引 0");
            materialIndex = 0;
        }

        mats[materialIndex] = materials[index];
        targetRenderer.materials = mats;
    }

    void SetMaterialByName(string colorName)
    {
        if (targetRenderer == null || materials == null) return;

        int index = colorName switch
        {
            "blue" => 0,
            "green" => 1,
            "yellow" => 2,
            "red" => 3,
            _ => 0
        };
        SetMaterial(index);
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[PaintCanItem] OnTriggerEnter 被觸發，碰撞物件: {other.name}");

        if (isCollected) return;

        // 往父物件尋找 PlayerController（因為 Collider 可能在子物件上）
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            Debug.Log($"[PaintCanItem] {other.name} 及其父物件都沒有 PlayerController");
            return;
        }

        Debug.Log($"[PaintCanItem] 玩家 {player.playerName} 收集了顏料罐！");
        isCollected = true;
        StartCoroutine(TriggerExplosion(player));
    }

    IEnumerator TriggerExplosion(PlayerController player)
    {
        string playerColor = player.playerColor;
        Vector3 explosionCenter = transform.position;

        // 根據角色力量計算爆炸參數
        float powerNormalized = Mathf.InverseLerp(minBounceForce, maxBounceForce, player.bounceForce);
        float powerMultiplier = Mathf.Lerp(minPowerMultiplier, maxPowerMultiplier, powerNormalized);

        float adjustedRadius = explosionRadius * powerMultiplier;
        float adjustedBrushSize = explosionBrushSize * powerMultiplier;
        int adjustedDensity = Mathf.RoundToInt(paintDensity * powerMultiplier);

        // 取得角色專屬筆刷貼圖
        Texture2D playerBrushTexture = player.brushTexture;

        string brushName = playerBrushTexture != null ? playerBrushTexture.name : "圓形";
        Debug.Log($"[PaintCanItem] {player.playerName} 力量:{player.bounceForce} 爆炸倍率:{powerMultiplier:F2} 範圍:{adjustedRadius:F2} 密度:{adjustedDensity} 筆刷:{brushName}");

        // 變成玩家的顏色
        SetMaterialByName(playerColor);

        // 播放收集音效
        if (collectSound != null)
        {
            PlaySoundAtPoint(collectSound, explosionCenter, soundVolume);
        }

        // 關閉碰撞
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // DOTween 收集動畫
        Sequence collectSequence = DOTween.Sequence();

        // 先彈跳放大
        collectSequence.Append(transform.DOScale(1.3f, 0.1f).SetEase(Ease.OutBack));

        // 快速旋轉
        collectSequence.Join(transform.DORotate(new Vector3(0, 360, 0), 0.3f, RotateMode.FastBeyond360).SetEase(Ease.Linear));

        // 縮小消失
        collectSequence.Append(transform.DOScale(0f, 0.2f).SetEase(Ease.InBack));

        // 等待動畫完成
        yield return collectSequence.WaitForCompletion();

        // 播放爆炸音效
        if (explosionSound != null)
        {
            PlaySoundAtPoint(explosionSound, explosionCenter, soundVolume);
        }

        // 生成爆炸粒子效果
        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(explosionEffectPrefab, explosionCenter, Quaternion.identity);

            var particleSystem = effect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                var main = particleSystem.main;
                main.startColor = GetColorFromString(playerColor);
            }

            Destroy(effect, effectDuration);
        }

        // 在畫布上噴灑顏料（使用根據角色力量調整後的參數 + 角色專屬筆刷貼圖）
        if (paintCanvas != null)
        {
            paintCanvas.PaintExplosion(explosionCenter, playerColor, adjustedRadius, adjustedBrushSize, adjustedDensity, playerBrushTexture, explosionOpacity);
        }

        // 通知 Spawner
        PaintCanSpawner spawner = FindFirstObjectByType<PaintCanSpawner>();
        if (spawner != null)
        {
            spawner.OnPaintCanCollected(spawnPointIndex);
        }

        Destroy(gameObject);
    }

    Color GetColorFromString(string colorName)
    {
        return colorName switch
        {
            "blue" => blueColor,
            "green" => greenColor,
            "yellow" => yellowColor,
            "red" => redColor,
            _ => Color.white
        };
    }

    public void SetSpawnPointIndex(int index)
    {
        spawnPointIndex = index;
    }

    /// <summary>
    /// 播放音效（支援自訂音量）
    /// </summary>
    void PlaySoundAtPoint(AudioClip clip, Vector3 position, float volume)
    {
        GameObject tempGO = new GameObject("TempAudio");
        tempGO.transform.position = position;
        AudioSource audioSource = tempGO.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.spatialBlend = 0f;  // 2D 音效，不受距離影響
        audioSource.Play();
        Destroy(tempGO, clip.length + 0.1f);
    }
}
