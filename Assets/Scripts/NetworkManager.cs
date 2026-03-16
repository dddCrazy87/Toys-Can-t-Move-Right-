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
public class TerminateMessage { public string type; public List<FinalPlayerData> finalPlayerDatas; }
[System.Serializable]
public class SelectLevelMessage : BaseMessage { public string level; }
[System.Serializable]
public class LevelSelectedMessage { public string type; public string level; }
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

    private static string hostPeerId = null;
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
    public void SetwebRTCConnection(string unityPeerId)
    {
        webRTCConnection.SetUniquePlayerName(unityPeerId);
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
                        BroadcastNavigateToGame();
                        gameManager.UpdatePlayerInfo(playersInfo);
                        SceneManager.LoadScene("3_Tutorial");
                    }
                    break;

                case "move":
                case "manualMove":
                    HandleMoveMessage(message, senderPeerId);
                    break;

                case "select_level":
                    HandleSelectLevel(message, senderPeerId);
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

    private void HandleIdentifyMessage(string message, string senderPeerId)
    {
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

        // 若無 Host 則指定
        if (hostPeerId == null || senderPeerId == hostPeerId)
        {
            hostPeerId = senderPeerId;
            Debug.Log($"{senderPeerId} ({identity.nickname}) is now the host.");
            BroadcastHostUpdate();
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
        webRTCConnection.SendDataChannelMessage(jsonMessage);
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

    // ------------- BroadcastNavigateToGame -------------

    private void BroadcastNavigateToGame()
    {
        StartCoroutine(BroadcastWithRetry("navigate_to_game", 3, 0.3f));
    }

    // ------------- BroadcastNavigateToPlaying -------------

    public void BroadcastNavigateToPlaying()
    {
        StartCoroutine(BroadcastWithRetry("navigate_to_playing", 3, 0.3f));
    }

    // ------------- BroadcastTerminate -------------

    public void BroadcastTerminate()
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
            type = "terminate",
            finalPlayerDatas = finalPlayerDatas
        };

        string jsonMessage = JsonUtility.ToJson(terminateMessage);

        // 使用重試機制發送
        StartCoroutine(BroadcastMessageWithRetry(jsonMessage, 3, 0.3f));
    }

    // ------------- 重試機制 Coroutines -------------

    /// <summary>
    /// 重複發送簡單訊息（只有 type）
    /// </summary>
    private IEnumerator BroadcastWithRetry(string messageType, int retryCount, float interval)
    {
        if (webRTCConnection == null) yield break;

        BaseMessage message = new() { type = messageType };
        string jsonMessage = JsonUtility.ToJson(message);

        for (int i = 0; i < retryCount; i++)
        {
            webRTCConnection.SendDataChannelMessage(jsonMessage);
            Debug.Log($"Broadcasting {messageType} (attempt {i + 1}/{retryCount}): {jsonMessage}");

            if (i < retryCount - 1)
            {
                yield return new WaitForSeconds(interval);
            }
        }
    }

    /// <summary>
    /// 重複發送已序列化的 JSON 訊息
    /// </summary>
    private IEnumerator BroadcastMessageWithRetry(string jsonMessage, int retryCount, float interval)
    {
        if (webRTCConnection == null) yield break;

        for (int i = 0; i < retryCount; i++)
        {
            webRTCConnection.SendDataChannelMessage(jsonMessage);
            Debug.Log($"Broadcasting message (attempt {i + 1}/{retryCount}): {jsonMessage}");

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
