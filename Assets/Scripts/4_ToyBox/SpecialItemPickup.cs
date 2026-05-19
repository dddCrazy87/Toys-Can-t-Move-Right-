using UnityEngine;

public enum SpecialItemType
{
    None,
    Blocker,
    FingerToyBuffer,
    Freezer
}

public class SpecialItemPickup : MonoBehaviour
{
    [Header("設定這個特殊道具的類型")]
    public SpecialItemType itemType;

    private void OnTriggerEnter(Collider other)
    {
        GameObject root = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
        if (!root.CompareTag("Player")) return;

        var player = root.GetComponent<ToyBoxPlayer>();

        if (player != null)
        {
            player.EquipSpecialItem(itemType);
            FindFirstObjectByType<GameSoundEffect>()?.PlayGetSpecialSound();

            ItemSpawner spawner = FindFirstObjectByType<ItemSpawner>();
            if (spawner != null) spawner.ItemCollected(gameObject);
            else Destroy(gameObject);
        }
    }
}