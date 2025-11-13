using UnityEngine;

public class CastleController : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        GameObject root = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
        if (!root.CompareTag("Player")) return;

        var player = root.GetComponent<PlayerController>();
        if (player != null)
        {
            player.CompleteItemCollection();
        }
    }
}
