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
    [Tooltip("該回合的區間大小 (例如 4 代表上下界相差 4)")]
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

    [Header("隨機目標範圍設定")]
    public int globalMinBound = 8;
    public int globalMaxBound = 18;

    [Header("遊戲設定")]
    public RoundConfig[] roundConfigs = new RoundConfig[5];

    [Header("倒數計時設定")]
    public CountDownUI countDownUI;
    public float numberSelectionTime = 30f;
    public float votingTime = 180f;
    [Header("開局動畫設定")]
    public GameStartCountDown gameStartCountDown;

    [Header("結算按鈕")]
    public GameObject ggButton;
    public string nextSceneName = "";

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
            int diff = Mathf.Max(0, roundConfigs[i].intervalSize - 1);
            int maxPossibleMin = globalMaxBound - diff;

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
                if (p.role == Role.BadGuy)
                {
                    bool hasChosen1 = p.selectedNumbersHistory.Contains(1);
                    bool hasChosen5 = p.selectedNumbersHistory.Contains(5);

                    if (currentRoundIndex == 3)
                    {
                        if (!hasChosen1 && !hasChosen5) p.currentSelectedNumber = UnityEngine.Random.Range(0, 2) == 0 ? 1 : 5;
                        else p.currentSelectedNumber = UnityEngine.Random.Range(1, 6);
                    }
                    else if (currentRoundIndex == 4)
                    {
                        if (!hasChosen1 && !hasChosen5) p.currentSelectedNumber = UnityEngine.Random.Range(0, 2) == 0 ? 1 : 5;
                        else if (!hasChosen1) p.currentSelectedNumber = 1;
                        else if (!hasChosen5) p.currentSelectedNumber = 5;
                        else p.currentSelectedNumber = UnityEngine.Random.Range(1, 6);
                    }
                    else p.currentSelectedNumber = UnityEngine.Random.Range(1, 6);
                }
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

        // 新增規則：好人只要成功 3 次，立刻獲勝，跳過投票階段
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

        // 新增規則：平票或沒投出壞人，皆為壞人贏
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

