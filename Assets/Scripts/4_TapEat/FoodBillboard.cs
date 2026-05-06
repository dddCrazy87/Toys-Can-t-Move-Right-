using UnityEngine;

/// <summary>
/// 讓食物 Sprite 永遠面朝相機（Billboard 效果）
/// 掛在有 SpriteRenderer 的食物物件上
/// </summary>
public class FoodBillboard : MonoBehaviour
{
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCam != null)
        {
            // 讓 Sprite 面朝相機
            transform.rotation = Quaternion.LookRotation(
                transform.position - mainCam.transform.position,
                mainCam.transform.up
            );
        }
    }
}
