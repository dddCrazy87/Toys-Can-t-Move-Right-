using UnityEngine;
using System.Collections;

public class CameraMovement : MonoBehaviour
{
    // 目標Transform
    public Transform target;
    
    // 移動速度 (數值越小移動越慢)
    public float moveSpeed = 5.0f;
    
    // 旋轉速度 (數值越小旋轉越慢)
    public float rotationSpeed = 5.0f;
    
    // 是否在移動期間看向目標
    public bool lookAtTarget = true;
    
    // 移動完成時的事件委託
    public delegate void MovementCompleteDelegate();
    public event MovementCompleteDelegate OnMovementComplete;
    
    // 移動完成的距離閾值
    public float arrivalThreshold = 0.1f;
    
    // 是否正在移動中
    private bool isMoving = false;
    
    void Update()
    {
        // 如果有設定目標且正在移動中
        if (target != null && isMoving)
        {
            // 計算目前與目標的距離
            float distance = Vector3.Distance(transform.position, target.position);
            
            // 如果距離小於閾值，表示到達目標
            if (distance < arrivalThreshold)
            {
                // 直接設定位置和旋轉到目標
                transform.position = target.position;
                if (lookAtTarget)
                {
                    transform.rotation = target.rotation;
                }
                
                // 停止移動
                isMoving = false;
                
                // 觸發完成事件
                if (OnMovementComplete != null)
                {
                    OnMovementComplete();
                }
            }
            else
            {
                // 平滑移動到目標位置
                transform.position = Vector3.Lerp(transform.position, target.position, moveSpeed * Time.deltaTime);
                
                // 如果需要，平滑旋轉到目標旋轉
                if (lookAtTarget)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, rotationSpeed * Time.deltaTime);
                }
            }
        }
    }
    
    // 開始移動到目標
    public void MoveToTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            isMoving = true;
        }
    }
    
    // 停止移動
    public void StopMoving()
    {
        isMoving = false;
    }
    
    // 移動到目標並等待完成的協程
    public IEnumerator MoveToTargetCoroutine(Transform newTarget)
    {
        MoveToTarget(newTarget);
        
        // 等待移動完成
        while (isMoving)
        {
            yield return null;
        }
    }
}