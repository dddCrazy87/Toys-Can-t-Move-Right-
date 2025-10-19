using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private QrCodeGenerator qrCodeGenerator;
    [SerializeField] private NetworkManager networkManager;
    void Start()
    {
        string roomId = System.Guid.NewGuid().ToString("N")[..8];
        string unityPeerId = $"unity-{roomId}";
        qrCodeGenerator.EncodeTextToQrCode("https://web-toy-cant-move.vercel.app/?roomId=" + roomId);
        networkManager.SetwebRTCConnection(unityPeerId);
    }
}
