using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SimpleWebRTC;
using UnityEngine.SceneManagement; 
using Random = UnityEngine.Random;

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
#endregion


public class GameManager : MonoBehaviour
{
    public bool isGameStart = false;

    [Header("Network Settings")]
    [Tooltip("拖入 QrCodeGenerator")]
    public QrCodeGenerator qrCodeGenerator;
    [Tooltip("拖入 WebRTCConnection")]
    public WebRTCConnection webRTCConnection;

    private static string hostPeerId = null;
    private Dictionary<string, Player> peerIdToPlayer = new Dictionary<string, Player>();
    public Dictionary<string, PlayerController> playerControllers = new Dictionary<string, PlayerController>();


    public void StartGame()
    {
        if (playersInfo.Count <= 0)
        {
            print("No player registered");
            return;
        }
        // 設定玩家Id
        AssignPlayerIndex();
        // 生成分數UI
        RenderPointUI();
        // 分配出生點
        AssignSpawnPoints();
        // 生成玩家
        SpawnPlayers();
        // 生成道具
        ;
        if (itemManager = FindFirstObjectByType<ItemManager>())
        {
            itemManager.StartSpawnItems();
        }
        else
        {
            print("No Item Manager");
        }
    }

    public void LoadPlayerData(List<Player> players)
    {
        playersInfo = players;
        if (playersInfo.Count < 2 || playersInfo.Count > 4)
        {
            Debug.LogWarning("超過限制");
            return;
        }
    }

    // ------------- Increase Player Point --------------
    public void PlayerIncreasePointByNumber(int playerIndex, int number)
    {
        playersInfo[playerIndex].point += number;
        playerPointUI[playerIndex].GetChild(1).GetComponent<TextMeshProUGUI>().text = playersInfo[playerIndex].point.ToString();
    }

    // ------- Assign Player Index -------

    [Header("Player Data")]
    public List<Player> playersInfo = new List<Player>();
    public GameObject yellowPlayersPrefab, bluePlayersPrefab, greenPlayersPrefab, redPlayersPrefab;
    void AssignPlayerIndex()
    {
        for (int i = 0; i < playersInfo.Count; i++)
        {
            playersInfo[i].index = i;
        }
    }

    // --------- Spawn Players -----------

    [Header("Player Spawn Setting")]
    public List<Transform> playerSpawnPoints = new();
    private void AssignSpawnPoints()
    {
        playerSpawnPoints.Clear();
        GameObject go = GameObject.Find("PlayerSpwanPoint");
        if (go != null)
        {
            Transform tf = go.transform;
            for (int i = 0; i < tf.childCount; ++i)
            {
                playerSpawnPoints.Add(tf.GetChild(i));
            }
        }
        else
        {
            print("No Player Spwan Point");
            return;
        }

        List<Transform> availablePoints = new(playerSpawnPoints);
        System.Random rnd = new();

        foreach (var player in playersInfo)
        {
            int index = rnd.Next(availablePoints.Count);
            player.spawnPoint = availablePoints[index].position;
            availablePoints.RemoveAt(index);
        }
    }

    private void SpawnPlayers()
    {
        foreach (var player in playersInfo)
        {
            GameObject go = null;
            switch (player.skin)
            {
                case "yellow":
                    go = Instantiate(yellowPlayersPrefab, player.spawnPoint, Quaternion.identity);
                    // go.GetComponent<PlayerController>().isP1 = true;
                    break;
                case "blue":
                    go = Instantiate(bluePlayersPrefab, player.spawnPoint, Quaternion.identity);
                    // go.GetComponent<PlayerController>().isP2 = true;
                    break;
                case "green":
                    go = Instantiate(greenPlayersPrefab, player.spawnPoint, Quaternion.identity);
                    // Destroy(go.GetComponent<PlayerController>());
                    break;
                case "red":
                    go = Instantiate(redPlayersPrefab, player.spawnPoint, Quaternion.identity);
                    // Destroy(go.GetComponent<PlayerController>());
                    break;
                default:
                    break;
            }
            if (go != null)
            {
                PlayerController pc = go.GetComponent<PlayerController>();
                pc.Initialize(player.name, player.index);

                playerControllers[player.skin] = pc;
            }
            // go.GetComponent<PlayerController>().Initialize(player.name, player.index);
        }
    }

    public PlayerController GetPlayerControllerBySkin(string skin)
    {
        if (playerControllers.ContainsKey(skin))
        {
            return playerControllers[skin];
        }
        return null;
    }

    // ------------- UI Setting -------------

