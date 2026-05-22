using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class Spy_UIManager : MonoBehaviour
{
    [Header("UI 參考")]
    public TextMeshProUGUI targetText;
    public TextMeshProUGUI gameStateText;
    [Header("動態歷史紀錄設定")]
    public GameObject historyItemPrefab;
    public Transform historyContainer;

    // 用來記錄所有生成出來的 UI 物件，方便最後一輪直接更新內容
    private List<GameObject> spawnedItems = new List<GameObject>();
    private Color successColor;
    private void Start()
    {
        ColorUtility.TryParseHtmlString("#30C630", out successColor);

        targetText.text = "遊戲初始化";
        gameStateText.text = "正在分配玩家身分...";
        ClearHistoryContainer();

        // 訂閱事件
        SpyGameManager.Instance.OnRoundStarted += UpdateRoundTargetUI;
        SpyGameManager.Instance.OnRoundResolved += UpdateHistoryUI;
        SpyGameManager.Instance.OnVotingPhaseStarted += ShowVotingUI;
        SpyGameManager.Instance.OnGameEnded += ShowGameEndUI;
    }

    private void OnDestroy()
    {
        if (SpyGameManager.Instance != null)
        {
            SpyGameManager.Instance.OnRoundStarted -= UpdateRoundTargetUI;
            SpyGameManager.Instance.OnRoundResolved -= UpdateHistoryUI;
            SpyGameManager.Instance.OnVotingPhaseStarted -= ShowVotingUI;
            SpyGameManager.Instance.OnGameEnded -= ShowGameEndUI;
        }
    }

    private void ClearHistoryContainer()
    {
        foreach (Transform child in historyContainer) Destroy(child.gameObject);
        spawnedItems.Clear();
    }

    private void UpdateRoundTargetUI(int minTarget, int maxTarget)
    {
        int currentRound = SpyGameManager.Instance.currentRoundIndex + 1;
        gameStateText.text = $"第{currentRound}回合開始       請看手機選擇數字";
        targetText.text = (minTarget == maxTarget) ? $"{minTarget}" : $"{minTarget} ～ {maxTarget}";
    }

    private void UpdateHistoryUI(int roundIndex, List<int> submittedNumbers)
    {
        GameObject newItem = Instantiate(historyItemPrefab, historyContainer);
        spawnedItems.Add(newItem);

        TextMeshProUGUI[] texts = newItem.GetComponentsInChildren<TextMeshProUGUI>();
        // 改為需要 3 個 Text 元素
        if (texts.Length < 3)
        {
            Debug.LogWarning("HistoryItemPrefab 需要包含至少 3 個 TextMeshProUGUI 元件！");
            return;
        }

        TextMeshProUGUI roundText = texts[0];
        TextMeshProUGUI targetRangeText = texts[1];
        TextMeshProUGUI numbersText = texts[2];

        int sum = 0;
        foreach (int num in submittedNumbers) sum += num;

        RoundConfig config = SpyGameManager.Instance.roundConfigs[roundIndex];
        bool isSuccess = sum >= config.minTarget && sum <= config.maxTarget;

        roundText.text = $"第{roundIndex + 1}輪";
        targetRangeText.text = (config.minTarget == config.maxTarget) ? $"{config.minTarget}" : $"{config.minTarget} ～ {config.maxTarget}";

        if (roundIndex < 2)
        {
            numbersText.text = $"[{string.Join(", ", submittedNumbers)}]";
            numbersText.color = isSuccess ? successColor : Color.red;

            gameStateText.text = $"第{roundIndex + 1}輪結算完畢！({(isSuccess ? "成功" : "失敗")})";
        }
        else
        {
            numbersText.text = "[???]";
            numbersText.color = Color.gray;

            gameStateText.text = $"第{roundIndex + 1}輪結算完畢！(結果隱藏)";
        }
    }

    private void RevealAllHistories()
    {
        // 進入結算或投票階段時，把過去生成出來的 UI 全部翻開，更新實際數據與成敗
        for (int i = 0; i < SpyGameManager.Instance.roundHistories.Count; i++)
        {
            if (i >= spawnedItems.Count) break;

            var history = SpyGameManager.Instance.roundHistories[i];
            int sum = 0;
            foreach (int num in history) sum += num;

            RoundConfig config = SpyGameManager.Instance.roundConfigs[i];
            bool isSuccess = sum >= config.minTarget && sum <= config.maxTarget;

            TextMeshProUGUI[] texts = spawnedItems[i].GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 3)
            {
                texts[2].text = $"[{string.Join(", ", history)}]";
                texts[2].color = isSuccess ? successColor : Color.red;
            }
        }
    }

    private void ShowVotingUI()
    {
        targetText.text = "目標：抓出壞人！";
        gameStateText.text = "請在手機上進行最後投票";

        RevealAllHistories();
    }

    private void ShowGameEndUI(GameResult result)
    {
        // 確保提早獲勝跳過投票時，也能翻開紀錄
        RevealAllHistories();

        string winnerStr = (result == GameResult.GoodGuysWin) ? "<color=#0055FF>好人勝利！</color>" : "<color=#FF0000>壞人獨贏！</color>";

        int badGuyId = -1;
        foreach (var p in SpyGameManager.Instance.players.Values)
        {
            if (p.role == Role.BadGuy) badGuyId = p.playerId;
        }

        string badGuyName = $"Player {badGuyId + 1}";
        var netManager = FindFirstObjectByType<NetworkManager>();
        if (netManager != null)
        {
            var badPlayer = netManager.playersInfo.FirstOrDefault(p => p.index == badGuyId);
            if (badPlayer != null) badGuyName = badPlayer.name;
        }

        gameStateText.text = $"遊戲結束！\n{winnerStr}\n壞人是：{badGuyName}";
        targetText.text = "";
    }
}


