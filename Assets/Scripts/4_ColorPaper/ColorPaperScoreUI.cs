using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ColorPaperScoreUI : MonoBehaviour
{
    [Header("長條圖容器（放在左上角）")]
    [SerializeField] private Transform barContainer;

    [Header("長條 Prefab")]
    [SerializeField] private GameObject barPrefab;
    // Prefab 結構：
    // - Root (有 LayoutElement)
    //   - Avatar (角色頭像 Image，固定在左側)
    //   - EnergyIcons (放在進度條上方)
    //     - Icon_0 ~ Icon_4 (Image，顏料罐 icon)
    //   - BarBackground (灰色圓角 Image)
    //     - BarFill (彩色圓角 Image，設定 Pivot 在左邊)
    //     - PercentText (TextMeshProUGUI，跟著 BarFill 移動)

    [Header("顏料 Icon 設定")]
    [SerializeField] private Sprite paintIconFull;   // 有顏料的 icon
    [SerializeField] private Sprite paintIconEmpty;  // 空的 icon（灰色）
    [SerializeField] private Color emptyIconColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

    [Header("正在消耗的 Icon 閃爍效果")]
    [SerializeField] private float blinkSpeed = 8f;           // 閃爍速度
    [SerializeField] private float blinkMinAlpha = 0.4f;      // 最小透明度
    [SerializeField] private float blinkMaxAlpha = 1f;        // 最大透明度

    [Header("顏色設定")]
    [SerializeField] private Color blueColor = new Color(0.2f, 0.4f, 1f, 1f);
    [SerializeField] private Color yellowColor = new Color(1f, 0.9f, 0.2f, 1f);
    [SerializeField] private Color greenColor = new Color(0.2f, 0.8f, 0.3f, 1f);
    [SerializeField] private Color redColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Header("角色頭像對照表")]
    [SerializeField] private List<SkinAvatarMapping> avatarMappings = new List<SkinAvatarMapping>();

    [System.Serializable]
    public class SkinAvatarMapping
    {
        public string skinName;  // 例如 "hat", "deer", "dog", "mouse", "wind-up"
        public Sprite blueAvatar;
        public Sprite yellowAvatar;
        public Sprite greenAvatar;
        public Sprite redAvatar;
    }

    /// <summary>
    /// 遞迴搜尋子物件
    /// </summary>
    Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    string GetFullPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    void LogAllChildren(Transform parent, string indent)
    {
        foreach (Transform child in parent)
        {
            Debug.Log($"{indent}{child.name} (Image: {child.GetComponent<Image>() != null})");
            LogAllChildren(child, indent + "  ");
        }
    }

    [Header("長條圖設定")]
    [SerializeField] private float maxBarWidth = 300f;      // 長條最大寬度（100% 時）
    [SerializeField] private float minBarWidth = 40f;       // 長條最小寬度（0% 時）
    [SerializeField] private float barLerpSpeed = 5f;       // 長條變化平滑速度
    [SerializeField] private float percentTextOffset = 10f; // 百分比文字相對於長條右邊緣的偏移

    [Header("排名動畫設定")]
    [SerializeField] private float rankSwapDuration = 0.3f; // 排名交換動畫時間
    [SerializeField] private float barHeight = 60f;         // 每個長條的高度（用於計算位置）
    [SerializeField] private float barSpacing = 10f;        // 長條間距

    private Dictionary<string, Color> colorMapping;
    private Dictionary<int, BarData> playerBars = new Dictionary<int, BarData>();
    private List<int> currentRanking = new List<int>();     // 目前排名順序
    private GameManager gameManager;

    private class BarData
    {
        public int playerId;
        public GameObject barObject;
        public RectTransform rootRect;
        public RectTransform barFillRect;
        public Image barFillImage;
        public RectTransform avatarRect;
        public Image avatarImage;
        public TextMeshProUGUI percentText;
        public RectTransform percentTextRect;  // 百分比文字的 RectTransform

        // 顏料 icon
        public Image[] energyIcons;  // 5 個顏料 icon
        public PaintEnergy paintEnergy;
        public PlayerController playerController;
        public string playerColorKey;  // 儲存玩家顏色 key
        public int activeIconIndex;    // 正在消耗的 icon 索引（-1 表示沒有）
        public float lastEnergy;       // 上次能量值（用於判斷是否正在消耗）

        public float targetWidth;
        public int currentPercent;
        public int targetPercent;

        // 排名動畫
        public int currentRank;
        public int targetRank;
        public float targetY;
        public bool isAnimating;
    }

    void Awake()
    {
        colorMapping = new Dictionary<string, Color>
        {
            { "blue", blueColor },
            { "yellow", yellowColor },
            { "green", greenColor },
            { "red", redColor }
        };
    }

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
    }

    /// <summary>
    /// 初始化玩家長條圖
    /// </summary>
    public void Initialize()
    {
        Debug.Log("[ColorPaperScoreUI] Initialize() 被呼叫了！");
        Debug.Log($"[ColorPaperScoreUI] paintIconFull 設定: {(paintIconFull != null ? paintIconFull.name : "未設定")}");
        Debug.Log($"[ColorPaperScoreUI] paintIconEmpty 設定: {(paintIconEmpty != null ? paintIconEmpty.name : "未設定")}");

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (gameManager == null)
        {
            Debug.LogWarning("[ColorPaperScoreUI] 初始化失敗：找不到 GameManager");
            return;
        }

        if (barPrefab == null)
        {
            Debug.LogWarning("[ColorPaperScoreUI] 初始化失敗：barPrefab 未設定");
            return;
        }

        if (barContainer == null)
        {
            Debug.LogWarning("[ColorPaperScoreUI] 初始化失敗：barContainer 未設定");
            return;
        }

        Debug.Log($"[ColorPaperScoreUI] playerControllers 數量: {gameManager.playerControllers.Count}");

        // 清除舊的長條
        foreach (Transform child in barContainer)
        {
            Destroy(child.gameObject);
        }
        playerBars.Clear();
        currentRanking.Clear();

        // 為每個玩家創建長條
        int rank = 0;
        foreach (var kvp in gameManager.playerControllers)
        {
            int playerId = kvp.Key;
            PlayerController player = kvp.Value;
            if (player == null) continue;

            Debug.Log($"[ColorPaperScoreUI] 創建玩家 {playerId} 的長條，顏色: {player.playerColor}");
            CreatePlayerBar(playerId, player.playerColor, rank);
            currentRanking.Add(playerId);
            rank++;
        }

        Debug.Log($"[ColorPaperScoreUI] 初始化完成，共 {playerBars.Count} 位玩家");
    }

    void CreatePlayerBar(int playerId, string playerColor, int initialRank)
    {
        GameObject barObj = Instantiate(barPrefab, barContainer);
        barObj.name = $"Bar_Player{playerId}";

        RectTransform rootRect = barObj.GetComponent<RectTransform>();

        // 找到子組件
        // 用名稱包含 "avatar"（不分大小寫）來搜尋
        Transform avatarTransform = null;
        Image[] allImages = barObj.GetComponentsInChildren<Image>();
        foreach (var img in allImages)
        {
            if (img.name.ToLower().Contains("avatar"))
            {
                avatarTransform = img.transform;
                break;
            }
        }

        // 找到 BarBackground 和 BarFill
        Transform barBackground = FindChildRecursive(barObj.transform, "BarBackground");
        if (barBackground == null)
        {
            // 嘗試找第一個有 BarFill 子物件的
            foreach (Transform child in barObj.transform)
            {
                if (child.Find("BarFill") != null)
                {
                    barBackground = child;
                    break;
                }
            }
        }

        RectTransform barFillRect = barBackground?.Find("BarFill")?.GetComponent<RectTransform>();
        Image barFillImage = barFillRect?.GetComponent<Image>();

        RectTransform avatarRect = avatarTransform?.GetComponent<RectTransform>();
        Image avatarImage = avatarTransform?.GetComponent<Image>();
        TextMeshProUGUI percentText = barObj.GetComponentInChildren<TextMeshProUGUI>();
        RectTransform percentTextRect = percentText?.GetComponent<RectTransform>();

        if (barFillRect == null || barFillImage == null)
        {
            Debug.LogWarning($"[ColorPaperScoreUI] Player {playerId} 找不到 BarFill");
            return;
        }

        // 設定長條顏色
        Color barColor = Color.white;
        if (colorMapping.TryGetValue(playerColor, out Color color))
        {
            barFillImage.color = color;
            barColor = color;
        }

        // 設定頭像（從現有的 SkinColorMapping 獲取）
        if (avatarImage != null)
        {
            Sprite avatarSprite = GetPlayerAvatarSprite(playerId);
            if (avatarSprite != null)
            {
                avatarImage.sprite = avatarSprite;
                Debug.Log($"[ColorPaperScoreUI] 玩家 {playerId} 頭像設定成功");
            }
            else
            {
                Debug.LogWarning($"[ColorPaperScoreUI] 玩家 {playerId} 找不到頭像！");
            }
        }

        // 找到並設定顏料 icon
        Image[] energyIcons = new Image[5];
        Transform energyIconsContainer = FindChildRecursive(barObj.transform, "EnergyIcons");
        if (energyIconsContainer != null)
        {
            Debug.Log($"[ColorPaperScoreUI] 玩家 {playerId} 找到 EnergyIcons 容器，子物件數量: {energyIconsContainer.childCount}");
            for (int i = 0; i < 5; i++)
            {
                Transform iconTransform = energyIconsContainer.Find($"Icon_{i}");
                if (iconTransform == null && i < energyIconsContainer.childCount)
                {
                    iconTransform = energyIconsContainer.GetChild(i);
                }

                if (iconTransform != null)
                {
                    energyIcons[i] = iconTransform.GetComponent<Image>();
                    // 設定 icon 顏色為玩家顏色
                    if (energyIcons[i] != null)
                    {
                        energyIcons[i].color = barColor;
                        Debug.Log($"[ColorPaperScoreUI] 玩家 {playerId} Icon_{i} 設定成功");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"[ColorPaperScoreUI] 玩家 {playerId} 找不到 EnergyIcons 容器！請確認 Prefab 結構");
        }

        // 取得玩家的 PaintEnergy
        PaintEnergy paintEnergy = null;
        if (gameManager.playerControllers.TryGetValue(playerId, out PlayerController player))
        {
            paintEnergy = player.GetComponent<PaintEnergy>();
        }

        // 初始位置
        float initialY = -initialRank * (barHeight + barSpacing);
        rootRect.anchoredPosition = new Vector2(rootRect.anchoredPosition.x, initialY);

        // 初始長條寬度
        Vector2 size = barFillRect.sizeDelta;
        size.x = minBarWidth;
        barFillRect.sizeDelta = size;

        // 初始百分比文字
        if (percentText != null)
        {
            percentText.text = "0%";
        }

        // 儲存資料
        BarData barData = new BarData
        {
            playerId = playerId,
            barObject = barObj,
            rootRect = rootRect,
            barFillRect = barFillRect,
            barFillImage = barFillImage,
            avatarRect = avatarRect,
            avatarImage = avatarImage,
            percentText = percentText,
            percentTextRect = percentTextRect,
            energyIcons = energyIcons,
            paintEnergy = paintEnergy,
            playerController = player,
            playerColorKey = playerColor,
            activeIconIndex = -1,
            lastEnergy = paintEnergy != null ? paintEnergy.GetCurrentEnergy() : 3f,
            targetWidth = minBarWidth,
            currentPercent = 0,
            targetPercent = 0,
            currentRank = initialRank,
            targetRank = initialRank,
            targetY = initialY,
            isAnimating = false
        };

        playerBars[playerId] = barData;

        // 訂閱 PaintEnergy 事件
        if (paintEnergy != null)
        {
            Debug.Log($"[ColorPaperScoreUI] 玩家 {playerId} 訂閱 PaintEnergy 事件成功，當前能量: {paintEnergy.GetCurrentEnergy()}/{paintEnergy.GetMaxEnergy()}");
            paintEnergy.OnEnergyChanged += (current, max) => UpdateEnergyIcons(barData, current, max);
            // 立即更新一次
            UpdateEnergyIcons(barData, paintEnergy.GetCurrentEnergy(), paintEnergy.GetMaxEnergy());
        }
        else
        {
            Debug.LogWarning($"[ColorPaperScoreUI] 玩家 {playerId} 找不到 PaintEnergy 組件！");
        }
    }

    Sprite GetPlayerAvatarSprite(int playerId)
    {
        if (gameManager == null || playerId >= gameManager.playersInfo.Count) return null;

        Player playerInfo = gameManager.playersInfo[playerId];
        string skin = playerInfo.skin;
        string color = playerInfo.color;

        // 從本地的 avatarMappings 對照表查找
        var skinMapping = avatarMappings.FirstOrDefault(x => x.skinName == skin);
        if (skinMapping != null)
        {
            Sprite avatar = null;
            if (color == "blue") avatar = skinMapping.blueAvatar;
            else if (color == "yellow") avatar = skinMapping.yellowAvatar;
            else if (color == "green") avatar = skinMapping.greenAvatar;
            else if (color == "red") avatar = skinMapping.redAvatar;

            if (avatar != null)
            {
                return avatar;
            }
        }

        Debug.LogWarning($"[ColorPaperScoreUI] 找不到 skin={skin}, color={color} 的頭像");
        return null;
    }

    /// <summary>
    /// 更新玩家分數
    /// </summary>
    public void UpdateScores(Dictionary<int, int> scores)
    {
        // 更新每個玩家的目標百分比
        foreach (var kvp in scores)
        {
            int playerId = kvp.Key;
            int percent = kvp.Value;

            if (playerBars.TryGetValue(playerId, out BarData data))
            {
                data.targetPercent = percent;
                data.targetWidth = Mathf.Lerp(minBarWidth, maxBarWidth, percent / 100f);
            }
        }

        // 計算新排名
        UpdateRanking(scores);
    }

    void UpdateRanking(Dictionary<int, int> scores)
    {
        // 根據分數排序（高分在前）
        List<int> newRanking = playerBars.Keys
            .OrderByDescending(id => scores.ContainsKey(id) ? scores[id] : 0)
            .ToList();

        // 檢查排名是否有變化
        bool rankingChanged = false;
        for (int i = 0; i < newRanking.Count; i++)
        {
            int playerId = newRanking[i];
            if (playerBars.TryGetValue(playerId, out BarData data))
            {
                if (data.targetRank != i)
                {
                    data.targetRank = i;
                    data.targetY = -i * (barHeight + barSpacing);
                    data.isAnimating = true;
                    rankingChanged = true;
                }
            }
        }

        if (rankingChanged)
        {
            currentRanking = newRanking;
        }
    }

    void Update()
    {
        foreach (var kvp in playerBars)
        {
            BarData data = kvp.Value;
            if (data.barFillRect == null) continue;

            // 平滑更新長條寬度
            UpdateBarWidth(data);

            // 更新百分比文字（位置和數值）
            UpdatePercentText(data);

            // 排名動畫
            UpdateRankAnimation(data);

            // 正在消耗的 icon 閃爍效果
            UpdateActiveIconBlink(data);
        }
    }

    /// <summary>
    /// 更新正在消耗的 icon 閃爍效果
    /// </summary>
    void UpdateActiveIconBlink(BarData data)
    {
        if (data.energyIcons == null) return;
        if (data.activeIconIndex < 0 || data.activeIconIndex >= 5) return;

        Image activeIcon = data.energyIcons[data.activeIconIndex];
        if (activeIcon == null) return;

        // 取得玩家顏色
        Color playerColor = Color.white;
        if (colorMapping.TryGetValue(data.playerColorKey, out Color mappedColor))
        {
            playerColor = mappedColor;
        }

        // 計算閃爍透明度（使用正弦波）
        float blinkAlpha = Mathf.Lerp(blinkMinAlpha, blinkMaxAlpha,
            (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f);

        // 應用閃爍效果
        Color blinkColor = playerColor;
        blinkColor.a = blinkAlpha;
        activeIcon.color = blinkColor;
    }

    void UpdateBarWidth(BarData data)
    {
        Vector2 size = data.barFillRect.sizeDelta;
        float currentWidth = size.x;

        if (Mathf.Abs(currentWidth - data.targetWidth) > 0.5f)
        {
            size.x = Mathf.Lerp(currentWidth, data.targetWidth, Time.deltaTime * barLerpSpeed);
            data.barFillRect.sizeDelta = size;
        }
    }

    void UpdatePercentText(BarData data)
    {
        if (data.percentText == null) return;

        // 根據長條寬度計算顯示百分比
        float widthPercent = Mathf.InverseLerp(minBarWidth, maxBarWidth, data.barFillRect.sizeDelta.x);
        int displayPercent = Mathf.RoundToInt(widthPercent * 100f);

        if (displayPercent != data.currentPercent)
        {
            data.currentPercent = displayPercent;
            data.percentText.text = $"{displayPercent}%";
        }

        // 讓百分比文字跟著進度條移動
        if (data.percentTextRect != null)
        {
            float barWidth = data.barFillRect.sizeDelta.x;
            float targetX = barWidth + percentTextOffset;

            Vector2 pos = data.percentTextRect.anchoredPosition;
            if (Mathf.Abs(pos.x - targetX) > 0.5f)
            {
                pos.x = Mathf.Lerp(pos.x, targetX, Time.deltaTime * barLerpSpeed);
                data.percentTextRect.anchoredPosition = pos;
            }
        }
    }

    void UpdateRankAnimation(BarData data)
    {
        if (!data.isAnimating || data.rootRect == null) return;

        Vector2 pos = data.rootRect.anchoredPosition;
        float currentY = pos.y;

        if (Mathf.Abs(currentY - data.targetY) > 0.5f)
        {
            pos.y = Mathf.Lerp(currentY, data.targetY, Time.deltaTime / rankSwapDuration * 3f);
            data.rootRect.anchoredPosition = pos;
        }
        else
        {
            pos.y = data.targetY;
            data.rootRect.anchoredPosition = pos;
            data.currentRank = data.targetRank;
            data.isAnimating = false;
        }
    }

    /// <summary>
    /// 更新顏料 icon 顯示
    /// </summary>
    void UpdateEnergyIcons(BarData data, float currentEnergy, float maxEnergy)
    {
        if (data.energyIcons == null)
        {
            Debug.LogWarning($"[ColorPaperScoreUI] 玩家 {data.playerId} energyIcons 是 null！");
            return;
        }

        // 取得玩家顏色
        Color playerColor = Color.white;
        if (colorMapping.TryGetValue(data.playerColorKey, out Color mappedColor))
        {
            playerColor = mappedColor;
            Debug.Log($"[ColorPaperScoreUI] 玩家 {data.playerId} 顏色 key={data.playerColorKey} -> RGB({playerColor.r:F2}, {playerColor.g:F2}, {playerColor.b:F2})");
        }
        else
        {
            Debug.LogWarning($"[ColorPaperScoreUI] 玩家 {data.playerId} 找不到顏色 key={data.playerColorKey}！");
        }

        // 判斷是否正在消耗能量（玩家正在按壓繪圖）
        bool isConsuming = data.playerController != null && data.playerController.IsPressing() && currentEnergy > 0;

        // 找出正在消耗的 icon 索引（部分填充的那格）
        int newActiveIndex = -1;
        if (isConsuming)
        {
            newActiveIndex = Mathf.FloorToInt(currentEnergy);
            if (newActiveIndex >= 5) newActiveIndex = 4;
            if (currentEnergy <= 0) newActiveIndex = -1;
        }
        data.activeIconIndex = newActiveIndex;

        // 更新每個 icon 的狀態
        for (int i = 0; i < 5; i++)
        {
            if (data.energyIcons[i] == null)
            {
                Debug.LogWarning($"[ColorPaperScoreUI] 玩家 {data.playerId} energyIcons[{i}] 是 null！");
                continue;
            }

            float iconEnergy = i + 1;  // icon 0 代表 0-1 格，icon 1 代表 1-2 格...

            if (currentEnergy >= iconEnergy)
            {
                // 滿格 - 顯示玩家顏色
                data.energyIcons[i].color = playerColor;
                Debug.Log($"[ColorPaperScoreUI] 玩家 {data.playerId} Icon_{i} 設為滿格顏色 RGB({playerColor.r:F2}, {playerColor.g:F2}, {playerColor.b:F2})");
                if (paintIconFull != null)
                {
                    data.energyIcons[i].sprite = paintIconFull;
                }
            }
            else if (currentEnergy > i)
            {
                // 部分填充 - 使用玩家顏色（閃爍效果在 Update 中處理）
                data.energyIcons[i].color = playerColor;
                if (paintIconFull != null)
                {
                    data.energyIcons[i].sprite = paintIconFull;
                }
            }
            else
            {
                // 空格 - 顯示灰色
                data.energyIcons[i].color = emptyIconColor;
                if (paintIconEmpty != null)
                {
                    data.energyIcons[i].sprite = paintIconEmpty;
                }
            }
        }

        data.lastEnergy = currentEnergy;
    }

    void OnDestroy()
    {
        // 取消訂閱事件（避免記憶體洩漏）
        foreach (var kvp in playerBars)
        {
            BarData data = kvp.Value;
            if (data.paintEnergy != null)
            {
                // 注意：由於使用 lambda，無法精確取消訂閱
                // 但 OnDestroy 時物件即將被銷毀，所以沒問題
            }
        }
    }
}
