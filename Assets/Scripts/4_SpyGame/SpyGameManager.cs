using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections;

// 遊戲狀態枚舉
public enum GameState
{
    WaitingToStart,
    NumberSelecting, // 玩家正在選數字的 30 秒
    RoundResolving,  // 結算當前回合數字
    Voting,          // 最後的 3 分鐘投票
    GameEnd          // 遊戲結束，顯示勝負
}

public enum GameResult
{
    GoodGuysWin,
    BadGuyWins
}

// 玩家身分枚舉
public enum Role
{
    GoodGuy, // 好人
    BadGuy   // 壞人
}

// 單一回合的設定檔
[System.Serializable]
public struct RoundConfig
{
    [Tooltip("該回合的區間大小 (例如 4 代表上下界相差 4)")]
    public int intervalSize;

    [HideInInspector] public int minTarget;
    [HideInInspector] public int maxTarget;
}

// 玩家資料模型
public class PlayerData
{
    public int playerId; // 0, 1, 2, 3
    public Role role;
    public int currentSelectedNumber; // 當前回合選擇的數字 (0代表還沒選)
    public int votedPlayerId;         // 投票給誰 (-1代表還沒投)

    public List<int> selectedNumbersHistory = new();
}

public class SpyGameManager : MonoBehaviour
{
    NetworkManager networkManager;
    BgmPlayer bgmPlayer;
    GameManager gameManager;

    public SceneFadeInFadeOut sceneFadeInFadeOut;
    public ImgBBUploader imgUploader;

    public static SpyGameManager Instance { get; private set; }

    // 這區 UI 事件廣播
    public event Action OnGameStarted;               // 遊戲開始，分發身分
    public event Action<int, int> OnRoundStarted;    // 回合開始 (傳遞最小、最大區間)
    public event Action<int, List<int>> OnRoundResolved; // 回合結算 (傳遞回合數、該回合大家出的數字組合)
    public event Action OnVotingPhaseStarted;        // 進入投票階段
    public event Action<GameResult> OnGameEnded;     // 遊戲結束，傳遞最終勝負結果

    [Header("隨機目標範圍設定")]
    [Tooltip("隨機產生的目標下界")]
    public int globalMinBound = 8;
    [Tooltip("隨機產生的目標上界")]
    public int globalMaxBound = 18;

    [Header("遊戲設定")]
    [Tooltip("設定五個回合的區間大小，會越來越窄")]
    public RoundConfig[] roundConfigs = new RoundConfig[5];

    [Header("倒數計時設定")]
    public CountDownUI countDownUI;          // 綁定倒數 UI 腳本
    public float numberSelectionTime = 30f;  // 選擇數字階段限時
    public float votingTime = 180f;          // 投票階段限時
    [Header("開局動畫設定")]
    public GameStartCountDown gameStartCountDown;

    [Header("結算按鈕")]
    public GameObject ggButton;
    public string nextSceneName = "";

    [Header("當前遊戲狀態 (唯讀測試用)")]
    public GameState currentState;
    public int currentRoundIndex = 0; // 0~4 代表第一到第五回合
    public int successfulRounds = 0;  // 成功回合數 (達到3次整體任務即算成功)


    // 玩家字典 (Key: playerId, Value: PlayerData)
    public Dictionary<int, PlayerData> players = new Dictionary<int, PlayerData>();

