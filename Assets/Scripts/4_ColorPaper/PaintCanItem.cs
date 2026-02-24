using UnityEngine;
using System.Collections;

public class PaintCanItem : MonoBehaviour
{
    [Header("爆炸設定")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float explosionBrushSize = 0.15f;
    [SerializeField] private int paintDensity = 30;

    [Header("視覺效果")]
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private float effectDuration = 1.5f;

    [Header("音效")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private AudioClip explosionSound;

    [Header("動畫")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.3f;
    [SerializeField] private float rotateSpeed = 50f;

    private PaintCanvas paintCanvas;
    private Vector3 startPosition;
    private bool isCollected = false;
    private int spawnPointIndex = -1;

    void Start()
    {
        startPosition = transform.position;
        paintCanvas = FindFirstObjectByType<PaintCanvas>();
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

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        isCollected = true;
        StartCoroutine(TriggerExplosion(player));
    }

    IEnumerator TriggerExplosion(PlayerController player)
    {
        string playerColor = player.playerColor;
        Vector3 explosionCenter = transform.position;

        // 播放收集音效
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, explosionCenter);
        }

        // 隱藏顏料罐
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = false;
        }
        GetComponent<Collider>().enabled = false;

        yield return new WaitForSeconds(0.1f);

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

        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }

    Color GetColorFromString(string colorName)
    {
        return colorName switch
        {
            "blue" => new Color(0.3f, 0.45f, 0.9f, 1f),
            "yellow" => new Color(1f, 0.85f, 0.2f, 1f),
            "green" => new Color(0.45f, 0.8f, 0.5f, 1f),
            "red" => new Color(0.95f, 0.36f, 0.37f, 1f),
            _ => Color.white
        };
    }

    public void SetSpawnPointIndex(int index)
    {
        spawnPointIndex = index;
    }
}
