using UnityEngine;

/// <summary>
/// 讓食物 Sprite 平躺在盤子上，微微朝相機傾斜以便看到全貌
/// 掛在有 SpriteRenderer 的食物物件上
/// </summary>
public class FoodBillboard : MonoBehaviour
{
    [Header("傾斜角度")]
    public float tiltAngle = 60f;  // 朝相機傾斜的角度（0=完全平躺，90=完全面朝相機）

    void Start()
    {
        ApplyRotation();
    }

    void ApplyRotation()
    {
        // Sprite 預設面朝 Z 軸正方向
        // 先讓它平躺（X 旋轉 90 度面朝上），再朝相機微傾
        transform.localRotation = Quaternion.Euler(90f - tiltAngle, 0f, 0f);
    }
}
