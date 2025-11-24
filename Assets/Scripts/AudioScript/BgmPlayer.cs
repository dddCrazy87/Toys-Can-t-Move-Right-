using UnityEngine;
using UnityEngine.SceneManagement;

public class BgmPlayer : MonoBehaviour
{
    private static BgmPlayer instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public AudioSource bgm;
    public AudioClip toyboxBgm;
    public AudioClip gameStartBgm;
    public AudioClip leaderboardBgm;

    public void PauseBGM()
    {
        bgm.Pause();
    }
    public void PlayBGM()
    {
        bgm.Play();
    }
    public void ChangeBgm()
    {
        PauseBGM();
        string sceneName = SceneManager.GetActiveScene().name;
        bgm.resource = sceneName switch
        {
            "1_GameStart" => gameStartBgm,
            "4_Toybox" => toyboxBgm,
            "5_GameRestart" => leaderboardBgm,
            _ => toyboxBgm
        };
        Invoke(nameof(PlayBGM), 0.5f);

        // switch (bgmName) {
        //     case "":
        //         PauseBGM();
        //         bgm.resource = toyboxBgm;
        //         Invoke(nameof(PlayBGM), 0.5f);
        //         break;
        //     default:
        //         break;
        // }
    }
}
