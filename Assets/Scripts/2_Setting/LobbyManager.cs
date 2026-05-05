using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private QrCodeGenerator qrCodeGenerator;
    [SerializeField] private NetworkManager networkManager;
    void Awake()
    {
        string roomId = System.Guid.NewGuid().ToString("N")[..8];
        string unityPeerId = $"unity-{roomId}";
        qrCodeGenerator.EncodeTextToQrCode("https://web-toy-cant-move.vercel.app/enter-name?peerId=" + unityPeerId);
        //for debug
        // qrCodeGenerator.EncodeTextToQrCode("https://web-toy-cant-move.vercel.app/enter-name?peerId=" + unityPeerId + "&debug=true"); 
        networkManager.SetwebRTCConnection(unityPeerId);
    }
}