    // 紀錄每一回合大家出的數字 (無序的，用來顯示在歷史紀錄)
    public List<List<int>> roundHistories = new List<List<int>>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        ggButton.SetActive(false);
        gameManager = FindFirstObjectByType<GameManager>();
        bgmPlayer = FindFirstObjectByType<BgmPlayer>();
        if (bgmPlayer) bgmPlayer.PauseBGM();
        networkManager = FindFirstObjectByType<NetworkManager>();
        currentState = GameState.WaitingToStart;
        InitializeNetworkPlayers();
        StartCoroutine(AutoStartGameCo());
    }

    System.Collections.IEnumerator AutoStartGameCo()
    {
        yield return new WaitForSeconds(0.5f);
        if (gameStartCountDown != null) gameStartCountDown.CountDownAndStartGame(StartGame);
        else StartGame();
    }

    private void InitializeNetworkPlayers()
    {
        players.Clear();

        NetworkManager nm = FindFirstObjectByType<NetworkManager>();
        if (nm != null && nm.playersInfo.Count > 0)
        {
            for (int i = 0; i < nm.playersInfo.Count; i++)
            {
                nm.playersInfo[i].index = i;
            }
            foreach (var p in nm.playersInfo)
            {
                players.Add(p.index, new PlayerData
                {
                    playerId = p.index,
                    currentSelectedNumber = 0,
                    votedPlayerId = -1
                });
            }
        }
        else
        {
            // 測試模式 (沒有網路玩家時)
            for (int i = 0; i < 4; i++)
            {
                players.Add(i, new PlayerData { playerId = i, currentSelectedNumber = 0, votedPlayerId = -1 });
            }
        }
    }

    public void StartGame()
    {
        if (bgmPlayer) bgmPlayer.ChangeBgm();
        if (currentState != GameState.WaitingToStart && currentState != GameState.GameEnd) return;

        List<int> playerIds = players.Keys.ToList();
        if (playerIds.Count == 0) return;

        int badGuyId = playerIds[UnityEngine.Random.Range(0, playerIds.Count)];
        Dictionary<int, string> roleDict = new Dictionary<int, string>();

        // --- 隨機決定本場遊戲每個回合的目標區間 ---
        for (int i = 0; i < roundConfigs.Length; i++)
        {
            int diff = Mathf.Max(0, roundConfigs[i].intervalSize - 1);
            int maxPossibleMin = globalMaxBound - diff;

            // 防呆：如果設定的區間跨度大於全域範圍，強行將 minTarget 設定為下界
            if (maxPossibleMin < globalMinBound) maxPossibleMin = globalMinBound;

            roundConfigs[i].minTarget = UnityEngine.Random.Range(globalMinBound, maxPossibleMin + 1);
            roundConfigs[i].maxTarget = roundConfigs[i].minTarget + diff;
        }

        foreach (var id in playerIds)
        {
            players[id].role = (id == badGuyId) ? Role.BadGuy : Role.GoodGuy;
            players[id].votedPlayerId = -1;
            players[id].currentSelectedNumber = 0;
            players[id].selectedNumbersHistory.Clear();
            roleDict.Add(id, players[id].role.ToString());
        }

        currentRoundIndex = 0;
        successfulRounds = 0;
        roundHistories.Clear();

        Debug.Log($"抓內鬼開始！壞人是 Player {badGuyId}");

        networkManager.BroadcastSpyGameInit(roleDict);
        OnGameStarted?.Invoke();

        StartNewRound();
    }

    /// 玩家提交數字 (由手機端呼叫)
    public void SubmitNumber(int playerId, int number)
    {
        if (currentState != GameState.NumberSelecting) return;
        if (!players.ContainsKey(playerId)) return;

        players[playerId].currentSelectedNumber = number;
        Debug.Log($"Player {playerId} 提交了數字 {number}");

        CheckAllNumbersSubmitted();
    }

    /// 玩家提交投票 (由手機端呼叫)
    public void SubmitVote(int playerId, int votedTargetId)
    {
        if (currentState != GameState.Voting) return;
        if (!players.ContainsKey(playerId)) return;

        players[playerId].votedPlayerId = votedTargetId;
        Debug.Log($"Player {playerId} 投票給了 Player {votedTargetId}");

        CheckAllVotesSubmitted();
    }

    // ==========================================
    // ▲ API 結束 ▲
    // ==========================================

    private void StartNewRound()
    {
        currentState = GameState.NumberSelecting;

        // 清空所有人這回合的選擇
        foreach (var p in players.Values)
        {
            p.currentSelectedNumber = 0;
        }

        RoundConfig currentConfig = roundConfigs[currentRoundIndex];
        Debug.Log($"--- 第 {currentRoundIndex + 1} 回合開始 ---");
        Debug.Log($"目標區間: {currentConfig.minTarget} ~ {currentConfig.maxTarget}");

        // 啟動 30 秒倒數，並設定超時回呼
        if (countDownUI != null)
        {
            countDownUI.StartCountdown(numberSelectionTime, OnNumberSelectionTimeout);
        }

        OnRoundStarted?.Invoke(currentConfig.minTarget, currentConfig.maxTarget);
        networkManager.BroadcastSpyRoundStart(currentRoundIndex, currentConfig.minTarget, currentConfig.maxTarget);
    }

    private void CheckAllNumbersSubmitted()
    {
        // 檢查是否還有玩家沒選數字 (0 代表沒選)
        if (players.Values.Any(p => p.currentSelectedNumber == 0)) return;

        // 若所有人提前完成，停止倒數計時
        if (countDownUI != null) countDownUI.StopCountdown();

        // 全員提交完畢，進入結算
        currentState = GameState.RoundResolving;
        ResolveRound();
    }

    private void OnNumberSelectionTimeout()
    {
        if (currentState != GameState.NumberSelecting) return;

        foreach (var p in players.Values)
        {
            if (p.currentSelectedNumber == 0)
            {
                // ▼ 修改：針對壞人與好人超時做不同處理 ▼
                if (p.role == Role.BadGuy)
                {
                    bool hasChosen1 = p.selectedNumbersHistory.Contains(1);
                    bool hasChosen5 = p.selectedNumbersHistory.Contains(5);

                    // 第四輪 (index為3)    
                    if (currentRoundIndex == 3)
                    {
                        if (!hasChosen1 && !hasChosen5) p.currentSelectedNumber = UnityEngine.Random.Range(0, 2) == 0 ? 1 : 5;
                        else p.currentSelectedNumber = UnityEngine.Random.Range(1, 6);
                    }
                    // 第五輪 (index為4)
                    else if (currentRoundIndex == 4)
                    {
                        if (!hasChosen1 && !hasChosen5) p.currentSelectedNumber = UnityEngine.Random.Range(0, 2) == 0 ? 1 : 5;
                        else if (!hasChosen1) p.currentSelectedNumber = 1;
                        else if (!hasChosen5) p.currentSelectedNumber = 5;
                        else p.currentSelectedNumber = UnityEngine.Random.Range(1, 6);
                    }
                    // 第一到三輪
                    else p.currentSelectedNumber = UnityEngine.Random.Range(1, 6);
                }
                // 好人超時一律隨機代選
                else p.currentSelectedNumber = UnityEngine.Random.Range(1, 6);

                Debug.Log($"Player {p.playerId} ({(p.role == Role.BadGuy ? "壞人" : "好人")}) 選擇超時，系統自動代選數字: {p.currentSelectedNumber}");
            }
        }

        CheckAllNumbersSubmitted();
    }

    private void ResolveRound()
    {
        foreach (var p in players.Values) p.selectedNumbersHistory.Add(p.currentSelectedNumber);

        List<int> submittedNumbers = players.Values.Select(p => p.currentSelectedNumber).ToList();

        // 洗牌
        for (int i = 0; i < submittedNumbers.Count; i++)
        {
            int temp = submittedNumbers[i];
            int randomIndex = UnityEngine.Random.Range(i, submittedNumbers.Count);
            submittedNumbers[i] = submittedNumbers[randomIndex];
            submittedNumbers[randomIndex] = temp;
        }

        roundHistories.Add(submittedNumbers);

        int sum = submittedNumbers.Sum();
        RoundConfig currentConfig = roundConfigs[currentRoundIndex];
        bool isSuccess = sum >= currentConfig.minTarget && sum <= currentConfig.maxTarget;

        if (isSuccess) successfulRounds++;

        Debug.Log($"本回合總和為: {sum}。出牌組合: [{string.Join(", ", submittedNumbers)}]");
        Debug.Log(isSuccess ? "任務成功！" : "任務失敗！");

        OnRoundResolved?.Invoke(currentRoundIndex, submittedNumbers);

        currentRoundIndex++;
        if (currentRoundIndex < 5) StartNewRound();
        else StartVotingPhase();
    }

    private void StartVotingPhase()
    {
        currentState = GameState.Voting;
        Debug.Log("--- 進入最後投票階段 ---");

        foreach (var p in players.Values) p.votedPlayerId = -1;

        // 啟動 3 分鐘 (180秒) 倒數，並設定超時回呼
        if (countDownUI != null)
        {
            countDownUI.StartCountdown(votingTime, OnVotingTimeout);
        }

        OnVotingPhaseStarted?.Invoke();
        networkManager?.BroadcastSpyVotingStart();
    }

    private void CheckAllVotesSubmitted()
    {
        // 檢查是否還有玩家沒投票 (-1 代表沒選)
        if (players.Values.Any(p => p.votedPlayerId == -1)) return;

        // 若所有人提前投票完成，停止倒數計時
        if (countDownUI != null) countDownUI.StopCountdown();

        currentState = GameState.GameEnd;
        ResolveGameEnd();
    }

    private void OnVotingTimeout()
    {
        // 確保還在投票階段
        if (currentState != GameState.Voting) return;

        List<int> validTargets = players.Keys.ToList();

        foreach (var p in players.Values)
        {
            if (p.votedPlayerId == -1)
            {
                // 超時未選的玩家，隨機投給場上任一玩家
                p.votedPlayerId = validTargets[UnityEngine.Random.Range(0, validTargets.Count)];
                Debug.Log($"Player {p.playerId} 投票超時，系統自動投票給 Player {p.votedPlayerId}");
            }
        }

        // 代選完畢後，呼叫檢查函式繼續遊戲結算與廣播流程
        CheckAllVotesSubmitted();
    }

    private void ResolveGameEnd()
    {
        Debug.Log("遊戲結束，準備結算勝負！");

        bool isTaskSuccess = successfulRounds >= 3;
        Debug.Log($"整體任務狀態: {(isTaskSuccess ? "成功" : "失敗")} (成功次數: {successfulRounds}/5)");

        Dictionary<int, int> voteCounts = new Dictionary<int, int>();
        for (int i = 0; i < 4; i++) voteCounts[i] = 0;

        foreach (var p in players.Values)
        {
            if (p.votedPlayerId != -1)
            {
                voteCounts[p.votedPlayerId]++;
            }
        }

        int maxVotes = voteCounts.Values.Max();

        List<int> maxVotedPlayers = voteCounts.Where(kvp => kvp.Value == maxVotes)
                                              .Select(kvp => kvp.Key).ToList();

        GameResult finalResult;

        if (maxVotedPlayers.Count > 1)
        {
            Debug.Log("投票結果：平票");
            finalResult = isTaskSuccess ? GameResult.GoodGuysWin : GameResult.BadGuyWins;
        }
        else
        {
            int highestVotedPlayerId = maxVotedPlayers[0];
            Role votedRole = players[highestVotedPlayerId].role;

            if (votedRole == Role.BadGuy)
            {
                Debug.Log("投票結果：壞人被抓到了！");
                finalResult = GameResult.GoodGuysWin;
            }
            else
            {
                Debug.Log($"投票結果：好人 (Player {highestVotedPlayerId}) 被誤認為內鬼！");
                finalResult = GameResult.BadGuyWins;
            }
        }

        Debug.Log($"====================");
        Debug.Log($"最終勝負：{(finalResult == GameResult.GoodGuysWin ? "好人陣營勝利！" : "壞人獨贏！")}");
        Debug.Log($"====================");

        OnGameEnded?.Invoke(finalResult);
        ggButton.SetActive(true);

        foreach (var p in players.Values)
        {
            if (finalResult == GameResult.GoodGuysWin)
            {
                if (p.role == Role.GoodGuy) gameManager.IncreasePlayerPoint(p.playerId, 1);
            }
            else
            {
                if (p.role == Role.BadGuy) gameManager.IncreasePlayerPoint(p.playerId, 1);
            }
        }
    }

    public void LoadNextScene()
    {
        if (bgmPlayer) bgmPlayer.PauseBGM();
        StartCoroutine(EndGameRoutine());
    }

    private IEnumerator EndGameRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        string uploadedUrl = "";

        if (imgUploader != null)
        {
            yield return StartCoroutine(imgUploader.UploadToImgBB((url) =>
            {
                uploadedUrl = url;
            }));

            if (!string.IsNullOrEmpty(uploadedUrl))
            {
                Debug.Log($"[ToyBoxGameManager] 圖片網址: {uploadedUrl}");
                if (gameManager) gameManager.hasPostcard = true;
            }
            else
            {
                Debug.LogWarning("[ToyBoxGameManager] 上傳失敗");
            }
        }
        else
        {
            Debug.LogWarning("[ToyBoxGameManager] imgUploader 未設定，跳過截圖上傳");
            yield return new WaitForSeconds(0.5f);
        }

        if (networkManager) networkManager.BroadcastTerminate(uploadedUrl);
        sceneFadeInFadeOut.LoadNextSceneWithFadeOut();
    }
}

