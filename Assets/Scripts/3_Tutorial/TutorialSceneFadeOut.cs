using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialSceneFadeOut : MonoBehaviour
{
    UIFadeScript fadeScript;
    public float fadeInSpeed, fadeOutSpeed;
    void Awake()
    {
        gameObject.SetActive(true);
        if (!(fadeScript = GetComponent<UIFadeScript>()))
        {
            fadeScript = gameObject.AddComponent<UIFadeScript>();
        }
    }

    public string nextSceneName = "";
    public void LoadNextSceneWithFadeOut()
    {
        fadeScript.ShowUI(fadeInSpeed);
        Invoke(nameof(LoadNextScene), 1 / fadeInSpeed);
    }
    private void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
