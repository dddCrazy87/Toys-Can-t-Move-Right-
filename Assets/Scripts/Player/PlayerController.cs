using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public bool isP1 = false, isP2 = false;
    public TextMeshProUGUI playerIDUI;
    public int playerIndex;
    public void Initialize(string name, int id) {
        if (playerIDUI != null) playerIDUI.text = name;
        playerIndex = id;
    }

    [Header("移動設置")]
    [Tooltip("玩家移動速度")]
    public float moveSpeed = 5f;
    
    [Tooltip("玩家旋轉速度")]
    public float rotateSpeed = 15f;

    private Rigidbody rb;
    private void Awake() {
        isP1 = false; isP2 = false;
        rb = GetComponent<Rigidbody>();
        followPoint.position += new Vector3(0, 0, -itemFollowSpace);
        moveSpeed *= -1;
    }

    public bool avilibleMovement = true;
    private Vector3 movement;
    void Update() {
        // float horizontal = Input.GetAxisRaw("Horizontal");
        // float vertical = Input.GetAxisRaw("Vertical");
        // movement = new Vector3(horizontal, 0f, vertical).normalized;

        if (!avilibleMovement) return;
        // for demo and test
        if(isP1) {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            movement = new Vector3(horizontal, 0f, vertical).normalized;
        }
        else if (isP2) {
            float horizontal = 0f;
            float vertical = 0f;
            if (Input.GetKey(KeyCode.J)) horizontal = -1f;
            else if (Input.GetKey(KeyCode.L)) horizontal = 1f;
            if (Input.GetKey(KeyCode.I)) vertical = 1f;
            else if (Input.GetKey(KeyCode.K)) vertical = -1f;
            movement = new Vector3(horizontal, 0f, vertical).normalized;
        }
    }

    void FixedUpdate() {
        Move();
    }

    /// <summary>
    /// 移動玩家
    /// </summary>
    private void Move()
    {
        if (movement != Vector3.zero) {
            // 計算目標移動位置
            Vector3 targetVelocity = movement * moveSpeed;
            // 使用物理系統移動玩家
            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
            // 平滑旋轉玩家面向移動方向
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotateSpeed * Time.fixedDeltaTime);
        }
        else {
            // 停止水平移動
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }



    [Header("道具設置")]
    [Tooltip("道具跟隨時的間距")]
    public float itemFollowSpace;
    [Tooltip("道具跟隨的點")]
    public Transform followPoint;
    List<Transform> collectedItems = new();

    /// <summary>
    /// 處理玩家與道具的碰撞
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Collectable")) {
            if (collectedItems.Contains(other.transform)) return;
            CollectItem(other.transform);
        }
    }

    /// <summary>
    /// 收集道具的處理邏輯
    /// </summary>
    private void CollectItem(Transform item)
    {
        // 取得道具的資料元件
        ItemData itemData = item.GetComponent<ItemData>();

        // 先移除原有的 ItemFollow（避免殘留追蹤設定）
        Destroy(item.GetComponent<ItemFollow>());

        // 新增一個新的 ItemFollow 並設定
        ItemFollow itemFollow = item.gameObject.AddComponent<ItemFollow>();

        // 道具是自由的（沒被其他玩家擁有）
        if (itemData.owner == null) {
            itemFollow.maxDistance = itemFollowSpace;
            itemFollow.followSpeed = rotateSpeed;
            itemData.collectedItemIndex = collectedItems.Count;

            // 設定跟隨目標為前一個道具，或是 followPoint（如果是第一個）
            itemFollow.follow = collectedItems.Count == 0 ? followPoint : collectedItems[^1];

            // 加入當前收集清單
            collectedItems.Add(item);
        }
        // 道具屬於其他玩家，需要搶奪整串道具
        else {
            // 向對方玩家要求從指定 index 開始的道具清單
            List<Transform> newItems = itemData.owner.GetComponent<PlayerController>().OnPlayerItemStolen(transform, itemData.collectedItemIndex);

            // 設定新串接的第一個道具的跟隨對象
            itemFollow.follow = collectedItems.Count == 0 ? followPoint : collectedItems[^1];

            // 把整串道具串接到自己後面
            collectedItems.AddRange(newItems);

            // 重新編號所有道具的 index
            for (int i = 0; i < collectedItems.Count; i++) {
                collectedItems[i].GetComponent<ItemData>().collectedItemIndex = i;
            }
        }
        // 最後更新該道具的 owner 為自己
        itemData.owner = transform;
    }

    /// <summary>
    /// 被搶奪時，從指定 index 起的所有道具都被轉移給搶奪者
    /// </summary>
    public List<Transform> OnPlayerItemStolen(Transform stealer, int index) {
        List<Transform> removedItems = new();
        // 把所有要被搶的道具記錄下來，同時設定它們的新擁有者
        for (int i = index; i < collectedItems.Count; i++) {
            collectedItems[i].GetComponent<ItemData>().owner = stealer;
            removedItems.Add(collectedItems[i]);
        }

        // 從本地清單中移除這些被搶走的道具
        collectedItems.RemoveRange(index, collectedItems.Count - index);

        // 回傳被搶走的道具清單
        return removedItems;
    }

    /// <summary>
    /// 移除一個收集的物件
    /// </summary>
    public void CompeleItemCollection() {
        if (collectedItems.Count <= 0) return;
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        gameManager.PlayerIncreasePointByNumber(playerIndex, collectedItems.Count);
        ItemManager itemManager = FindFirstObjectByType<ItemManager>();
        foreach (var item in collectedItems) {
            itemManager.ItemCollected(item.gameObject);
            Destroy(item.gameObject);
        }
        collectedItems.Clear();
    }
}
