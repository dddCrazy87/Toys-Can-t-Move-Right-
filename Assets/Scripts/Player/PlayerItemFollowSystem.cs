using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理玩家及其跟隨道具的系統
/// </summary>
public class PlayerItemFollowSystem : MonoBehaviour
{
    [Header("跟隨設置")]
    [Tooltip("添加新道具時的距離偏移量")]
    public float distanceBetweenItems = 1.0f;

    [Tooltip("道具關節的力度，控制跟隨緊密程度")]
    public float jointSpring = 20f;
    
    [Tooltip("道具關節的阻尼，控制跟隨平滑度")]
    public float jointDamper = 2f;

    [Tooltip("軌跡點之間的最小距離")]
    public float minPathPointDistance = 0.1f;

    [Tooltip("軌跡歷史記錄最大數量")]
    public int maxPathPoints = 100;

    // 存儲玩家移動軌跡的列表
    private List<Vector3> pathPoints = new List<Vector3>();
    
    // 當前跟隨的所有道具
    private List<Transform> followingItems = new List<Transform>();
    
    // 道具的目標位置列表（用於平滑跟隨）
    private List<Vector3> itemTargetPositions = new List<Vector3>();

    private void Update()
    {
        // 記錄玩家移動軌跡
        RecordPath();
        
        // 更新所有道具的目標位置
        UpdateItemTargets();
        
        // 確保所有道具保持在適當的位置和旋轉
        EnforceItemAlignment();
    }
    
    /// <summary>
    /// 強制所有道具保持正確的對齊和間距
    /// </summary>
    private void EnforceItemAlignment()
    {
        if (followingItems.Count == 0)
            return;
            
        // 處理第一個道具 - 確保它正確跟隨在玩家後方
        Transform firstItem = followingItems[0];
        if (firstItem != null)
        {
            // 當玩家停止移動時，確保第一個道具在玩家正後方
            if (pathPoints.Count <= 1 || Vector3.Distance(pathPoints[0], pathPoints[1]) < 0.01f)
            {
                Vector3 desiredPos = transform.position - transform.forward * distanceBetweenItems;
                desiredPos.y = transform.position.y; // 保持相同高度
                
                // 使用物理方式移動道具
                Rigidbody rb = firstItem.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // 計算需要施加的力以達到目標位置
                    Vector3 moveDirection = (desiredPos - firstItem.position);
                    if (moveDirection.magnitude > 0.1f)
                    {
                        rb.AddForce(moveDirection * 10f, ForceMode.Acceleration);
                    }
                }
                
                // 確保朝向與玩家一致
                firstItem.rotation = transform.rotation;
            }
        }
        
        // 處理其餘道具 - 確保它們正確跟隨前一個道具
        for (int i = 1; i < followingItems.Count; i++)
        {
            if (followingItems[i] == null || followingItems[i-1] == null)
                continue;
                
            Transform currentItem = followingItems[i];
            Transform prevItem = followingItems[i-1];
            
            // 計算理想位置 - 前一個道具的正後方
            Vector3 desiredPos = prevItem.position - prevItem.forward * distanceBetweenItems;
            desiredPos.y = transform.position.y; // 保持與玩家相同的高度
            
            // 使用物理方式移動道具
            Rigidbody rb = currentItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // 如果偏離太遠，施加更強的力
                Vector3 moveDirection = (desiredPos - currentItem.position);
                if (moveDirection.magnitude > 0.2f)
                {
                    rb.AddForce(moveDirection * 15f, ForceMode.Acceleration);
                }
            }
            
