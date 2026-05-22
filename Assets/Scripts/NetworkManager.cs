using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleWebRTC;
using UnityEngine.SceneManagement;
using System.Linq;

#region MessageTypeClasses
[System.Serializable]
public class BaseMessage { public string type; }
[System.Serializable]
public class IdentityMessage : BaseMessage { public string characterName; public string nickname; }
[System.Serializable]
public class Vector2Data { public float x; public float y; }
[System.Serializable]
public class MoveMessage : BaseMessage { public Vector2Data vector; public bool is_press; }
[System.Serializable]
public class HostUpdateMessage { public string type; public string hostId; }
[System.Serializable]
public class InitialMessage { public string type; public string color; }
[System.Serializable]
public class FinalPlayerData { public int rank; public string name; public int point; public string color; public string skin; }
[System.Serializable]
public class TerminateMessage { public string type; public string link; public string levelName; public List<FinalPlayerData> finalPlayerDatas; }
[System.Serializable]
public class SelectLevelMessage : BaseMessage { public string level; }
[System.Serializable]
public class LevelSelectedMessage { public string type; public string level; }
[System.Serializable]
public class NavigateAckMessage : BaseMessage { public string target; }
[System.Serializable]
public class TapActionMessage : BaseMessage { }

// spy game
[System.Serializable]
public class SpyGameInitMessage : BaseMessage { public int myPlayerId; public string role; public List<string> playerNames; }
[System.Serializable]
public class SpyRoundStartMessage : BaseMessage { public int roundIndex; public int minTarget; public int maxTarget; }
[System.Serializable]
public class SpyVotingStartMessage : BaseMessage { public string message; }
[System.Serializable]
public class NumberSelectMessage : BaseMessage { public int number; }
[System.Serializable]
public class VoteSubmitMessage : BaseMessage { public int votedTargetId; }
[System.Serializable]
public class SpyGameResetMessage : BaseMessage { }
#endregion

public class NetworkManager : MonoBehaviour
{
    public WebRTCConnection webRTCConnection;
    [Header("GameManager")]
    public GameManager gameManager;
    [Header("Player Info")]
    public List<Player> playersInfo = new();
    public Dictionary<string, Player> peerIdToPlayer = new();
    private List<string> selectedSkinColor = new();

    private string hostPeerId = null;
    public string unityPeerId { get; private set; }
    private Dictionary<string, float> lastIdentifyTime = new();  // 防止重複 identify
    private const float IDENTIFY_COOLDOWN = 1f;  // 1 秒內不重複處理

    // ACK 追蹤：記錄哪些 peer 已確認收到哪個 navigate 訊息
    private Dictionary<string, HashSet<string>> receivedAcks = new();
    void Start()
    {
        WebRTCManager.OnDataMessageReceived_Static += OnDataReceived;
        WebRTCManager.OnPeerDisconnected_Static += OnPeerDisconnected;
        gameManager = FindFirstObjectByType<GameManager>();
        selectedSkinColor = new List<string> { "green", "yellow", "blue", "red" };
        hostPeerId = null;
    }
    void OnDestroy()
    {
        WebRTCManager.OnDataMessageReceived_Static -= OnDataReceived;
        WebRTCManager.OnPeerDisconnected_Static -= OnPeerDisconnected;
    }
    public void SetwebRTCConnection(string peerId)
    {
        unityPeerId = peerId;
        webRTCConnection.SetUniquePlayerName(peerId);
        webRTCConnection.Connect();
    }

    // ------------- OnPeerDisconnected -------------

