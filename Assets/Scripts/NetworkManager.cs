using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleWebRTC;
using UnityEngine.SceneManagement;

#region MessageTypeClasses
[System.Serializable]
public class BaseMessage { public string type; }
[System.Serializable]
public class IdentityMessage : BaseMessage { public string characterName; public string nickname; }
[System.Serializable]
public class Vector2Data { public float x; public float y; }
[System.Serializable]
public class MoveMessage : BaseMessage { public Vector2Data vector; }
[System.Serializable]
public class HostUpdateMessage { public string type; public string hostId; }
[System.Serializable]
public class InitialMessage { public string type; public string color; }
#endregion

public class NetworkManager : MonoBehaviour
{
    [Header("WebRTCConnection")]
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
        if (peerIdToPlayer.ContainsKey(senderPeerId))
        {
            Player leavingPlayer = peerIdToPlayer[senderPeerId];
            playersInfo.Remove(leavingPlayer);

            peerIdToPlayer.Remove(senderPeerId);

            if (hostPeerId == senderPeerId)
            {
                Debug.LogWarning("Host has disconnected. Clearing host.");
                hostPeerId = null;
            }
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
        if (hostPeerId == null)
        {
            hostPeerId = senderPeerId;
            Debug.Log($"{senderPeerId} ({identity.nickname}) is now the host.");
            BroadcastHostUpdate();
        }
    }

    private void HandleMoveMessage(string message, string senderPeerId)
    {
        // 僅傳遞資料到 GameLogicManager，由它控制角色
        if (peerIdToPlayer.ContainsKey(senderPeerId))
        {
            Player movingPlayer = peerIdToPlayer[senderPeerId];
            MoveMessage msg = JsonUtility.FromJson<MoveMessage>(message);
            gameManager.OnRemotePlayerMove(movingPlayer.skin, msg.vector.x, msg.vector.y);
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

    // ------------- BroadcastNavigateToGame -------------

    private void BroadcastNavigateToGame()
    {
        if (webRTCConnection == null) return;

        BaseMessage navigateMessage = new() { type = "navigate_to_game" };
        string jsonMessage = JsonUtility.ToJson(navigateMessage);

        webRTCConnection.SendDataChannelMessage(jsonMessage);
        Debug.Log("Broadcasting Navigate to Game: " + jsonMessage);
    }

    // ------------- BroadcastNavigateToPlaying -------------

    public void BroadcastNavigateToPlaying()
    {
        if (webRTCConnection == null) return;

        BaseMessage navigateMessage = new() { type = "navigate_to_playing" };
        string jsonMessage = JsonUtility.ToJson(navigateMessage);

        webRTCConnection.SendDataChannelMessage(jsonMessage);
        Debug.Log("Broadcasting Navigate to Playing: " + jsonMessage);
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
