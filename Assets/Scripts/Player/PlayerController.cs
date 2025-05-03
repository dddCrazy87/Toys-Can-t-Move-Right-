using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public bool isP1 = false, isP2 = false;
    public TextMeshProUGUI playerIDUI;
    public void Initialize(string id) {
        if (playerIDUI != null) playerIDUI.text = id;
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
        ConfigureRigidbody();
        followPoint.position += new Vector3(0, 0, -itemFollowSpace);
        moveSpeed *= -1;
    }

    private Vector3 movement;
    void Update() {
        // float horizontal = Input.GetAxisRaw("Horizontal");
        // float vertical = Input.GetAxisRaw("Vertical");
        // movement = new Vector3(horizontal, 0f, vertical).normalized;

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
    [Header("道具設置")]
    [Tooltip("道具跟隨時的間距")]
    public float itemFollowSpace;
    [Tooltip("道具跟隨的點")]
    public Transform followPoint;
    List<Transform> collectedItems = new();
    private void CollectItem(Transform item)
    {
        ItemData itemData = item.GetComponent<ItemData>();

        ItemManager itemManager = FindFirstObjectByType<ItemManager>();
        //itemManager.ItemCollected(item.gameObject);
        Destroy(item.GetComponent<ItemFollow>());
        ItemFollow itemFollow = item.gameObject.AddComponent<ItemFollow>();

        if (itemData.owner == null) {
            itemFollow.maxDistance = itemFollowSpace;
            itemFollow.followSpeed = rotateSpeed;
            itemData.collectedItemIndex = collectedItems.Count;
            itemFollow.follow = collectedItems.Count == 0 ? followPoint : collectedItems[^1];
            collectedItems.Add(item);
        }
        else {
            List<Transform> newItems = itemData.owner.GetComponent<PlayerController>().OnPlayerItemStolen(transform, itemData.collectedItemIndex);
            itemFollow.follow = collectedItems.Count == 0 ? followPoint : collectedItems[^1];
            collectedItems.AddRange(newItems);
            for (int i = 0; i < collectedItems.Count; i++) {
                collectedItems[i].GetComponent<ItemData>().collectedItemIndex = i;
            }
        }
        itemData.owner = transform;
    }

    public List<Transform> OnPlayerItemStolen(Transform stealer, int index) {
        List<Transform> removedItems = new();
        for (int i = index; i < collectedItems.Count; i++) {
            collectedItems[i].GetComponent<ItemData>().owner = stealer;
            removedItems.Add(collectedItems[i]);
        }
        collectedItems.RemoveRange(index, collectedItems.Count - index);
        return removedItems;
    }

    /// <summary>
    /// 配置剛體的物理屬性
    /// </summary>
    private void ConfigureRigidbody()
    {
        // 設置為非運動學剛體，這樣可以接收物理力和進行碰撞檢測
        rb.isKinematic = false;
        
        // 設置合適的質量和拖曳力
        rb.mass = 5f;
        rb.linearDamping = 1f;
        rb.angularDamping = 5f;
        
        // 防止玩家休眠
        rb.sleepThreshold = 0f;
        
        // 設置插值模式，使運動更平滑
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        
        // 碰撞檢測設置
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        // 限制旋轉以防止不必要的翻滾
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
    }
}
