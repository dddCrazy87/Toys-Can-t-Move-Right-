using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;
using TMPro;

// 遊戲狀態枚舉
public enum GameState
{
    WaitingToStart,
    NumberSelecting,
    RoundResolving,
    Voting,
    GameEnd
}

public enum GameResult
{
    GoodGuysWin,
    BadGuyWins
}

// 玩家身分枚舉
public enum Role
{
    GoodGuy,
    BadGuy
}

[System.Serializable]
public struct RoundConfig
{
    [Tooltip("正式遊戲時：該回合隨機區間的下界 (例如 10)")]
    public int minBound;

    [Tooltip("正式遊戲時：該回合隨機區間的上界 (例如 20)")]
    public int maxBound;

    [Tooltip("該回合的區間大小 (例如 4 代表包含 4 個數字，若起點為17，則為 17~20)")]
    public int intervalSize;

    [HideInInspector] public int minTarget;
    [HideInInspector] public int maxTarget;
}

public class PlayerData
{
    public int playerId;
    public Role role;
    public int currentSelectedNumber;
    public int votedPlayerId;

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

    public event Action OnGameStarted;
    public event Action<int, int> OnRoundStarted;
    public event Action<int, List<int>> OnRoundResolved;
    public event Action OnVotingPhaseStarted;
    public event Action<GameResult> OnGameEnded;

    [Header("遊戲設定")]
    public RoundConfig[] roundConfigs = new RoundConfig[5];

    [Header("倒數計時設定")]
    public CountDownUI countDownUI;
    public float numberSelectionTime = 30f;
    public float votingTime = 180f;

    [Header("開局動畫設定")]
    public GameStartCountDown gameStartCountDown;
    public string nextSceneName = "";

    [Header("試玩與結算設定")]
    public bool isTrial = true; // 預設第一把是試玩
    public GameObject ggButton;
    public TextMeshProUGUI ggButtonText; // 用來修改按鈕文字

    [Header("當前遊戲狀態 (唯讀測試用)")]
    public GameState currentState;
    public int currentRoundIndex = 0;
    public int successfulRounds = 0;


    public Dictionary<int, PlayerData> players = new Dictionary<int, PlayerData>();

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

        for (int i = 0; i < roundConfigs.Length; i++)
        {
            // 根據是否為試玩，決定這回合的上下界
            // 試玩時固定 6~18，正式版時讀取 Inspector 的設定
            int currentMinBound = isTrial ? 6 : roundConfigs[i].minBound;
            int currentMaxBound = isTrial ? 18 : roundConfigs[i].maxBound;

            // 區間寬度對應的數值差 (依然套用 Inspector 設定的區間大小)
            int diff = Mathf.Max(0, roundConfigs[i].intervalSize - 1);

            // 最大可能的區間起點，確保加上 diff 後不會超過當前的上限
            int maxPossibleMin = currentMaxBound - diff;

            // 保險機制：如果設定的值小於區間寬度，強制修正
            if (maxPossibleMin < currentMinBound)
                maxPossibleMin = currentMinBound;

            // 在允許的範圍內隨機決定該回合的 minTarget
            roundConfigs[i].minTarget = UnityEngine.Random.Range(currentMinBound, maxPossibleMin + 1);

            // 計算出 maxTarget
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

        Debug.Log($"抓內鬼開始！目前是 {(isTrial ? "試玩階段" : "正式關卡")}，壞人是 Player {badGuyId}");

        networkManager.BroadcastSpyGameInit(roleDict);
        OnGameStarted?.Invoke();

        StartCoroutine(StartFirstRoundAfterDelay());
    }

    private IEnumerator StartFirstRoundAfterDelay()
    {
        // 給 init 廣播足夠時間讓所有玩家收到（init 間隔 1 秒，至少等 1.5 秒）
        yield return new WaitForSeconds(1.5f);
        StartNewRound();
    }

    public void SubmitNumber(int playerId, int number)
    {
        if (currentState != GameState.NumberSelecting) return;
        if (!players.ContainsKey(playerId)) return;

        players[playerId].currentSelectedNumber = number;
        Debug.Log($"Player {playerId} 提交了數字 {number}");

        CheckAllNumbersSubmitted();
    }

    public void SubmitVote(int playerId, int votedTargetId)
    {
        if (currentState != GameState.Voting) return;
        if (!players.ContainsKey(playerId)) return;

        players[playerId].votedPlayerId = votedTargetId;
        Debug.Log($"Player {playerId} 投票給了 Player {votedTargetId}");

        CheckAllVotesSubmitted();
    }