    private void OnPeerDisconnected(string senderPeerId)
    {
        Debug.Log($"Peer {senderPeerId} is disconnected.");

        // 檢查 peer 是否存在於字典中（可能連線還沒完成就斷了）
        if (!peerIdToPlayer.ContainsKey(senderPeerId))
        {
            Debug.LogWarning($"Peer {senderPeerId} 不在玩家字典中，可能連線尚未完成就斷線了。");
            return;
        }

        if (SceneManager.GetActiveScene().name == "2_Setting")
        {
            Player leavingPlayer = peerIdToPlayer[senderPeerId];
            playersInfo.Remove(leavingPlayer);
            peerIdToPlayer.Remove(senderPeerId);
            lastIdentifyTime.Remove(senderPeerId);

            if (hostPeerId == senderPeerId)
            {
                // Debug.LogWarning("Host has disconnected. Clearing host.");
                // hostPeerId = null;
                if (peerIdToPlayer.Count > 0)
                    hostPeerId = peerIdToPlayer.First().Key;
                else hostPeerId = null;
                BroadcastHostUpdate();
            }
            FindFirstObjectByType<LobbyUI>().UpdateLobbyUI();
        }
        else if (peerIdToPlayer.ContainsKey(senderPeerId))
        {
            Player p = peerIdToPlayer[senderPeerId];
            Debug.Log($"Player {p.name} ({senderPeerId}) went offline. Waiting for reconnect...");
        }
    }

    // ------------- OnDataReceived -------------

