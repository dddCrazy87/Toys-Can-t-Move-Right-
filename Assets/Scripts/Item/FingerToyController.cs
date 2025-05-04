using System.Collections;
using UnityEngine;

public class FingerToyController : MonoBehaviour
{
    [Header("旋轉設定")]
    [Tooltip("旋轉的速度 (度/秒)")]
    public float rotationSpeed = 30f;
    Rigidbody rb;
    void Start() {
        // rb = GetComponent<Rigidbody>();
    }
    // void FixedUpdate() {
    //     rb.angularVelocity = new Vector3(0, 0, rotationSpeed * Mathf.Deg2Rad);
    // }

    void Update() {
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }

    [Header("彈力設定")]
    [Tooltip("彈力")]
    public float bounceForce = 10f;
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player")) {
            Rigidbody playerRb = collision.rigidbody;
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (playerRb != null) {
                player.avilibleMovement = false;
                Vector3 bounceDir = -collision.contacts[0].normal;
                StartCoroutine(ApplyBounce(player, playerRb, bounceDir, bounceForce, 0.3f));
            }
        }
    }

    IEnumerator ApplyBounce(PlayerController player, Rigidbody playerRb, Vector3 direction, float totalForce, float duration) {
        float timer = 0f;
        while (timer < duration) {
            playerRb.AddForce(direction * (totalForce / duration) * Time.fixedDeltaTime, ForceMode.VelocityChange);
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        player.avilibleMovement = true;
    }
}
