using Unity.VisualScripting;
using UnityEngine;

public class UIFadeScript : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private bool fadeIn = false, fadeOut = false;
    private float fadeInSpeed = 1, fadeOutSpeed = 1;
    void Awake()
    {
        if (!(canvasGroup = GetComponent<CanvasGroup>()))
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        fadeIn = fadeOut = false;
        fadeInSpeed = fadeOutSpeed = 1;
    }
    public void ShowUI(float speed)
    {
        canvasGroup.alpha = 0;
        fadeIn = true;
        fadeInSpeed = speed;
    }
    public void HideUI(float speed)
    {
        canvasGroup.alpha = 1;
        fadeOut = true;
        fadeOutSpeed = speed;
    }
    public bool IsFadingOut()
    {
        return fadeOut;
    }
    void Update()
    {
        if (fadeIn)
        {
            if (canvasGroup.alpha < 1)
            {
                canvasGroup.alpha += Time.deltaTime * fadeInSpeed;
            }
            if (canvasGroup.alpha >= 1)
            {
                canvasGroup.alpha = 1;
                fadeIn = false;
            }
        }
        if (fadeOut)
        {
            if (canvasGroup.alpha > 0)
            {
                canvasGroup.alpha -= Time.deltaTime * fadeOutSpeed;
            }
            if (canvasGroup.alpha <= 0)
            {
                canvasGroup.alpha = 0;
                fadeOut = false;
                gameObject.SetActive(false);
            }
        }
    }
}
