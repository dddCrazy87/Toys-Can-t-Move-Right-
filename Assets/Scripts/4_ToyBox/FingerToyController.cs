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
            PlayerController player = otherRb.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                player.avilibleMovement = false;

                // 以接觸點法線反向為彈跳方向，但略微往上調整避免貼地穿牆
                Vector3 bounceDir = (-collision.contacts[0].normal + Vector3.up * 0.1f).normalized;

                StartCoroutine(ApplyBounce(player, otherRb, bounceDir));
                sound.Play();
            }
        }
    }

    IEnumerator ApplyBounce(PlayerController player, Rigidbody playerRb, Vector3 direction)
    {
        // 先清空速度，避免疊加造成彈飛
        playerRb.linearVelocity = Vector3.zero;

        // 立即施加一次性彈跳（以Impulse方式）
        playerRb.AddForce(direction * bounceForce, ForceMode.Impulse);

        // 等待彈跳結束
        yield return new WaitForSeconds(bounceDuration);

        // 等待剛體穩定（確保不再穿牆）
        yield return new WaitForFixedUpdate();

        player.avilibleMovement = true;
    }
}
