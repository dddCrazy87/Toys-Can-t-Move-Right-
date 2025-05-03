using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    private Rigidbody rb;
    private void Start() {
        rb = GetComponent<Rigidbody>();
    }

    public void lerpMoveToPosition(Vector3 targetPosition) {
        Vector3 newPosition = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * 30f
        );
        rb.MovePosition(newPosition);
    }

    private bool isCollected = false;
    private void OnTriggerEnter(Collider other) {
        
        if (other.CompareTag("Player"))
        {
            if (isCollected) return;
            // 玩家碰到道具，通知道具管理器
            ItemManager itemManager = FindFirstObjectByType<ItemManager>();
            if (itemManager != null) {
                itemManager.ItemCollected(gameObject);
            }
            
            // 通知玩家收集到道具 - 直接將道具添加到碰撞的玩家
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null) {
                playerController.AddItem(gameObject);
                isCollected = true;
            }
        }
    }
}