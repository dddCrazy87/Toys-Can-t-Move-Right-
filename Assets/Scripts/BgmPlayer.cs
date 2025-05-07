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
    // public 
    public void PauseBGM() {
        bgm.Pause();
    }
    public void PlayBGM() {
        bgm.Play();
    }
}
