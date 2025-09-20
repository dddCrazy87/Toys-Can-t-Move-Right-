using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManager : MonoBehaviour
{
    List<Player> playersInfo = new();
    [SerializeField] private QrCodeGenerator qrCodeGenerator;
    void Start()
    {
        playersInfo = new() {
            new Player { name = "yellowP", skin = "yellow" },
            new Player { name = "blueP",   skin = "blue" },
            // new Player { name = "redP",    skin = "red" },
            // new Player { name = "greenP",  skin = "green" },
        };
        // FindFirstObjectByType<GameManager>().LoadPlayerData(playersInfo);
        // Invoke(nameof(BlueRegistered), 7f);

        string roomId = System.Guid.NewGuid().ToString("N")[..8];
        string unityPeerId = $"unity-{roomId}";
        qrCodeGenerator.EncodeTextToQrCode("https://dddcrazy87.github.io/Web-for-toy-cant-move/?roomId="
                                            + roomId + "&unityPeerId=" + unityPeerId);
    }

    public AudioSource registeredSound;
    public Sprite blueRegistered, yellowRegistered;
    public Image blueQrcode, yellowQrcode;
    void BlueRegistered()
    {
        registeredSound.Play();
        blueQrcode.sprite = blueRegistered;
        Invoke(nameof(YellowRegistered), 3f);
    }

    void YellowRegistered()
    {
        registeredSound.Play();
        yellowQrcode.sprite = yellowRegistered;
        Invoke(nameof(StartToyBoxGame), 1.5f);
    }

    public Transform toyboxCameraPostion;
    public CameraMovement cameraMovement;
    public SceneFadeInFadeOut sceneFadeInFadeOut;
    public GameObject ui1, ui2, ui3, ui4;
    void StartToyBoxGame()
    {
        ui1.SetActive(false); ui2.SetActive(false); ui3.SetActive(false); ui4.SetActive(false);
        cameraMovement.MoveToTarget(toyboxCameraPostion);
        cameraMovement.OnMovementComplete += () =>
        {
            FindFirstObjectByType<BgmPlayer>().ChangeBgm("Toybox");
            sceneFadeInFadeOut.LoadNextSceneWithFadeOut();
        };
    }
}
