using UnityEngine;

public class CastleController : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        GameObject go = other.gameObject;
        if (!go.CompareTag("Player")) return;
        go.GetComponent<PlayerController>().CompeleItemCollection();
    }
}
