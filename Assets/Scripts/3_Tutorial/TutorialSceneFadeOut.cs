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

    /// <summary>
    /// 動態設定下一個場景名稱
    /// </summary>
    public void SetNextScene(string sceneName)
    {
        nextSceneName = sceneName;
        Debug.Log($"[TutorialSceneFadeOut] 下一個場景設定為: {sceneName}");
    }

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
