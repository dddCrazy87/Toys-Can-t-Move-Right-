using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManager : MonoBehaviour
{
    List<Player> playersInfo = new();
    void Start() {
        playersInfo = new () {
            new Player { name = "yellowP", skin = "yellow" },
            new Player { name = "blueP",   skin = "blue" },
            // new Player { name = "redP",    skin = "red" },
            // new Player { name = "greenP",  skin = "green" },
        };
        FindFirstObjectByType<GameManager>().LoadPlayerData(playersInfo);
        Invoke(nameof(BlueRegistered), 7f);
    }

    public AudioSource registeredSound;
    public Sprite blueRegistered, yellowRegistered;
    public Image blueQrcode, yellowQrcode;
    void BlueRegistered() {
        registeredSound.Play();
        blueQrcode.sprite = blueRegistered;
        Invoke(nameof(YellowRegistered), 3f);
    }

    void YellowRegistered() {
        registeredSound.Play();
        yellowQrcode.sprite = yellowRegistered;
        Invoke(nameof(StartToyBoxGame), 1.5f);
    }

    public Transform toyboxCameraPostion;
    public CameraMovement cameraMovement;
    public SceneFadeInFadeOut sceneFadeInFadeOut;
    void StartToyBoxGame() {
        cameraMovement.MoveToTarget(toyboxCameraPostion);
        cameraMovement.OnMovementComplete += () => {
            FindFirstObjectByType<BgmPlayer>().ChangeBgm("Toybox");
            sceneFadeInFadeOut.LoadNextSceneWithFadeOut();
        };
    }
}
