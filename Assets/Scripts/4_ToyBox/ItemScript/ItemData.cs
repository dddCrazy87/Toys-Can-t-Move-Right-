using UnityEngine;

public class ItemData : MonoBehaviour
{
    [Header("生成點索引（由 ItemSpawner 設定）")]
    public int spawnPointIndex = -1;

    [Header("特殊加分設定")]
    public int extraScore = 0;

    [HideInInspector] public Transform owner;  // 目前持有這顆的玩家（null 代表自由道具）
    [HideInInspector] public int index = -1;   // 在玩家串中的順位
    [HideInInspector] public bool isBusy = false; // 正在被處理（避免多人同時搶）

}