    private void OnDataReceived(string message, string senderPeerId)
    {
        try
        {
            BaseMessage data = JsonUtility.FromJson<BaseMessage>(message);

            switch (data.type)
            {
                case "identify":
                    HandleIdentifyMessage(message, senderPeerId);
                    break;

                case "start_game":
                    if (senderPeerId == hostPeerId)
                    {
                        Debug.Log("Host Requested Start Game！");
                        StartCoroutine(HandleStartGame());
                    }
                    break;

                case "move":
                case "manualMove":
                    HandleMoveMessage(message, senderPeerId);
                    break;

                case "select_level":
                    HandleSelectLevel(message, senderPeerId);
                    break;

                case "navigate_ack":
                    HandleNavigateAck(message, senderPeerId);
                    break;

                case "tap_action":
                    HandleTapAction(senderPeerId);
                    break;

                case "discard_action":
                    HandleDiscardAction(senderPeerId);
                    break;

                case "submit_number":
                    if (peerIdToPlayer.ContainsKey(senderPeerId))
                    {
                        NumberSelectMessage numMsg = JsonUtility.FromJson<NumberSelectMessage>(message);
                        int pIndex = peerIdToPlayer[senderPeerId].index;
                        // 呼叫我們抓內鬼的 GameManager (注意：這裡用 SpyGameManager 避免跟你原本的 GameManager 撞名，稍後會說明)
                        SpyGameManager.Instance?.SubmitNumber(pIndex, numMsg.number);
                    }
                    break;

                case "submit_vote":
                    if (peerIdToPlayer.ContainsKey(senderPeerId))
                    {
                        VoteSubmitMessage voteMsg = JsonUtility.FromJson<VoteSubmitMessage>(message);
                        int pIndex = peerIdToPlayer[senderPeerId].index;
                        SpyGameManager.Instance?.SubmitVote(pIndex, voteMsg.votedTargetId);
                    }
                    break;

                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to process message ({senderPeerId}): {ex.Message} - {message}");
        }
    }

    private IEnumerator HandleStartGame()
    {
        // 先發送 level_selected，等 1 秒確保所有玩家收到
        if (gameManager != null) BroadcastLevelSelected(gameManager.selectedLevel);
        yield return new WaitForSeconds(1f);

        // 再發送 navigate_to_game
        BroadcastNavigateToGame();
        gameManager.UpdatePlayerInfo(playersInfo);
        SceneManager.LoadScene("3_Tutorial");
    }

    private void HandleIdentifyMessage(string message, string senderPeerId)
    {
        // 防止短時間內重複處理同一玩家的 identify
        float currentTime = Time.time;
        if (lastIdentifyTime.ContainsKey(senderPeerId))
        {
            if (currentTime - lastIdentifyTime[senderPeerId] < IDENTIFY_COOLDOWN)
            {
                Debug.Log($"[NetworkManager] 忽略重複的 identify（冷卻中）: {senderPeerId}");
                return;
            }
        }
        lastIdentifyTime[senderPeerId] = currentTime;

        IdentityMessage identity = JsonUtility.FromJson<IdentityMessage>(message);
        Debug.Log($"Received identify message from {senderPeerId} ({identity.nickname}) is now controlling {identity.characterName}");

        if (peerIdToPlayer.ContainsKey(senderPeerId))
        {
            // 更新玩家資料
            Player oldPlayer = peerIdToPlayer[senderPeerId];
            oldPlayer.name = identity.nickname;
            oldPlayer.skin = identity.characterName;
            Debug.Log($"Player {oldPlayer.name} reconnected!");
            BroadcastInitialToPeer(senderPeerId, oldPlayer.color);
        }
        else
        {
            int randomIndex = UnityEngine.Random.Range(0, selectedSkinColor.Count);
            string chosenColor = selectedSkinColor[randomIndex];

            // 新增新玩家
            Player newPlayer = new()
            {
                name = identity.nickname,
                skin = identity.characterName,
                color = chosenColor,
                point = 0
            };
            playersInfo.Add(newPlayer);
            FindFirstObjectByType<LobbyUI>().UpdateLobbyUI();
            peerIdToPlayer[senderPeerId] = newPlayer;
            selectedSkinColor.RemoveAt(randomIndex);
            BroadcastInitialToPeer(senderPeerId, chosenColor);
        }

        // 若無 Host 或舊 Host 已斷線，則指定新 Host
        if (string.IsNullOrEmpty(hostPeerId) || !peerIdToPlayer.ContainsKey(hostPeerId))
        {
            hostPeerId = senderPeerId;
            Debug.Log($"{senderPeerId} ({identity.nickname}) is now the host.");
        }

        // 每次有玩家 identify（含重連）都廣播 host_update，確保重玩時 Web 端能收到房主資訊
        BroadcastHostUpdate();

        if (gameManager != null && !string.IsNullOrEmpty(gameManager.selectedLevel))
        {
            BroadcastLevelSelected(gameManager.selectedLevel);
        }
    }

    private void HandleMoveMessage(string message, string senderPeerId)
    {
        if (peerIdToPlayer.ContainsKey(senderPeerId))
        {
            Player movingPlayer = peerIdToPlayer[senderPeerId];
            MoveMessage msg = JsonUtility.FromJson<MoveMessage>(message);
            gameManager.OnRemotePlayerMove(movingPlayer.index, msg.vector.x, msg.vector.y, msg.is_press);
        }
    }

    private void HandleSelectLevel(string message, string senderPeerId)
    {
        // 只有主機可以選擇關卡
        if (senderPeerId != hostPeerId)
        {
            Debug.LogWarning($"非主機 {senderPeerId} 嘗試選擇關卡，已忽略");
            return;
        }

        SelectLevelMessage msg = JsonUtility.FromJson<SelectLevelMessage>(message);

        // 驗證關卡是否有效
        if (!System.Array.Exists(GameManager.AvailableLevels, level => level == msg.level))
        {
            Debug.LogWarning($"無效的關卡: {msg.level}");
            return;
        }

        // 存儲選擇的關卡
        if (gameManager != null)
        {
            gameManager.selectedLevel = msg.level;
            Debug.Log($"[NetworkManager] 主機選擇了關卡: {msg.level}");

            // 廣播給所有玩家
            BroadcastLevelSelected(msg.level);
        }
    }

    // ------------- HandleTapAction -------------

    private void HandleTapAction(string senderPeerId)
    {
        if (peerIdToPlayer.ContainsKey(senderPeerId))
        {
            Player player = peerIdToPlayer[senderPeerId];
            var tapManager = FindFirstObjectByType<TapEatGameManager>();
            if (tapManager != null)
            {
                tapManager.OnTapAction(player.index);
            }
        }
    }

    // ------------- HandleDiscardAction -------------

    private void HandleDiscardAction(string senderPeerId)
    {
        if (peerIdToPlayer.ContainsKey(senderPeerId))
        {
            Player player = peerIdToPlayer[senderPeerId];
            var tapManager = FindFirstObjectByType<TapEatGameManager>();
            if (tapManager != null)
            {
                tapManager.OnDiscardAction(player.index);
            }
        }
    }

    // ------------- BroadcastInitialToPeer -------------

    private void BroadcastInitialToPeer(string peerId, string color)
    {
        if (webRTCConnection == null) return;

        InitialMessage initialMessage = new()
        {
            type = "initial",
            color = color
        };
        string jsonMessage = JsonUtility.ToJson(initialMessage);
        webRTCConnection.SendDataChannelMessageToPeer(peerId, jsonMessage);
    }

    // ------------- BroadcastHostUpdate -------------

    private void BroadcastHostUpdate()
    {
        if (hostPeerId == null || webRTCConnection == null) return;
        HostUpdateMessage hostMessage = new()
        {
            type = "host_update",
            hostId = hostPeerId
        };
        string jsonMessage = JsonUtility.ToJson(hostMessage);
        // 改用重試機制，確保手機端一定能收到
        StartCoroutine(BroadcastMessageWithRetry(jsonMessage, 3, 0.3f));
        Debug.Log("Broadcasting Host Update: " + jsonMessage);
    }

    // ------------- BroadcastLevelSelected -------------

    private void BroadcastLevelSelected(string level)
    {
        if (webRTCConnection == null) return;

        LevelSelectedMessage levelMessage = new()
        {
            type = "level_selected",
            level = level
        };
        string jsonMessage = JsonUtility.ToJson(levelMessage);

        // 使用重試機制發送
        StartCoroutine(BroadcastMessageWithRetry(jsonMessage, 3, 0.3f));
    }

    // ------------- HandleNavigateAck -------------

    private void HandleNavigateAck(string message, string senderPeerId)
    {
        NavigateAckMessage ack = JsonUtility.FromJson<NavigateAckMessage>(message);
        string target = ack.target;

        if (!receivedAcks.ContainsKey(target))
        {
            receivedAcks[target] = new HashSet<string>();
        }
        receivedAcks[target].Add(senderPeerId);
        Debug.Log($"[ACK] Received navigate_ack from {senderPeerId} for target: {target} ({receivedAcks[target].Count}/{peerIdToPlayer.Count})");
    }

    /// <summary>
    /// 檢查是否所有玩家都已 ACK
    /// </summary>
    private bool AllPeersAcked(string target)
    {
        if (!receivedAcks.ContainsKey(target)) return false;
        foreach (var peerId in peerIdToPlayer.Keys)
        {
            if (!receivedAcks[target].Contains(peerId)) return false;
        }
        return true;
    }

    // ------------- BroadcastNavigateToLobby -------------

    public void BroadcastNavigateToLobby()
    {
        BaseMessage message = new() { type = "navigate_to_lobby" };
        string jsonMessage = JsonUtility.ToJson(message);
        StartCoroutine(BroadcastMessageWithRetry(jsonMessage, 3, 0.3f));
    }

    // ------------- BroadcastNavigateToGame -------------

    private void BroadcastNavigateToGame()
    {
        receivedAcks.Remove("tutorial");
        StartCoroutine(BroadcastWithAckRetry("navigate_to_game", "tutorial", 5, 2f));
    }

    // ------------- BroadcastNavigateToPlaying -------------

    public void BroadcastNavigateToPlaying()
    {
        receivedAcks.Remove("playing");
        StartCoroutine(BroadcastWithAckRetry("navigate_to_playing", "playing", 5, 2f));
    }

    // ------------- BroadcastTerminate -------------

    public void BroadcastTerminate(string url = "")
    {
        if (webRTCConnection == null) return;

        var sortedPlayers = playersInfo
                .OrderByDescending(p => p.point)
                .ToList();

        List<FinalPlayerData> finalPlayerDatas = new List<FinalPlayerData>();

        int currentRank = 1;
        int previousPoint = -1;
        int playersProcessed = 0;

        foreach (var p in sortedPlayers)
        {
            playersProcessed++;

            if (p.point != previousPoint)
            {
                currentRank = playersProcessed;
                previousPoint = p.point;
            }

            finalPlayerDatas.Add(new FinalPlayerData
            {
                rank = currentRank,
                name = p.name,
                point = p.point,
                color = p.color,
                skin = p.skin
            });
        }

        TerminateMessage terminateMessage = new()
        {
            link = url,
            type = "terminate",
            levelName = gameManager?.selectedLevel ?? "",
            finalPlayerDatas = finalPlayerDatas
        };

        string jsonMessage = JsonUtility.ToJson(terminateMessage);

        receivedAcks.Remove("terminate");
        StartCoroutine(BroadcastMessageWithAckRetry(jsonMessage, "terminate", 5, 2f));
    }


    //------------------------Spy Game-------------------

    public void BroadcastSpyGameInit(Dictionary<int, string> playerRoles)
    {
        if (webRTCConnection == null) return;
        StartCoroutine(BroadcastSpyGameInitWithRetry(playerRoles, 5, 1f));
    }

    private IEnumerator BroadcastSpyGameInitWithRetry(Dictionary<int, string> playerRoles, int maxRetries, float interval)
    {
        // 建立一份所有人暱稱的清單（根據 p.index 確保順序是 0, 1, 2, 3）
        List<string> allNicknames = new List<string>();
        foreach (var p in playersInfo.OrderBy(player => player.index))
        {
            allNicknames.Add(p.name);
        }

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            foreach (var kvp in peerIdToPlayer)
            {
                string peerId = kvp.Key;
                Player p = kvp.Value;

                if (playerRoles.ContainsKey(p.index))
                {
                    SpyGameInitMessage msg = new SpyGameInitMessage
                    {
                        type = "spy_game_init",
                        myPlayerId = p.index,
                        role = playerRoles[p.index],
                        playerNames = allNicknames
                    };
                    webRTCConnection.SendDataChannelMessageToPeer(peerId, JsonUtility.ToJson(msg));
                }
                else
                {
                    Debug.LogWarning($"[SpyGameInit] Player {p.name} (peerId={peerId}) index={p.index} 不在 playerRoles 中！");
                }
            }

            Debug.Log($"[SpyGameInit] 廣播角色分配 (attempt {attempt + 1}/{maxRetries})");

            if (attempt < maxRetries - 1)
            {
                yield return new WaitForSeconds(interval);
            }
        }
    }

