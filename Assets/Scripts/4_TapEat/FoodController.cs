using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum FoodType
{
    Normal,     // 普通食物，吃了得分
    Golden,     // 金色食物，吃了高分
    Trash       // 不能吃的，吃了扣分，要滑動丟棄
}

[System.Serializable]
public class FoodEntry
{
    public Sprite sprite;
    public FoodType type = FoodType.Normal;
    [Tooltip("咬幾口吃完（Trash 類型無效）")]
    public int bites = 5;
    [Tooltip("得分（Trash 為負分）")]
    public int score = 1;
}

public class FoodController : MonoBehaviour
{
    [Header("Settings")]
    public int playerIndex = 0;

    [Header("Food Pool")]
    public FoodEntry[] foodPool;

    [Header("Bite Mask")]
    public GameObject biteMaskPrefab;
    public float biteRadius = 0.3f;
    public float foodRadius = 0.5f;

    [Header("References")]
    public SpriteRenderer foodSpriteRenderer;
    public TextMeshPro bitesText;  // 顯示剩餘咬口數

    // 當前食物狀態
    [HideInInspector] public FoodEntry currentFood;
    private int currentBiteCount = 0;
    private int currentFoodIndex = 0;
    private List<GameObject> activeBiteMasks = new();
    private Vector3 originalScale;
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

        baseSortingOrder = playerIndex * 20;
        foodSpriteRenderer.sortingOrder = baseSortingOrder;
        foodSpriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;

        if (foodPool != null && foodPool.Length > 0)
        {
            currentFoodIndex = 0;
            ServeFood(currentFoodIndex);
        }
    }

    /// <summary>
    /// 咬一口。回傳 true 表示吃完了。
    /// </summary>
    public bool Bite()
    {
        if (foodSpriteRenderer == null || currentFood == null) return false;

        // 不能吃的食物被點擊 → 扣分但不算吃完
        if (currentFood.type == FoodType.Trash)
        {
            return false; // TapEatGameManager 會處理扣分
        }

        currentBiteCount++;
        UpdateBitesText();

        float angle = (currentBiteCount - 1) * (360f / currentFood.bites);
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

        if (currentBiteCount >= currentFood.bites)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 丟棄當前食物，上下一道
    /// </summary>
    public void Discard()
    {
        ServeNextFood();
    }

    /// <summary>
    /// 取得當前食物的得分
    /// </summary>
    public int GetCurrentScore()
    {
        return currentFood?.score ?? 0;
    }

    /// <summary>
    /// 當前食物是否為不能吃的
    /// </summary>
    public bool IsTrash()
    {
        return currentFood != null && currentFood.type == FoodType.Trash;
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

            // 下一道食物（隨機從 pool 裡選）
            if (foodPool != null && foodPool.Length > 0)
            {
                currentFoodIndex = Random.Range(0, foodPool.Length);
            }
            ServeFood(currentFoodIndex);

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

    private void ServeFood(int index)
    {
        if (foodPool == null || index >= foodPool.Length || foodSpriteRenderer == null) return;

        currentFood = foodPool[index];
        foodSpriteRenderer.sprite = currentFood.sprite;
        currentBiteCount = 0;
        UpdateBitesText();
    }

    private void UpdateBitesText()
    {
        if (bitesText == null) return;

        if (currentFood == null)
        {
            bitesText.text = "";
            return;
        }

        if (currentFood.type == FoodType.Trash)
        {
            bitesText.text = "✕";
            bitesText.color = Color.red;
        }
        else
        {
            int remaining = currentFood.bites - currentBiteCount;
            bitesText.text = $"×{remaining}";
            bitesText.color = currentFood.type == FoodType.Golden ? new Color(1f, 0.84f, 0f) : Color.white;
        }
    }
}
