using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStartUI : MonoBehaviour
{
    UIFadeScript fadeScript;
    public float fadeInSpeed, fadeOutSpeed;
    public AudioSource startAudio;
    void Awake()
    {
        if (!(fadeScript = GetComponent<UIFadeScript>()))
        {
            fadeScript = gameObject.AddComponent<UIFadeScript>();
        }
        FindFirstObjectByType<BgmPlayer>().ChangeBgm();
    }
    public void HideCanvas()
    {
        fadeScript.HideUI(fadeOutSpeed);
        FindFirstObjectByType<BgmPlayer>().PauseBGM();
        startAudio.Play();
        Invoke(nameof(ChangeScene), 1 / fadeOutSpeed + fadeOutSpeed);
    }
    public string nextSceneName = "";
    void ChangeScene()
    {
        FindFirstObjectByType<BgmPlayer>().PlayBGM();
        SceneManager.LoadScene(nextSceneName);
    }
}
