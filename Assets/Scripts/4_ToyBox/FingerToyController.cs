using System.Collections;
using UnityEngine;

public class FingerToyController : MonoBehaviour
{
    public AudioSource sound;

    [Header("旋轉設定")]
    public float rotationSpeed = 30f;

    void Update()
    {
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        Rigidbody otherRb = collision.rigidbody;
        if (otherRb != null && otherRb.CompareTag("Player"))
        {
            PlayerController player = otherRb.GetComponent<PlayerController>();
            if (player != null)
            {
                Vector3 bounceDir = (-collision.contacts[0].normal).normalized;

                player.StartKnockback(bounceDir);

                if (sound) sound.Play();
            }
        }
    }



    [Header("放大的倍率")]
    public float scaleMultiplier = 2f;

    [Header("持續時間")]
    public float activeDuration = 5f;

    private bool isScaled = false;
    private Vector3 originalScale;

    private void Start()
    {
        originalScale = transform.localScale;
    }

    // ++ 新增：讓外部事前檢查是否可觸發 ++
    public bool CanActivate()
    {
        return !isScaled;
    }

    // ++ 新增：純粹負責觸發效果 ++
    public void Activate()
    {
        if (isScaled) return;
        StartCoroutine(ActivateRoutine());
    }

    // 讓玩家呼叫的方法。回傳 true 代表成功觸發，回傳 false 代表正在忙
    public bool TryActivate()
    {
        if (isScaled) return false; // 已經變大了，拒絕觸發

        StartCoroutine(ActivateRoutine());
        return true;
    }

    private IEnumerator ActivateRoutine()
    {
        isScaled = true;
        FindFirstObjectByType<GameSoundEffect>()?.PlayFingerSound();

        // 瞬間變大 (如果你想要有漸變動畫，可以在這裡改用 Vector3.Lerp)
        transform.localScale = originalScale * scaleMultiplier;

        yield return new WaitForSeconds(activeDuration); // 等待秒數

        // 恢復原狀
        transform.localScale = originalScale;
        isScaled = false;
    }

}
