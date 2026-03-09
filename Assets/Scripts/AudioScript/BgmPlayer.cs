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

    [Header("各場景 BGM")]
    public AudioClip gameStartBgm;      // 預設（大廳、設定、教學等）
    public AudioClip toyboxBgm;         // 4_Toybox
    public AudioClip colorPaperBgm;     // 4_ColorPaper
    public AudioClip leaderboardBgm;    // 5_GameRestart（頒獎）

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
            "4_Toybox" => toyboxBgm,
            "4_ColorPaper" => colorPaperBgm,
            "5_GameRestart" => leaderboardBgm,
            _ => gameStartBgm  // 預設（大廳、設定、教學等）
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
