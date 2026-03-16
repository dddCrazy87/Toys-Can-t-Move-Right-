using UnityEngine;
using DG.Tweening;

/// <summary>
/// 顏料補充道具
/// 收集後補充 1 格顏料，不噴出任何顏料
/// </summary>
public class PaintRefillItem : MonoBehaviour
{
    [Header("補充設定")]
    [SerializeField] private float refillAmount = 1f;  // 補充 1 格

    [Header("材質設定")]
    [SerializeField] private int materialIndex = 0;
    [SerializeField] private float colorChangeInterval = 0.4f;

    [Header("四種材質球")]
    [SerializeField] private Material blueMaterial;
    [SerializeField] private Material greenMaterial;
    [SerializeField] private Material yellowMaterial;
    [SerializeField] private Material redMaterial;

    [Header("顏色對照")]
    [SerializeField] private Color blueColor = new Color(0.3f, 0.45f, 0.9f, 1f);
    [SerializeField] private Color greenColor = new Color(0.45f, 0.8f, 0.5f, 1f);
    [SerializeField] private Color yellowColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color redColor = new Color(0.95f, 0.36f, 0.37f, 1f);

    [Header("視覺效果")]
    [SerializeField] private GameObject collectEffectPrefab;
    [SerializeField] private float effectDuration = 1f;

    [Header("音效")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] [Range(0f, 2f)] private float soundVolume = 1f;

    [Header("閒置動畫")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.3f;
    [SerializeField] private float rotateSpeed = 50f;

    private Renderer targetRenderer;
    private Material[] materials;
    private Color[] colors;
    private int currentColorIndex = 0;
    private float colorTimer = 0f;

    private Vector3 startPosition;
    private bool isCollected = false;
    private int spawnPointIndex = -1;

    void Start()
    {
        startPosition = transform.position;

        // 動態取得 Renderer
        targetRenderer = GetComponentInChildren<Renderer>();

        // 初始化材質和顏色陣列
        materials = new Material[] { blueMaterial, greenMaterial, yellowMaterial, redMaterial };
        colors = new Color[] { blueColor, greenColor, yellowColor, redColor };

        if (targetRenderer != null)
        {
            SetMaterial(0);
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

        // 材質循環閃爍
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
        if (targetRenderer == null) return;
        if (materials == null || index >= materials.Length) return;
        if (materials[index] == null) return;

        Material[] mats = targetRenderer.materials;

        // 檢查 materialIndex 是否超出範圍
        if (materialIndex >= mats.Length)
        {
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

    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        // 檢查玩家是否有 PaintBrush 組件
        PaintBrush brush = player.GetComponent<PaintBrush>();
        if (brush == null) return;

        // 檢查玩家是否有 PaintEnergy 組件
        PaintEnergy energy = player.GetComponent<PaintEnergy>();
        if (energy == null) return;

        // 檢查顏料是否已滿
        if (energy.IsFull())
        {
            Debug.Log($"[PaintRefillItem] {player.playerName} 顏料已滿，無法收集");
            return;
        }

        Debug.Log($"[PaintRefillItem] 玩家 {player.playerName} 收集了顏料補充道具！");
        isCollected = true;

        // 變成玩家的顏色
        SetMaterialByName(player.playerColor);

        // 補充顏料
        brush.RefillEnergy(refillAmount);
        Debug.Log($"[PaintRefillItem] {player.playerName} 補充了 {refillAmount} 格顏料");

        // 播放收集效果
        PlayCollectEffect(player);
    }

    void PlayCollectEffect(PlayerController player)
    {
        Vector3 collectPosition = transform.position;

        // 播放音效
        if (collectSound != null)
        {
            PlaySoundAtPoint(collectSound, collectPosition, soundVolume);
        }

        // 關閉碰撞
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // DOTween 收集動畫
        Sequence collectSequence = DOTween.Sequence();
        collectSequence.Append(transform.DOScale(1.3f, 0.1f).SetEase(Ease.OutBack));
        collectSequence.Join(transform.DORotate(new Vector3(0, 360, 0), 0.3f, RotateMode.FastBeyond360).SetEase(Ease.Linear));
        collectSequence.Append(transform.DOScale(0f, 0.2f).SetEase(Ease.InBack));
        collectSequence.OnComplete(() =>
        {
            // 生成收集粒子效果（使用玩家顏色）
            if (collectEffectPrefab != null)
            {
                GameObject effect = Instantiate(collectEffectPrefab, collectPosition, Quaternion.identity);
                var particleSystem = effect.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    var main = particleSystem.main;
                    main.startColor = GetColorFromString(player.playerColor);
                }
                Destroy(effect, effectDuration);
            }

            // 通知 Spawner
            PaintRefillSpawner spawner = FindFirstObjectByType<PaintRefillSpawner>();
            if (spawner != null)
            {
                spawner.OnPaintRefillCollected(spawnPointIndex);
            }

            Destroy(gameObject);
        });
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
