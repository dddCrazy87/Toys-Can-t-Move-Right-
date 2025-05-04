using UnityEngine;

public class CastleController : MonoBehaviour
{
    public GameManager gameManager;
    void OnTriggerEnter(Collider other)
    {
        GameObject go = other.gameObject;
        if (go.CompareTag("Collectable")) {
            PlayerController player = go.GetComponent<ItemData>().owner.GetComponent<PlayerController>();
            gameManager.PlayerIncreasePoint(player.playerIndex);
            player.CollectItemToCastle(go.GetComponent<ItemData>().collectedItemIndex);
        }
    }
}
