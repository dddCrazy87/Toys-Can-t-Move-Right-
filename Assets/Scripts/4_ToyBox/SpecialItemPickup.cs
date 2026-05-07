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
        // 確保碰到的是 ToyBox 模式下的玩家
        if (other.transform.parent == null) return;
        ToyBoxPlayer player = other.transform.parent.GetComponent<ToyBoxPlayer>();

        if (player != null)
        {
            // 1. 賦予玩家這個道具的能力 (新的會自動蓋過舊的)
            player.EquipSpecialItem(itemType);

            // 2. 播放撿到道具的音效 (若有需要)
            FindFirstObjectByType<GameSoundEffect>()?.PlayGetItemSound();

            // 3. 通知 ItemSpawner 回收並安排重生
            ItemSpawner spawner = FindFirstObjectByType<ItemSpawner>();
            if (spawner != null)
            {
                //spawner.ItemCollected(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}