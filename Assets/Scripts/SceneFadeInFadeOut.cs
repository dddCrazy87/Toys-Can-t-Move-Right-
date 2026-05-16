using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFadeInFadeOut : MonoBehaviour
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

    void Start()
    {
        fadeScript.gameObject.SetActive(true);
        fadeScript.HideUI(fadeOutSpeed);
    }

    public string nextSceneName = "";
    public void LoadNextSceneWithFadeOut()
    {
        gameObject.SetActive(true);
        fadeScript.ShowUI(fadeInSpeed);
        Invoke(nameof(LoadNextScene), 1 / fadeInSpeed);
    }
    private void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
