using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private QrCodeGenerator qrCodeGenerator;
    [SerializeField] private NetworkManager networkManagerRef;
    private NetworkManager networkManager;
    void Awake()
    {
        // 優先用 DontDestroyOnLoad 的 NetworkManager，找不到才用 SerializeField 的
        networkManager = FindFirstObjectByType<NetworkManager>() ?? networkManagerRef;

        // 如果 NetworkManager 已經有連線中的玩家（重玩），保留連線並顯示玩家
        if (networkManager != null && networkManager.playersInfo.Count > 0)
        {
            Debug.Log("[LobbyManager] 已有玩家連線，顯示現有玩家");

            // 用現有的 peerId 重新產生 QR code（讓新玩家也能加入）
            string existingPeerId = networkManager.unityPeerId;
            qrCodeGenerator.EncodeTextToQrCode("https://toys-dont-move-right.vercel.app/enter-name?peerId=" + existingPeerId);

            // 更新大廳 UI 顯示已連線的玩家
            LobbyUI lobbyUI = FindFirstObjectByType<LobbyUI>();
            if (lobbyUI != null)
            {
                lobbyUI.UpdateLobbyUI();
            }
            return;
        }

        string roomId = System.Guid.NewGuid().ToString("N")[..8];
        string unityPeerId = $"unity-{roomId}";
        qrCodeGenerator.EncodeTextToQrCode("https://toys-dont-move-right.vercel.app/enter-name?peerId=" + unityPeerId);
        //for debug
        // qrCodeGenerator.EncodeTextToQrCode("https://toys-dont-move-right.vercel.app/enter-name?peerId=" + unityPeerId + "&debug=true");
        networkManager.SetwebRTCConnection(unityPeerId);
    }

    private int lastPlayerCount = 0;

    [System.Serializable]
    public class PlayerCountMessage
    {
        public string type = "player_count_update";
        public int count;
    }

    private void Start()
    {
        BgmPlayer bgmPlayer = FindFirstObjectByType<BgmPlayer>();
        if (bgmPlayer) bgmPlayer.ChangeBgm();
    }

    void Update()
    {
        if (networkManager != null && networkManager.playersInfo.Count != lastPlayerCount)
        {
            lastPlayerCount = networkManager.playersInfo.Count;
            BroadcastPlayerCount(lastPlayerCount);
        }
    }

    private void BroadcastPlayerCount(int count)
    {
        PlayerCountMessage msg = new() { count = count };
        string json = JsonUtility.ToJson(msg);

        // 傳 null 代表廣播給所有已連線的 WebRTC 節點 (所有手機)
        networkManager.webRTCConnection.SendDataChannelMessage(json);
    }
}
