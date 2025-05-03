using UnityEngine;

/// <summary>
/// 用於強制道具對齊的輔助腳本，附加到每個道具上
/// </summary>
public class ItemAlignmentHelper : MonoBehaviour
{
    [Tooltip("強制對齊的強度")]
    public float alignmentForce = 10f;
    
    [Tooltip("允許的最大偏移距離")]
    public float maxOffset = 0.5f;
    
    private Rigidbody rb;
    private Transform parentTransform;
    private float desiredDistance;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // 獲取理想距離
        PlayerItemFollowSystem followSystem = GetComponentInParent<PlayerItemFollowSystem>();
        if (followSystem != null)
        {
            desiredDistance = followSystem.distanceBetweenItems;
        }
        else
        {
            desiredDistance = 1f; // 默認值
        }
    }
    
    private void Start()
    {
        // 獲取父物體
        parentTransform = transform.parent;
    }
    
    private void FixedUpdate()
    {
        if (parentTransform == null || rb == null)
            return;
            
        // 計算理想位置 - 父物體的正後方
        Vector3 desiredPosition = parentTransform.position - parentTransform.forward * desiredDistance;
        
        // 保持Y軸高度不變
        desiredPosition.y = parentTransform.position.y;
        
        // 計算當前偏移量
        Vector3 offset = desiredPosition - transform.position;
        
        // 如果偏移超過閾值，應用修正力
        if (offset.magnitude > maxOffset)
        {
            // 計算需要的力以移動到理想位置
            Vector3 correctionForce = offset.normalized * alignmentForce * (offset.magnitude - maxOffset);
            rb.AddForce(correctionForce, ForceMode.Acceleration);
        }
        
        // 確保朝向與父物體一致
        if (transform.rotation != parentTransform.rotation)
        {
            Quaternion targetRotation = Quaternion.Slerp(
                transform.rotation, 
                parentTransform.rotation, 
                Time.fixedDeltaTime * 10f
            );
            
            rb.MoveRotation(targetRotation);
        }
    }
    
    private void OnDrawGizmos()
    {
        // 視覺化理想位置（僅在編輯器中）
        if (parentTransform != null)
        {
            Vector3 desiredPosition = parentTransform.position - parentTransform.forward * desiredDistance;
            desiredPosition.y = parentTransform.position.y;
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(desiredPosition, 0.2f);
            Gizmos.DrawLine(transform.position, desiredPosition);
        }
    }
}