using UnityEngine;

public class SpecialItemRotator : MonoBehaviour
{
    [Header("旋轉速度 (度/秒)")]
    public float rotationSpeed = 60f;

    private ItemData data;

    private void Awake()
    {
        // 嘗試獲取 ItemData。
        // 如果是「可收集加分道具」就會有；如果是「純效果特殊道具」就會是 null。
        data = GetComponent<ItemData>();
    }

    private void Update()
    {
        // 【關鍵判斷】
        // 如果這個道具有 ItemData，且 owner 不為空 (代表已經被玩家撿起來了)
        // 就停止原地旋轉，讓 ItemFollow 負責它的物理轉向。
        if (data != null && data.owner != null)
        {
            return;
        }

        // 沿著世界座標的 Y 軸 (向上) 緩慢旋轉
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
    }
}