using UnityEngine;

public class CountDownTest : MonoBehaviour
{
    [SerializeField] private CountDownUI countdownUI;
    [SerializeField] private float countdownTime = 90f;

    void Start()
    {
        // 倒數 90 秒（1 分 30 秒）
        countdownUI.StartCountdown(countdownTime, OnCountdownFinished);
    }

    void OnCountdownFinished()
    {
        Debug.Log("倒數結束！");
        // 例如：開始遊戲、顯示新 UI、播放音效...
    }
}
