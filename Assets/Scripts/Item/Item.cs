using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    [Header("道具設置")]
    [Tooltip("道具的移動速度係數，影響跟隨的敏感度")]
    public float moveSpeed = 5f;
    
    [Tooltip("道具的旋轉速度係數")]
    public float rotateSpeed = 10f;
    
    // 道具的剛體組件
    private Rigidbody rb;
    
    // 目標位置（通過ConfigurableJoint自動控制）
    private Vector3 lastPosition;
    
    // 參考前一個物體(父物體)，用於計算朝向
    private Transform followTarget;

    private void Awake()
    {
        // 獲取剛體組件
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // 配置剛體屬性
        ConfigureRigidbody();
        
        // 記錄初始位置
        lastPosition = transform.position;
    }

    private void Start()
    {
        // 獲取跟隨目標（父物體）
        followTarget = transform.parent;
    }

    private void FixedUpdate()
    {
        if (followTarget != null)
        {
            // 計算移動方向
            Vector3 moveDirection = (transform.position - lastPosition).normalized;
            if (moveDirection != Vector3.zero)
            {
                // 平滑旋轉，使道具朝向移動方向
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotateSpeed * Time.fixedDeltaTime);
            }
        }
        
        // 記錄當前位置，用於下一幀計算移動方向
        lastPosition = transform.position;
    }

    /// <summary>
    /// 配置剛體的物理屬性
    /// </summary>
    private void ConfigureRigidbody()
    {
        // 設置為非運動學剛體，以保持物理碰撞
        rb.isKinematic = false;
        
        // 設置合適的質量和拖曳力 - 增加阻力以避免過度晃動
        rb.mass = 1f;
        rb.linearDamping = 5f; // 增加線性阻力
        rb.angularDamping = 10f; // 增加角阻力，減少旋轉
        
        // 防止道具休眠
        rb.sleepThreshold = 0f;
        
        // 設置插值模式，平滑運動
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        
        // 碰撞檢測設置
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        // 完全凍結旋轉，保持道具始終面向前方
        rb.constraints = RigidbodyConstraints.FreezeRotationX | 
                         RigidbodyConstraints.FreezeRotationY | 
                         RigidbodyConstraints.FreezeRotationZ |
                         RigidbodyConstraints.FreezePositionY; // 凍結Y軸位置，保持高度一致
        
        // 根據物體大小調整質量
        float objectScale = transform.localScale.magnitude;
        rb.mass = Mathf.Clamp(objectScale, 0.5f, 5f);
    }

    /// <summary>
    /// 處理碰撞事件（可擴展）
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        // 檢測與哪些物體發生碰撞
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log($"道具 {gameObject.name} 與玩家 {collision.gameObject.name} 發生碰撞");
            
            // 這裡可以添加碰撞時的效果或邏輯
            // 例如：檢查是否需要轉移道具所有權
            CheckForItemTransfer(collision.gameObject);
        }
    }

    /// <summary>
    /// 檢查是否需要轉移道具所有權
    /// </summary>
    private void CheckForItemTransfer(GameObject otherPlayer)
    {
        // 獲取當前的擁有者（根玩家）
        PlayerItemFollowSystem currentOwner = GetRootOwner();
        
        // 獲取碰撞的另一個玩家的跟隨系統
        PlayerItemFollowSystem targetPlayer = otherPlayer.GetComponent<PlayerItemFollowSystem>();
        
        // 確保目標玩家有跟隨系統且不是當前擁有者
        if (targetPlayer != null && targetPlayer != currentOwner)
        {
            // 檢查是否滿足轉移條件（這裡可以添加自定義的判斷邏輯）
            bool shouldTransfer = ShouldTransferToPlayer(targetPlayer);
            
            if (shouldTransfer)
            {
                // 找出當前道具在擁有者道具列表中的索引
                Transform rootTransform = transform;
                while (rootTransform.parent != null && rootTransform.parent.GetComponent<PlayerItemFollowSystem>() == null)
                {
                    rootTransform = rootTransform.parent;
                }
                
                // 執行轉移
                // 注意：實際使用時，你可能需要更複雜的邏輯來找出正確的索引
                // 這裡假設能找到父級關係
                if (currentOwner != null)
                {
                    int itemIndex = 0; // 這裡需要找到正確的索引
                    currentOwner.TransferItemsToAnotherPlayer(targetPlayer, itemIndex);
                }
            }
        }
    }

    /// <summary>
    /// 獲取道具的根擁有者（玩家）
    /// </summary>
    private PlayerItemFollowSystem GetRootOwner()
    {
        Transform current = transform;
        PlayerItemFollowSystem owner = null;
        
        // 向上遍歷物件層級直到找到PlayerItemFollowSystem組件
        while (current != null)
        {
            owner = current.GetComponent<PlayerItemFollowSystem>();
            if (owner != null)
                break;
                
            current = current.parent;
        }
        
        return owner;
    }

    /// <summary>
    /// 判斷是否應該將道具轉移給目標玩家（可自定義邏輯）
    /// </summary>
    private bool ShouldTransferToPlayer(PlayerItemFollowSystem targetPlayer)
    {
        // 這裡可以添加自定義的轉移條件
        // 例如：根據遊戲規則、碰撞的速度或角度等決定是否轉移
        
        // 簡單示例：只要碰撞就有機率轉移
        float transferChance = 0.2f; // 20%的轉移機率
        return Random.value < transferChance;
    }
}