using System.Collections;
using UnityEngine;

public class FingerToyController : MonoBehaviour
{
    public AudioSource sound;

    [Header("旋轉設定")]
    [Tooltip("旋轉的速度 (度/秒)")]
    public float rotationSpeed = 30f;

    void Update()
    {
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }

    [Header("彈力設定")]
    [Tooltip("彈力")]
    public float bounceForce = 10f;
    public float bounceDuration = 0.1f;

    void OnCollisionEnter(Collision collision)
    {
        Rigidbody otherRb = collision.rigidbody;
        if (otherRb != null && otherRb.CompareTag("Player"))
        {
            PlayerController player = otherRb.GetComponent<PlayerController>();
            if (player != null)
            {
                player.avilibleMovement = false;
                player.ForceStopMotion(); // 清空狀態避免卡死

                // 計算彈跳方向（稍微往上）
                Vector3 bounceDir = (-collision.contacts[0].normal + Vector3.up * 0.1f).normalized;

                StartCoroutine(ApplyBounce(player, otherRb, bounceDir));
                if (sound) sound.Play();
            }
        }
    }

    IEnumerator ApplyBounce(PlayerController player, Rigidbody playerRb, Vector3 direction)
    {
        playerRb.AddForce(direction * bounceForce, ForceMode.Impulse);

        // 等待彈跳穩定
        yield return new WaitForSeconds(bounceDuration);

        // 強制清理速度確保移動恢復正常
        player.ForceStopMotion();
        player.avilibleMovement = true;
    }
}