    private void StartNewRound()
    {
        currentState = GameState.NumberSelecting;

        foreach (var p in players.Values)
        {
            p.currentSelectedNumber = 0;
        }

        RoundConfig currentConfig = roundConfigs[currentRoundIndex];
        Debug.Log($"--- 第 {currentRoundIndex + 1} 回合開始 ---");
        Debug.Log($"目標區間: {currentConfig.minTarget} ~ {currentConfig.maxTarget}");

        if (countDownUI != null)
        {
            countDownUI.StartCountdown(numberSelectionTime, OnNumberSelectionTimeout);
        }

        OnRoundStarted?.Invoke(currentConfig.minTarget, currentConfig.maxTarget);
        networkManager.BroadcastSpyRoundStart(currentRoundIndex, currentConfig.minTarget, currentConfig.maxTarget);
    }

    private void CheckAllNumbersSubmitted()
    {
        if (players.Values.Any(p => p.currentSelectedNumber == 0)) return;

        if (countDownUI != null) countDownUI.StopCountdown();

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
                p.currentSelectedNumber = UnityEngine.Random.Range(1, 6);
                Debug.Log($"Player {p.playerId} ({(p.role == Role.BadGuy ? "壞人" : "好人")}) 選擇超時，系統自動代選數字: {p.currentSelectedNumber}");
            }
        }

        CheckAllNumbersSubmitted();
    }

    private void ResolveRound()
    {
        foreach (var p in players.Values) p.selectedNumbersHistory.Add(p.currentSelectedNumber);

        List<int> submittedNumbers = players.Values.Select(p => p.currentSelectedNumber).ToList();

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

        // 好人只要成功 3 次，立刻獲勝，跳過投票階段
        if (successfulRounds >= 3)
        {
            Debug.Log("好人已達成 3 次任務成功，直接獲勝並跳過投票！");
            currentState = GameState.GameEnd;
            ExecuteGameEnd(GameResult.GoodGuysWin);
            return;
        }

        currentRoundIndex++;
        if (currentRoundIndex < 5) StartNewRound();
        else StartVotingPhase();
    }

    private void StartVotingPhase()
    {
        currentState = GameState.Voting;
        Debug.Log("--- 任務失敗，進入最後投票階段 ---");

        foreach (var p in players.Values) p.votedPlayerId = -1;

        if (countDownUI != null)
        {
            countDownUI.StartCountdown(votingTime, OnVotingTimeout);
        }

        OnVotingPhaseStarted?.Invoke();
        networkManager?.BroadcastSpyVotingStart();
    }

    private void CheckAllVotesSubmitted()
    {
        if (players.Values.Any(p => p.votedPlayerId == -1)) return;

        if (countDownUI != null) countDownUI.StopCountdown();

        currentState = GameState.GameEnd;
        ResolveGameEnd();
    }

    private void OnVotingTimeout()
    {
        if (currentState != GameState.Voting) return;

        List<int> validTargets = players.Keys.ToList();

        foreach (var p in players.Values)
        {
            if (p.votedPlayerId == -1)
            {
                p.votedPlayerId = validTargets[UnityEngine.Random.Range(0, validTargets.Count)];
                Debug.Log($"Player {p.playerId} 投票超時，系統自動投票給 Player {p.votedPlayerId}");
            }
        }

        CheckAllVotesSubmitted();
    }

    private void ResolveGameEnd()
    {
        Debug.Log("投票結束，準備結算勝負！");

        Dictionary<int, int> voteCounts = new Dictionary<int, int>();
        foreach (int key in players.Keys) voteCounts[key] = 0;

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

        // 平票或沒投出壞人，皆為壞人贏
        if (maxVotedPlayers.Count > 1)
        {
            Debug.Log("投票結果：平票，壞人勝利！");
            finalResult = GameResult.BadGuyWins;
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
                Debug.Log($"投票結果：好人 (Player {highestVotedPlayerId}) 被誤認為內鬼！壞人勝利！");
                finalResult = GameResult.BadGuyWins;
            }
        }

        ExecuteGameEnd(finalResult);
    }

    private void ExecuteGameEnd(GameResult finalResult)
    {
        Debug.Log($"====================");
        Debug.Log($"最終勝負：{(finalResult == GameResult.GoodGuysWin ? "好人陣營勝利！" : "壞人獨贏！")}");
        Debug.Log($"====================");

        OnGameEnded?.Invoke(finalResult);
        if (ggButton != null) ggButton.SetActive(true);

        if (isTrial) { if (ggButtonText != null) ggButtonText.text = "正式遊戲"; }
        else
        {
            if (ggButtonText != null) ggButtonText.text = "前往頒獎";
            foreach (var p in players.Values)
            {
                if (finalResult == GameResult.GoodGuysWin)
                {
                    if (p.role == Role.GoodGuy) gameManager.IncreasePlayerPoint(p.playerId, 10);
                }
                else
                {
                    if (p.role == Role.BadGuy) gameManager.IncreasePlayerPoint(p.playerId, 10);
                }
            }
        }
    }

    public void OnGGButtonClicked()
    {
        if (isTrial)
        {
            isTrial = false;
            if (ggButton != null) ggButton.SetActive(false);

            if (networkManager != null) networkManager.BroadcastSpyGameReset();

            currentState = GameState.WaitingToStart;
            StartGame();
        }
        else LoadNextScene();
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


