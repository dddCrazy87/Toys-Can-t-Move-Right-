using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStartUI : MonoBehaviour
{
    UIFadeScript fadeScript;
    public float fadeInSpeed, fadeOutSpeed;
    private bool isHide = false;
    public AudioSource startAudio;
    void Start() {
        isHide = false;
        if (!(fadeScript = GetComponent<UIFadeScript>())) {
            fadeScript = gameObject.AddComponent<UIFadeScript>();
        }
    }
    public void HideCanvas() {
        fadeScript.HideUI(fadeOutSpeed);
        FindFirstObjectByType<BgmPlayer>().PauseBGM();
        startAudio.Play();
        Invoke("ChangeScene", 1 / fadeOutSpeed + fadeOutSpeed);
    }
    public string nextSceneName = "";
    void ChangeScene() {
        FindFirstObjectByType<BgmPlayer>().PlayBGM();
        SceneManager.LoadScene(nextSceneName);
    }
}
