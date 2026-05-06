using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 控制單個玩家的食物顯示與被咬效果
/// 掛在每個玩家的 Food 物件上
/// </summary>
public class FoodController : MonoBehaviour
{
    [Header("Settings")]
    public int bitesPerFood = 8;

    [Header("Food Sprites")]
    public Sprite[] foodSprites;  // 食物素材池（Inspector 拖入）

    [Header("Bite Mask")]
    public GameObject biteMaskPrefab;  // 咬痕遮罩 Prefab
    public float biteRadius = 0.3f;    // 咬痕大小
    public float foodRadius = 0.5f;    // 食物半徑

    [Header("References")]
    public SpriteRenderer foodSpriteRenderer;  // 食物的 SpriteRenderer

    private int currentBiteCount = 0;
    private List<GameObject> activeBiteMasks = new();
    private int currentFoodIndex = 0;

    void Start()
    {
        if (foodSprites != null && foodSprites.Length > 0)
        {
            ServeFoodAtIndex(0);
        }
    }

    /// <summary>
    /// 被咬一口，回傳是否吃完整盤
    /// </summary>
    public bool Bite()
    {
        currentBiteCount++;

        // 計算咬痕位置（均勻分佈在圓形邊緣）
        float angle = (currentBiteCount - 1) * (360f / bitesPerFood);
        float rad = angle * Mathf.Deg2Rad;
        Vector3 biteOffset = new Vector3(
            Mathf.Cos(rad) * foodRadius,
            Mathf.Sin(rad) * foodRadius,
            -0.01f  // 稍微在食物前面
        );

        // 生成咬痕遮罩
        if (biteMaskPrefab != null)
        {
            GameObject mask = Instantiate(biteMaskPrefab, transform);
            mask.transform.localPosition = biteOffset;
            mask.transform.localScale = Vector3.one * biteRadius * 2;
            activeBiteMasks.Add(mask);
        }

        // 食物縮小效果（簡單視覺回饋）
        float scaleRatio = 1f - (currentBiteCount * 0.05f);
        foodSpriteRenderer.transform.localScale = Vector3.one * Mathf.Max(scaleRatio, 0.3f);

        // 吃完一盤
        if (currentBiteCount >= bitesPerFood)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 換下一盤食物
    /// </summary>
    public void ServeNextFood()
    {
        StartCoroutine(ServeNextFoodRoutine());
    }

    private IEnumerator ServeNextFoodRoutine()
    {
        // 舊食物淡出
        float fadeTime = 0.2f;
        float elapsed = 0f;
        Color originalColor = foodSpriteRenderer.color;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            foodSpriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        // 清除所有咬痕
        foreach (var mask in activeBiteMasks)
        {
            if (mask != null) Destroy(mask);
        }
        activeBiteMasks.Clear();
        currentBiteCount = 0;

        // 恢復食物大小
        foodSpriteRenderer.transform.localScale = Vector3.one;

        // 換新食物
        currentFoodIndex = (currentFoodIndex + 1) % foodSprites.Length;
        ServeFoodAtIndex(currentFoodIndex);

        // 新食物掉落動畫
        foodSpriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
        Vector3 startPos = foodSpriteRenderer.transform.localPosition + Vector3.up * 0.5f;
        Vector3 endPos = foodSpriteRenderer.transform.localPosition;
        foodSpriteRenderer.transform.localPosition = startPos;

        float bounceTime = 0.3f;
        elapsed = 0f;
        while (elapsed < bounceTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / bounceTime;
            // 簡單彈跳：先快後慢
            float bounce = 1f - Mathf.Pow(1f - t, 3f);
            foodSpriteRenderer.transform.localPosition = Vector3.Lerp(startPos, endPos, bounce);
            yield return null;
        }
        foodSpriteRenderer.transform.localPosition = endPos;
    }

    private void ServeFoodAtIndex(int index)
    {
        if (foodSprites != null && index < foodSprites.Length)
        {
            foodSpriteRenderer.sprite = foodSprites[index];
        }
    }
}
