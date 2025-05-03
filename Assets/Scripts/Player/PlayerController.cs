using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public TextMeshProUGUI playerIDUI;
    public void Initialize(string id) {
        if (playerIDUI != null) playerIDUI.text = id;
    }

    [Header("移動設置")]
    [Tooltip("玩家移動速度")]
    public float moveSpeed = 5f;
    
    [Tooltip("玩家旋轉速度")]
    public float rotateSpeed = 15f;
    
    [Header("道具設置")]
    [Tooltip("要收集的道具預製體")]
    public GameObject itemPrefab;


    // 跟隨系統組件
    private PlayerItemFollowSystem followSystem;
    private Rigidbody rb;
    private void Awake() {
        rb = GetComponent<Rigidbody>();
        ConfigureRigidbody();
        followSystem = GetComponent<PlayerItemFollowSystem>();
        moveSpeed *= -1;
    }


    public Transform playerModel;
    private Vector3 movement;
    void Update() {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        movement = new Vector3(horizontal, 0f, vertical).normalized;
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
    private void OnTriggerEnter(Collider other) {
        // 檢查是否碰到了可收集的道具
        if (other.CompareTag("Collectable")) {
            if (other.transform.parent != null) return;
            CollectItem(other.gameObject);
        }
    }

    /// <summary>
    /// 收集道具的處理邏輯
    /// </summary>
    private void CollectItem(GameObject item)
    {
        // 禁用道具的碰撞器，防止重複收集
        // Collider itemCollider = item.GetComponent<Collider>();
        // if (itemCollider != null) {
        //     itemCollider.enabled = false;
        // }
        
        // 添加到跟隨系統
        if (followSystem != null) {
            followSystem.AddExistingItem(item);
        }
    }

    private List<GameObject> collectedItems = new List<GameObject>();

    public void AddItem(GameObject item) {
        item.transform.parent = transform;
        collectedItems.Add(item);
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