    public void BroadcastSpyRoundStart(int roundIndex, int min, int max)
    {
        SpyRoundStartMessage msg = new SpyRoundStartMessage
        {
            type = "spy_round_start",
            roundIndex = roundIndex,
            minTarget = min,
            maxTarget = max
        };
        StartCoroutine(BroadcastMessageWithRetry(JsonUtility.ToJson(msg), 3, 0.3f));
    }

    public void BroadcastSpyVotingStart()
    {
        SpyVotingStartMessage msg = new SpyVotingStartMessage
        {
            type = "spy_voting_start",
            message = "請投票抓出內鬼！"
        };
        StartCoroutine(BroadcastMessageWithRetry(JsonUtility.ToJson(msg), 3, 0.3f));
    }

    public void BroadcastSpyGameReset()
    {
        BaseMessage msg = new BaseMessage { type = "spy_game_reset" };
        StartCoroutine(BroadcastMessageWithRetry(JsonUtility.ToJson(msg), 3, 0.3f));
    }

    // ------------- ACK 重試機制 Coroutines -------------

    /// <summary>
    /// 重複發送簡單訊息，直到所有 peer 回傳 ACK 或達到最大重試次數
    /// </summary>
    private IEnumerator BroadcastWithAckRetry(string messageType, string ackTarget, int maxRetries, float interval)
    {
        if (webRTCConnection == null) yield break;

        BaseMessage message = new() { type = messageType };
        string jsonMessage = JsonUtility.ToJson(message);

        for (int i = 0; i < maxRetries; i++)
        {
            // 只發送給尚未 ACK 的 peer
            foreach (var peerId in peerIdToPlayer.Keys)
            {
                if (receivedAcks.ContainsKey(ackTarget) && receivedAcks[ackTarget].Contains(peerId))
                    continue;
                webRTCConnection.SendDataChannelMessageToPeer(peerId, jsonMessage);
            }
            Debug.Log($"[ACK Retry] Broadcasting {messageType} (attempt {i + 1}/{maxRetries})");

            if (AllPeersAcked(ackTarget))
            {
                Debug.Log($"[ACK] All peers confirmed {ackTarget}!");
                yield break;
            }

            yield return new WaitForSeconds(interval);

            if (AllPeersAcked(ackTarget))
            {
                Debug.Log($"[ACK] All peers confirmed {ackTarget}!");
                yield break;
            }
        }

        Debug.LogWarning($"[ACK] Not all peers confirmed {ackTarget} after {maxRetries} retries.");
    }

