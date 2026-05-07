using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodController : MonoBehaviour
{
    [Header("Settings")]
    public int bitesPerFood = 8;

    [Header("Food Sprites")]
    public Sprite[] foodSprites;

    [Header("Bite Mask")]
    public GameObject biteMaskPrefab;
    public float biteRadius = 0.3f;
    public float foodRadius = 0.5f;

    [Header("References")]
    public SpriteRenderer foodSpriteRenderer;  // 可手動拖，沒拖會自動找

    private int currentBiteCount = 0;
    private List<GameObject> activeBiteMasks = new();
    private int currentFoodIndex = 0;

    void Start()
    {
        // 自動找 SpriteRenderer（如果沒手動拖的話）
        if (foodSpriteRenderer == null)
        {
            foodSpriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (foodSpriteRenderer == null)
        {
            foodSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (foodSpriteRenderer == null)
        {
            Debug.LogError($"[FoodController] {gameObject.name}: 找不到 SpriteRenderer！請在 Food 物件上加 SpriteRenderer 元件。");
            return;
        }

        // 設定 Mask Interaction 讓遮罩生效
        foodSpriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;

        if (foodSprites != null && foodSprites.Length > 0)
        {
            currentFoodIndex = 0;
            ServeFoodAtIndex(currentFoodIndex);
        }
    }

    public bool Bite()
    {
        if (foodSpriteRenderer == null) return false;

        currentBiteCount++;

        // 咬痕位置（均勻分佈在圓形邊緣）
        float angle = (currentBiteCount - 1) * (360f / bitesPerFood);
        float rad = angle * Mathf.Deg2Rad;
        Vector3 biteOffset = new Vector3(
            Mathf.Cos(rad) * foodRadius,
            Mathf.Sin(rad) * foodRadius,
            -0.01f
        );

        // 生成咬痕遮罩
        if (biteMaskPrefab != null)
        {
            GameObject mask = Instantiate(biteMaskPrefab, transform);
            mask.transform.localPosition = biteOffset;
            mask.transform.localScale = Vector3.one * biteRadius * 2;
            activeBiteMasks.Add(mask);
        }

        if (currentBiteCount >= bitesPerFood)
        {
            return true;
        }

        return false;
    }

    public void ServeNextFood()
    {
        StartCoroutine(ServeNextFoodRoutine());
    }

    private IEnumerator ServeNextFoodRoutine()
    {
        // 舊食物淡出
        if (foodSpriteRenderer != null)
        {
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

            // 清除咬痕
            foreach (var mask in activeBiteMasks)
            {
                if (mask != null) Destroy(mask);
            }
            activeBiteMasks.Clear();
            currentBiteCount = 0;

            // 恢復大小
            foodSpriteRenderer.transform.localScale = Vector3.one;

            // 換下一盤食物（照順序循環）
            if (foodSprites != null && foodSprites.Length > 0)
            {
                currentFoodIndex = (currentFoodIndex + 1) % foodSprites.Length;
            }
            ServeFoodAtIndex(currentFoodIndex);

            // 新食物掉落動畫
            foodSpriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
            Vector3 endPos = foodSpriteRenderer.transform.localPosition;
            Vector3 startPos = endPos + Vector3.up * 0.5f;
            foodSpriteRenderer.transform.localPosition = startPos;

            float bounceTime = 0.3f;
            elapsed = 0f;
            while (elapsed < bounceTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / bounceTime;
                float bounce = 1f - Mathf.Pow(1f - t, 3f);
                foodSpriteRenderer.transform.localPosition = Vector3.Lerp(startPos, endPos, bounce);
                yield return null;
            }
            foodSpriteRenderer.transform.localPosition = endPos;
        }
    }

    private void ServeFoodAtIndex(int index)
    {
        if (foodSprites != null && index < foodSprites.Length && foodSpriteRenderer != null)
        {
            foodSpriteRenderer.sprite = foodSprites[index];
        }
    }
}
