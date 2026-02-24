using UnityEngine;
using System.Collections;
using DG.Tweening;

public class PaintCanItem : MonoBehaviour
{
    [Header("爆炸設定")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float explosionBrushSize = 0.15f;
    [SerializeField] private int paintDensity = 30;

    [Header("顏色設定")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private int materialIndex = 0;
    [SerializeField] private float colorChangeInterval = 0.4f;

    [Header("四種顏色")]
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

    private Color[] colors;
    private MaterialPropertyBlock propertyBlock;

    void Start()
    {
        startPosition = transform.position;
        paintCanvas = FindFirstObjectByType<PaintCanvas>();

        // 初始化顏色陣列
        colors = new Color[] { blueColor, greenColor, yellowColor, redColor };

        // 初始化 MaterialPropertyBlock
        propertyBlock = new MaterialPropertyBlock();

        // 設定初始顏色
        if (targetRenderer != null)
        {
            Debug.Log($"[PaintCanItem] 初始化成功，位置: {transform.position}");
            SetColor(0);
        }
        else
        {
            Debug.LogWarning("[PaintCanItem] Target Renderer 未設定！");
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

        // 顏色循環
        colorTimer += Time.deltaTime;
        if (colorTimer >= colorChangeInterval)
        {
            colorTimer = 0f;
            currentColorIndex = (currentColorIndex + 1) % colors.Length;
            SetColor(currentColorIndex);
        }
    }

    void SetColor(int index)
    {
        if (targetRenderer == null || colors == null || index >= colors.Length) return;

        // 使用 MaterialPropertyBlock 改變顏色（效能好、不影響其他物件）
        targetRenderer.GetPropertyBlock(propertyBlock, materialIndex);
        // 同時設定兩種常見的顏色屬性名稱
        propertyBlock.SetColor("_BaseColor", colors[index]);  // URP
        propertyBlock.SetColor("_Color", colors[index]);      // Standard
        targetRenderer.SetPropertyBlock(propertyBlock, materialIndex);
    }

    void SetColorByName(string colorName)
    {
        Color color = GetColorFromString(colorName);

        if (targetRenderer == null) return;

        targetRenderer.GetPropertyBlock(propertyBlock, materialIndex);
        propertyBlock.SetColor("_BaseColor", color);  // URP
        propertyBlock.SetColor("_Color", color);      // Standard
        targetRenderer.SetPropertyBlock(propertyBlock, materialIndex);
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

        // 變成玩家的顏色
        SetColorByName(playerColor);

        // 播放收集音效
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, explosionCenter);
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
            AudioSource.PlayClipAtPoint(explosionSound, explosionCenter);
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

        // 在畫布上噴灑顏料
        if (paintCanvas != null)
        {
            paintCanvas.PaintExplosion(explosionCenter, playerColor, explosionRadius, explosionBrushSize, paintDensity);
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
}