            // 確保朝向與前一個道具一致
            currentItem.rotation = prevItem.rotation;
        }
    }

    /// <summary>
    /// 記錄玩家的移動軌跡
    /// </summary>
    private void RecordPath()
    {
        if (pathPoints.Count == 0 || Vector3.Distance(transform.position, pathPoints[0]) > minPathPointDistance)
        {
            // 在軌跡前端添加新的位置點
            pathPoints.Insert(0, transform.position);
            
            // 限制軌跡點的最大數量
            if (pathPoints.Count > maxPathPoints)
            {
                pathPoints.RemoveAt(pathPoints.Count - 1);
            }
        }
    }

    /// <summary>
    /// 更新所有道具的目標位置
    /// </summary>
    private void UpdateItemTargets()
    {
        // 確保目標位置列表大小與道具數量相匹配
        while (itemTargetPositions.Count < followingItems.Count)
        {
            itemTargetPositions.Add(transform.position);
        }

        // 為每個道具計算目標位置
        for (int i = 0; i < followingItems.Count; i++)
        {
            float distance = (i + 1) * distanceBetweenItems;
            Vector3 targetPos = CalculatePositionAlongPath(distance);
            itemTargetPositions[i] = targetPos;
        }
    }

    /// <summary>
    /// 計算沿著軌跡的特定距離處的位置
    /// </summary>
    private Vector3 CalculatePositionAlongPath(float distance)
    {
        float currentDistance = 0f;
        
        // 如果軌跡為空或距離為0，則返回玩家位置
        if (pathPoints.Count <= 1 || distance <= 0)
            return transform.position;

        // 沿著軌跡行走指定的距離
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            float segmentLength = Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
            
            if (currentDistance + segmentLength >= distance)
            {
                // 計算在當前路徑段上的插值
                float t = (distance - currentDistance) / segmentLength;
                return Vector3.Lerp(pathPoints[i], pathPoints[i + 1], t);
            }
            
            currentDistance += segmentLength;
        }
        
        // 如果距離超出軌跡總長度，則返回軌跡的最後一個點
        return pathPoints[pathPoints.Count - 1];
    }

    /// <summary>
    /// 添加新的跟隨道具
    /// </summary>
    public void AddItem(GameObject itemPrefab)
    {
        // 決定新道具的生成位置
        Vector3 spawnPosition;
        Transform parentTransform;
        
        if (followingItems.Count == 0)
        {
            // 第一個道具生成在玩家正後方
            spawnPosition = transform.position - transform.forward * distanceBetweenItems;
            parentTransform = transform;
        }
        else
        {
            // 後續道具生成在前一個道具的正後方
            Transform lastItem = followingItems[followingItems.Count - 1];
            spawnPosition = lastItem.position - lastItem.forward * distanceBetweenItems;
            parentTransform = lastItem;
        }
        
        // 實例化道具，使其面向與父物體相同
        GameObject newItem = Instantiate(itemPrefab, spawnPosition, parentTransform.rotation);
        
        // 設置父子關係
        newItem.transform.SetParent(parentTransform);
        
        // 確保新道具位置正確
        newItem.transform.localPosition = new Vector3(0, 0, -distanceBetweenItems);
        
        // 確保新道具Y座標與玩家相同，避免高度差異
        newItem.transform.position = new Vector3(
            newItem.transform.position.x,
            transform.position.y,
            newItem.transform.position.z
        );
        
        // 添加並設置關節組件
        ConfigurableJoint joint = newItem.AddComponent<ConfigurableJoint>();
        ConfigureItemJoint(joint, parentTransform);
        
        // 確保道具的剛體配置正確
        Rigidbody itemRb = newItem.GetComponent<Rigidbody>();
        if (itemRb != null)
        {
            // 根據道具大小調整質量，避免大小道具行為不一致
            float itemScale = newItem.transform.localScale.magnitude;
            itemRb.mass = Mathf.Clamp(itemScale, 0.5f, 5f);
            
            // 統一拖曳力，使所有道具受力行為一致
            itemRb.linearDamping = 5f;
            itemRb.angularDamping = 10f;
        }
        
        // 將新道具添加到跟隨列表中
        followingItems.Add(newItem.transform);
        
        Debug.Log($"已添加新道具。當前道具數量: {followingItems.Count}");
    }

    /// <summary>
    /// 配置道具關節的物理屬性
    /// </summary>
    private void ConfigureItemJoint(ConfigurableJoint joint, Transform connectedBody)
    {
        // 設置關節的連接物體
        joint.connectedBody = connectedBody.GetComponent<Rigidbody>();
        
        // 配置關節屬性 - 使用Locked來固定Y軸位置，避免跳動
        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Limited;
        
        // 更嚴格限制旋轉，保持方向一致
        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;
        
        // 提高彈簧和阻尼屬性，使跟隨更精確
        SoftJointLimitSpring spring = new SoftJointLimitSpring
        {
            spring = jointSpring * 2f, // 增加剛性
            damper = jointDamper * 1.5f // 增加阻尼
        };
        
        joint.linearLimitSpring = spring;
        
        // 設置更嚴格的線性限制
        SoftJointLimit limit = new SoftJointLimit
        {
            limit = distanceBetweenItems * 0.8f, // 增加限制精確度
            bounciness = 0f
        };
        
        joint.linearLimit = limit;
        
        // 強制道具保持與連接體相同的旋轉
        joint.configuredInWorldSpace = false;
        joint.rotationDriveMode = RotationDriveMode.XYAndZ; // 改為XYAndZ以更精確控制旋轉
        
        JointDrive slerpDrive = new JointDrive
        {
            positionSpring = 500f, // 大幅增加，確保方向對齊
            positionDamper = 50f,
            maximumForce = 2000f
        };
        
        joint.slerpDrive = slerpDrive;
        
        // 設置連接錨點位置 - 確保道具排在前一個物體的正後方
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = Vector3.zero; // 本體連接點在中心
        joint.connectedAnchor = new Vector3(0, 0, -distanceBetweenItems); // 連接到前一個物體的正後方
    }

    /// <summary>
    /// 從一個玩家轉移道具到另一個玩家
    /// </summary>
    public void TransferItemsToAnotherPlayer(PlayerItemFollowSystem targetPlayer, int fromIndex)
    {
        if (fromIndex < 0 || fromIndex >= followingItems.Count)
            return;
        
        // 獲取要轉移的道具
        Transform itemToTransfer = followingItems[fromIndex];
        
        // 獲取需要轉移的所有道具（當前索引及其後續的所有道具）
        List<Transform> itemsToTransfer = new List<Transform>();
        for (int i = fromIndex; i < followingItems.Count; i++)
        {
            itemsToTransfer.Add(followingItems[i]);
        }
        
        // 從當前玩家的列表中移除這些道具
        followingItems.RemoveRange(fromIndex, followingItems.Count - fromIndex);
        
        // 在新玩家中添加這些道具
        foreach (Transform item in itemsToTransfer)
        {
            // 先移除原有的關節
            ConfigurableJoint oldJoint = item.GetComponent<ConfigurableJoint>();
            if (oldJoint != null)
            {
                Destroy(oldJoint);
            }
            
            // 添加到目標玩家
            targetPlayer.AddExistingItem(item.gameObject);
        }
        
        Debug.Log($"已轉移 {itemsToTransfer.Count} 個道具到另一個玩家");
    }

    /// <summary>
    /// 添加已存在的道具到跟隨系統
    /// </summary>
    public void AddExistingItem(GameObject item)
    {
        // 決定父物體
        Transform parentTransform;
        if (followingItems.Count == 0)
        {
            parentTransform = transform;
        }
        else
        {
            parentTransform = followingItems[followingItems.Count - 1];
        }
        
        // 設置新的父子關係
        item.transform.SetParent(parentTransform);
        
        // 添加並設置關節組件
        ConfigurableJoint joint = item.AddComponent<ConfigurableJoint>();
        ConfigureItemJoint(joint, parentTransform);
        
        // 將道具添加到跟隨列表
        followingItems.Add(item.transform);
    }
}