    [Header("UI Setting")]
    public List<Transform> pointUiTypes = new();
    public List<Sprite> pointUiSkins = new();
    List<Transform> playerPointUI = new();
    private void RenderPointUI()
    {
        if (playersInfo == null || playersInfo.Count < 2)
        {
            Debug.LogWarning($"Player ({playersInfo?.Count}) is less than 2 players.");
            foreach (var item in pointUiTypes)
            {
                item.gameObject.SetActive(false);
            }
            return;
        }
        int uiIndex = playersInfo.Count - 2;
        if (uiIndex < 0 || uiIndex >= pointUiTypes.Count)
        {
            Debug.LogError($"Can't find {playersInfo.Count} players' UI (index {uiIndex})");
            return;
        }

        pointUiTypes[uiIndex].gameObject.SetActive(true);

        for (int i = 0; i < playersInfo.Count; i++)
        {
            playerPointUI.Add(pointUiTypes[playersInfo.Count - 2].GetChild(i));
        }
        for (int i = 0; i < playersInfo.Count; i++)
        {
            switch (playersInfo[i].skin)
            {
                case "blue":
                    playerPointUI[i].GetChild(0).GetComponent<Image>().sprite = pointUiSkins[0];
                    break;
                case "yellow":
                    playerPointUI[i].GetChild(0).GetComponent<Image>().sprite = pointUiSkins[1];
                    break;
                case "green":
                    playerPointUI[i].GetChild(0).GetComponent<Image>().sprite = pointUiSkins[2];
                    break;
                case "red":
                    playerPointUI[i].GetChild(0).GetComponent<Image>().sprite = pointUiSkins[3];
                    break;
                default:
                    break;
            }
            playerPointUI[i].GetChild(1).GetComponent<TextMeshProUGUI>().text = playersInfo[i].point.ToString();
        }
    }

    // ----------- Item Script -----------

    [Header("Item Script")]
    public ItemManager itemManager;

    // ------------- Dont Destroy On Load -------------

    private static GameManager instance;

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

    void Start()
    {
        WebRTCManager.OnDataMessageReceived_Static += OnDataReceived;
        WebRTCManager.OnPeerDisconnected_Static += OnPeerDisconnected;

        string roomId = System.Guid.NewGuid().ToString("N")[..8];
        string unityPeerId = $"unity-{roomId}";

        if (qrCodeGenerator == null || webRTCConnection == null)
        {
            Debug.LogError("GameManager requires References (QrCodeGenerator, WebRTCConnection)");
            return;
        }

        qrCodeGenerator.EncodeTextToQrCode("https://web-toy-cant-move.vercel.app/?roomId=" + roomId);
        webRTCConnection.SetUniquePlayerName(unityPeerId);
        webRTCConnection.Connect();
    }

    void OnDestroy()
    {
        WebRTCManager.OnDataMessageReceived_Static -= OnDataReceived;
        WebRTCManager.OnPeerDisconnected_Static -= OnPeerDisconnected;
    }

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

    private void OnDataReceived(string message, string senderPeerId)
    {
        try
        {
            BaseMessage data = JsonUtility.FromJson<BaseMessage>(message);

            if (data.type == "identify")
            {
                IdentityMessage identity = JsonUtility.FromJson<IdentityMessage>(message);
                Debug.Log($"Received identify message from {senderPeerId} ({identity.nickname}) is now controlling {identity.characterName}");

                if (peerIdToPlayer.ContainsKey(senderPeerId))
                {
                    Debug.Log($"PeerId {senderPeerId} re-identifying, updating player info.");
                    Player oldPlayer = peerIdToPlayer[senderPeerId];
                    oldPlayer.name = identity.nickname;
                    oldPlayer.skin = identity.characterName;
                }
                else
                {
                    Player newPlayer = new Player
                    {
                        name = identity.nickname,
                        skin = identity.characterName,
                        point = 0
                    };
                    playersInfo.Add(newPlayer);

                    peerIdToPlayer[senderPeerId] = newPlayer;
                }

                if (hostPeerId == null)
                {
                    hostPeerId = senderPeerId;
                    Debug.Log($"{senderPeerId} ({identity.nickname}) is now the host.");
                    BroadcastHostUpdate();
                }
            }
            else if (data.type == "start_game")
            {
                if (senderPeerId == hostPeerId)
                {
                    Debug.Log("Host Requested Start Game！");
                    BroadcastNavigateToGame();
                    StartCoroutine(LoadGameSceneAndStart("Toybox"));
                }
            }
            else if (data.type == "move" || data.type == "manualMove")
            {
                if (peerIdToPlayer.ContainsKey(senderPeerId))
                {
                    Player movingPlayer = peerIdToPlayer[senderPeerId];
                    if (playerControllers.ContainsKey(movingPlayer.skin))
                    {
                        PlayerController pc = playerControllers[movingPlayer.skin];
                        MoveMessage msg = JsonUtility.FromJson<MoveMessage>(message);
                        pc.SetNetworkInput(msg.vector.x, msg.vector.y);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to process message ({senderPeerId}): {ex.Message} - {message}");
        }
    }

    private void BroadcastHostUpdate()
    {
        if (hostPeerId == null || webRTCConnection == null) return;
        HostUpdateMessage hostMessage = new HostUpdateMessage
        {
            type = "host_update",
            hostId = hostPeerId
        };
        string jsonMessage = JsonUtility.ToJson(hostMessage);
        webRTCConnection.SendDataChannelMessage(jsonMessage);
        Debug.Log("Broadcasting Host Update: " + jsonMessage);
    }
    
    private void BroadcastNavigateToGame()
    {
        if (webRTCConnection == null) return;
        
        BaseMessage navigateMessage = new BaseMessage { type = "navigate_to_game" };
        string jsonMessage = JsonUtility.ToJson(navigateMessage);
        
        webRTCConnection.SendDataChannelMessage(jsonMessage);
        Debug.Log("Broadcasting Navigate to Game: " + jsonMessage);
    }
    
    private IEnumerator LoadGameSceneAndStart(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        StartGame();
    }
}



[System.Serializable]
public class Player {
    public string name = "";
    public string skin = "";
    public Vector3 spawnPoint;
    public int point = 0;
    public int index;
}
