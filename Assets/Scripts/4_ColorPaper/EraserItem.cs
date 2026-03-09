using UnityEngine;
using DG.Tweening;

public class EraserItem : MonoBehaviour
{
    [Header("橡皮擦設定")]
    [SerializeField] private float eraserDuration = 5f;  // 給予的橡皮擦持續時間

    [Header("視覺效果")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.3f;
    [SerializeField] private float rotateSpeed = 50f;

    [Header("音效")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] [Range(0f, 2f)] private float soundVolume = 1f;

    private Vector3 startPosition;
    private bool isCollected = false;
    private int spawnPointIndex = -1;

    void Start()
    {
        startPosition = transform.position;
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

        // 往父物件尋找 PlayerController
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        // 取得 PaintBrush 組件
        PaintBrush paintBrush = player.GetComponent<PaintBrush>();
        if (paintBrush == null)
        {
            Debug.LogWarning("[EraserItem] 玩家沒有 PaintBrush 組件！");
            return;
        }

        Debug.Log($"[EraserItem] 玩家 {player.playerName} 撿到橡皮擦！");
        isCollected = true;

        // 啟動橡皮擦模式
        paintBrush.ActivateEraserMode(eraserDuration);

        // 播放音效
        if (pickupSound != null)
        {
            PlaySoundAtPoint(pickupSound, transform.position, soundVolume);
        }

        // 關閉碰撞
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 收集動畫
        Sequence collectSequence = DOTween.Sequence();
        collectSequence.Append(transform.DOScale(1.3f, 0.1f).SetEase(Ease.OutBack));
        collectSequence.Join(transform.DORotate(new Vector3(0, 360, 0), 0.3f, RotateMode.FastBeyond360).SetEase(Ease.Linear));
        collectSequence.Append(transform.DOScale(0f, 0.2f).SetEase(Ease.InBack));
        collectSequence.OnComplete(() =>
        {
            // 通知 Spawner
            EraserSpawner spawner = FindFirstObjectByType<EraserSpawner>();
            if (spawner != null)
            {
                spawner.OnEraserCollected(spawnPointIndex);
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
