using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    
    public TextMeshProUGUI playerIDUI;

    public void Initialize(string id)
    {
        if (playerIDUI != null) playerIDUI.text = id;
    }

    public float speed = 5f;
    private Rigidbody rb;

    private Vector3 movement;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        speed *= -1;

        // 初始化玩家位置歷史
        for (int i = 0; i < trailLength; i++) {
            positionHistory.Enqueue(transform.position);
        }
    }

    public Transform playerModel;

    void Update()
    {
        // 測試時使用鍵盤，未來可用手機替代
        movement.x = Input.GetAxis("Horizontal");
        movement.z = Input.GetAxis("Vertical");

        // 正規化移動向量，確保對角線移動不會更快
        if (movement.magnitude > 1) {
            movement.Normalize();
        }

         // 使用 atan2 計算旋轉角度，這樣可以處理所有方向
        if (movement.magnitude > 0) {
            // 計算角度
            float angle = Mathf.Atan2(-movement.x, -movement.z) * Mathf.Rad2Deg;
            // 根據計算的角度設置旋轉
            playerModel.rotation = Quaternion.Euler(0, angle, 0);
        }

        // 更新每個收集到的道具的位置
        UpdateCollectedItemsPositions();
    }

    public Transform itemFollowPoint;
    // 記錄玩家移動軌跡
    private Queue<Vector3> positionHistory = new Queue<Vector3>();
    // 記錄軌跡的長度
    private const int trailLength = 1000;

    void FixedUpdate()
    {
        rb.linearVelocity = movement * speed;

        // 更新位置歷史
        positionHistory.Enqueue(itemFollowPoint.position);
        if (positionHistory.Count > trailLength) {
            positionHistory.Dequeue();
        }
    }

    // 此玩家收集的道具
    private List<GameObject> collectedItems = new List<GameObject>();

    // 添加新收集的道具
    public void AddItem(GameObject item)
    {
        item.transform.parent = transform;
        collectedItems.Add(item);
    }

    // 獲取某個時間點前的位置
    public Vector3 GetPositionInPast(int framesAgo)
    {
        framesAgo = Mathf.Clamp(framesAgo, 0, positionHistory.Count - 1);
        return positionHistory.ToArray()[positionHistory.Count - 1 - framesAgo];
    }


    // 更新所有收集的道具位置
    private void UpdateCollectedItemsPositions()
    {
        for (int i = 0; i < collectedItems.Count; i++)
        {
            // 計算每個道具應該在的位置
            // 每個道具都跟隨著玩家幾幀之前的位置
            // 調整這個值來改變跟隨的緊密程度
            int framesAgo = (i + 1) * 5;
            Vector3 targetPosition = GetPositionInPast(framesAgo);
            
            // 平滑移動道具到目標位置
            // collectedItems[i].transform.position = Vector3.Lerp(
            //     collectedItems[i].transform.position,
            //     targetPosition,
            //     Time.deltaTime * 5f
            // );

            collectedItems[i].GetComponent<Item>().lerpMoveToPosition(targetPosition);
        }
    }
}
