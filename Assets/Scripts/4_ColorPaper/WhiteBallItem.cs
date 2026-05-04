using UnityEngine;
using System.Collections;
using DG.Tweening;

/// <summary>
/// 白色水球道具
/// 固定白色外觀，吃到噴出白色擦除效果
/// </summary>
public class WhiteBallItem : MonoBehaviour
{
    [Header("爆炸設定")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float explosionBrushSize = 0.2f;
    [SerializeField] private int eraseDensity = 30;

    [Header("角色力量影響")]
    [SerializeField] private float minPowerMultiplier = 0.6f;
    [SerializeField] private float maxPowerMultiplier = 1.5f;
    [SerializeField] private float minBounceForce = 25f;
    [SerializeField] private float maxBounceForce = 100f;

    [Header("視覺效果")]
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private float effectDuration = 1.5f;
    [SerializeField] private Color whiteColor = Color.white;

    [Header("音效")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] [Range(0f, 2f)] private float soundVolume = 1.5f;

    [Header("閒置動畫")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.3f;
    [SerializeField] private float rotateSpeed = 50f;

    private PaintCanvas paintCanvas;
    private ColorGrid colorGrid;
    private Vector3 startPosition;
    private bool isCollected = false;
    private int spawnPointIndex = -1;

    void Start()
    {
        startPosition = transform.position;
        paintCanvas = FindFirstObjectByType<PaintCanvas>();
        colorGrid = FindFirstObjectByType<ColorGrid>();
    }

    void Update()
    {
        if (isCollected) return;

        // 上下浮動動畫
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);

        // 旋轉動畫
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        Debug.Log($"[WhiteBallItem] 玩家 {player.playerName} 收集了白色水球！");
        isCollected = true;
        StartCoroutine(TriggerExplosion(player));
    }

    IEnumerator TriggerExplosion(PlayerController player)
    {
        Vector3 explosionCenter = transform.position;

        // 根據角色力量計算爆炸參數
        float powerNormalized = Mathf.InverseLerp(minBounceForce, maxBounceForce, player.bounceForce);
        float powerMultiplier = Mathf.Lerp(minPowerMultiplier, maxPowerMultiplier, powerNormalized);

        float adjustedRadius = explosionRadius * powerMultiplier;
        float adjustedBrushSize = explosionBrushSize * powerMultiplier;
        int adjustedDensity = Mathf.RoundToInt(eraseDensity * powerMultiplier);

        // 取得角色專屬筆刷貼圖（用於擦除形狀）
        Texture2D playerBrushTexture = player.brushTexture;

        Debug.Log($"[WhiteBallItem] {player.playerName} 力量:{player.bounceForce} 爆炸倍率:{powerMultiplier:F2}");

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
        collectSequence.Append(transform.DOScale(1.3f, 0.1f).SetEase(Ease.OutBack));
        collectSequence.Join(transform.DORotate(new Vector3(0, 360, 0), 0.3f, RotateMode.FastBeyond360).SetEase(Ease.Linear));
        collectSequence.Append(transform.DOScale(0f, 0.2f).SetEase(Ease.InBack));

        yield return collectSequence.WaitForCompletion();

        // 播放爆炸音效
        if (explosionSound != null)
        {
            PlaySoundAtPoint(explosionSound, explosionCenter, soundVolume);
        }

        // 生成白色爆炸粒子效果
        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(explosionEffectPrefab, explosionCenter, Quaternion.identity);
            var particleSystem = effect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                var main = particleSystem.main;
                main.startColor = whiteColor;
            }
            Destroy(effect, effectDuration);
        }

        // 在畫布上噴灑白色擦除
        if (paintCanvas != null)
        {
            paintCanvas.WhiteExplosion(explosionCenter, adjustedRadius, adjustedBrushSize, adjustedDensity, playerBrushTexture);
        }

        // 同步擦除 ColorGrid — 擦除爆炸範圍內所有格子
        // 視覺上每個噴灑點還有 brushSize 的範圍，需要加上去才能匹配
        if (colorGrid != null)
        {
            float canvasWorldWidth = 39.2f;  // canvasMax.x - canvasMin.x
            float brushWorldRadius = adjustedBrushSize * canvasWorldWidth * 0.5f;
            colorGrid.EraseArea(explosionCenter, adjustedRadius + brushWorldRadius);
        }

        // 通知 Spawner
        WhiteBallSpawner spawner = FindFirstObjectByType<WhiteBallSpawner>();
        if (spawner != null)
        {
            spawner.OnWhiteBallCollected(spawnPointIndex);
        }

        Destroy(gameObject);
    }



    public void SetSpawnPointIndex(int index)
    {
        spawnPointIndex = index;
    }

    void PlaySoundAtPoint(AudioClip clip, Vector3 position, float volume)
    {
        GameObject tempGO = new GameObject("TempAudio");
        tempGO.transform.position = position;
        AudioSource audioSource = tempGO.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.spatialBlend = 0f;
        audioSource.Play();
        Destroy(tempGO, clip.length + 0.1f);
    }
}
