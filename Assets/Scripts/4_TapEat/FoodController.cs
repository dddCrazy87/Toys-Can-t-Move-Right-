using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodController : MonoBehaviour
{
    [Header("Settings")]
    public int bitesPerFood = 8;
    public int playerIndex = 0;  // 由 TapEatGameManager 設定

    [Header("Food Sprites")]
    public Sprite[] foodSprites;

    [Header("Bite Mask")]
    public GameObject biteMaskPrefab;
    public float biteRadius = 0.3f;
    public float foodRadius = 0.5f;

    [Header("References")]
    public SpriteRenderer foodSpriteRenderer;

    private int currentBiteCount = 0;
    private List<GameObject> activeBiteMasks = new();
    private int currentFoodIndex = 0;
    private Vector3 originalScale;

    // 每個玩家用不同的 sortingOrder 範圍來隔離 SpriteMask
    private int baseSortingOrder;

    void Start()
    {
        if (foodSpriteRenderer == null)
            foodSpriteRenderer = GetComponent<SpriteRenderer>();
        if (foodSpriteRenderer == null)
            foodSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (foodSpriteRenderer == null)
        {
            Debug.LogError($"[FoodController] {gameObject.name}: 找不到 SpriteRenderer！");
            return;
        }

        originalScale = foodSpriteRenderer.transform.localScale;

        // 每個玩家的食物用不同的 sortingOrder（間隔 20）
        baseSortingOrder = playerIndex * 20;
        foodSpriteRenderer.sortingOrder = baseSortingOrder;
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

        float angle = (currentBiteCount - 1) * (360f / bitesPerFood);
        float rad = angle * Mathf.Deg2Rad;
        Vector3 biteOffset = new Vector3(
            Mathf.Cos(rad) * foodRadius,
            Mathf.Sin(rad) * foodRadius,
            -0.01f
        );

        if (biteMaskPrefab != null)
        {
            GameObject mask = Instantiate(biteMaskPrefab, transform);
            mask.transform.localPosition = biteOffset;
            mask.transform.localScale = Vector3.one * biteRadius * 2;

            // 讓咬痕遮罩只影響同一 sortingOrder 範圍的食物
            SpriteMask sm = mask.GetComponent<SpriteMask>();
            if (sm == null) sm = mask.GetComponentInChildren<SpriteMask>();
            if (sm != null)
            {
                sm.isCustomRangeActive = true;
                sm.frontSortingOrder = baseSortingOrder + 1;
                sm.backSortingOrder = baseSortingOrder - 1;
            }

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

            // 恢復原始大小
            foodSpriteRenderer.transform.localScale = originalScale;

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
