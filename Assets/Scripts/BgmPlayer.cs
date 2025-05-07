using UnityEngine;

public class BgmPlayer : MonoBehaviour
{
    private static BgmPlayer instance;

    void Awake() {
        if (instance != null && instance != this) {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public AudioSource bgm;
    public AudioClip toyboxBgm;

    public void PauseBGM() {
        bgm.Pause();
    }
    public void PlayBGM() {
        bgm.Play();
    }
    public void ChangeBgm(string bgmName) {
        switch (bgmName) {
            case "Toybox":
                PauseBGM();
                bgm.resource = toyboxBgm;
                Invoke(nameof(PlayBGM), 0.5f);
                break;
            default:
                break;
        }
    }
}