    /// <summary>
    /// 重複發送已序列化的 JSON 訊息，直到所有 peer 回傳 ACK 或達到最大重試次數
    /// </summary>
    private IEnumerator BroadcastMessageWithAckRetry(string jsonMessage, string ackTarget, int maxRetries, float interval)
    {
        if (webRTCConnection == null) yield break;

        for (int i = 0; i < maxRetries; i++)
        {
            foreach (var peerId in peerIdToPlayer.Keys)
            {
                if (receivedAcks.ContainsKey(ackTarget) && receivedAcks[ackTarget].Contains(peerId))
                    continue;
                webRTCConnection.SendDataChannelMessageToPeer(peerId, jsonMessage);
            }
            Debug.Log($"[ACK Retry] Broadcasting message (attempt {i + 1}/{maxRetries})");

            if (AllPeersAcked(ackTarget))
            {
                Debug.Log($"[ACK] All peers confirmed {ackTarget}!");
                yield break;
            }

            yield return new WaitForSeconds(interval);

            if (AllPeersAcked(ackTarget))
            {
                Debug.Log($"[ACK] All peers confirmed {ackTarget}!");
                yield break;
            }
        }

        Debug.LogWarning($"[ACK] Not all peers confirmed {ackTarget} after {maxRetries} retries.");
    }

    /// <summary>
    /// 簡單重試廣播（不需要 ACK 的訊息，例如 level_selected）
    /// </summary>
    private IEnumerator BroadcastMessageWithRetry(string jsonMessage, int retryCount, float interval)
    {
        if (webRTCConnection == null) yield break;

        for (int i = 0; i < retryCount; i++)
        {
            webRTCConnection.SendDataChannelMessage(jsonMessage);
            Debug.Log($"Broadcasting message (attempt {i + 1}/{retryCount})");

            if (i < retryCount - 1)
            {
                yield return new WaitForSeconds(interval);
            }
        }
    }

    // -------------------- Reset ---------------------

    public void ResetNetworkData()
    {
        Debug.Log("ResetNetworkData()");

        playersInfo.Clear();
        peerIdToPlayer.Clear();
        lastIdentifyTime.Clear();
        receivedAcks.Clear();
        selectedSkinColor = new List<string>() { "green", "yellow", "blue", "red" };
        hostPeerId = null;
    }

    // ------------- Dont Destroy On Load -------------

    private static NetworkManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
