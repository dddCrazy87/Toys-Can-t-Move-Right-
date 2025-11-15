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

}
