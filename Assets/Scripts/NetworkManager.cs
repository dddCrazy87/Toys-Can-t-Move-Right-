using UnityEngine;
using System;
using System.Collections.Generic;
using SimpleWebRTC;

public class NetworkManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private QrCodeGenerator qrCodeGenerator;
    [SerializeField] private WebRTCConnection webRTCConnection;
    [SerializeField] private GameManager gameManager;

    private static string hostPeerId = null;
    private List<Player> pendingPlayers = new List<Player>();
    private Dictionary<string, string> peerIdToCharacter = new Dictionary<string, string>();


    void Start()
    {
        WebRTCManager.OnDataMessageReceived_Static += OnDataReceived;

        if (qrCodeGenerator == null || webRTCConnection == null || gameManager == null)
        {
            Debug.LogError("NetworkManager requires References (QrCodeGenerator, WebRTCConnection, GameManager)！");
            return;
        }

        string roomId = System.Guid.NewGuid().ToString("N")[..8];
        qrCodeGenerator.EncodeTextToQrCode("https://web-toy-cant-move.vercel.app/?roomId=" + roomId);
        
        string unityPeerId = $"unity-{roomId}";
        webRTCConnection.SetUniquePlayerName(unityPeerId);
        webRTCConnection.Connect();
    }

    void OnDestroy()
    {
        WebRTCManager.OnDataMessageReceived_Static -= OnDataReceived;
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

                Player newPlayer = new Player
                {
                    name = identity.nickname,
                    skin = identity.characterName
                };
                pendingPlayers.Add(newPlayer);

                // 綁定 peerId -> 角色
                peerIdToCharacter[senderPeerId] = identity.characterName;

                // 指定 Host
                if (hostPeerId == null)
                {
                    hostPeerId = senderPeerId;
                    Debug.Log($" {senderPeerId} ({identity.nickname}) is now the host.");
                    BroadcastHostUpdate();
                }
            }
            else if (data.type == "start_game")
            {
                if (senderPeerId == hostPeerId)
                {
                    Debug.Log("Host 請求開始遊戲！");
                    gameManager.LoadPlayerData(pendingPlayers);
                    gameManager.StartGame();
                }
            }
            else if (data.type == "move" || data.type == "manualMove")
            {
                if (peerIdToCharacter.ContainsKey(senderPeerId))
                {
                    string characterSkin = peerIdToCharacter[senderPeerId];
                    PlayerController pc = gameManager.GetPlayerControllerBySkin(characterSkin);

                    if (pc != null)
                    {
                        MoveMessage msg = JsonUtility.FromJson<MoveMessage>(message);
                        pc.SetNetworkInput(msg.vector.x, msg.vector.y);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"解析訊息失敗 ({senderPeerId}): {ex.Message} - {message}");
        }
    }
    private void BroadcastHostUpdate()
    {
        if (hostPeerId == null || webRTCConnection == null) return;
        HostUpdateMessage hostMessage = new HostUpdateMessage {
            type = "host_update",
            hostId = hostPeerId
        };
        string jsonMessage = JsonUtility.ToJson(hostMessage);
        webRTCConnection.SendDataChannelMessage(jsonMessage);
        Debug.Log("廣播 Host 更新: " + jsonMessage);
    }
}