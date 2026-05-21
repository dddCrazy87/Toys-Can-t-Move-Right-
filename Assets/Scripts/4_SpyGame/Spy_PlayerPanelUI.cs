using UnityEngine;
using TMPro;

public class Spy_PlayerPanelUI : MonoBehaviour
{
    [Header("玩家設定")]
    public int myPlayerId; // 請在 Inspector 中手動設定為 0, 1, 2, 3

    [Header("UI 參考")]
    public TextMeshProUGUI roleText;           // 顯示身分
    public GameObject numberPanel;  // 裝 1~5 按鈕的容器
    public GameObject votePanel;    // 裝 投票 按鈕的容器
    public TextMeshProUGUI statusText;         // 顯示提示，例如 "已選擇 3，等待其他人..."

    private void Start()
    {
        // 初始隱藏
        numberPanel.SetActive(false);
        votePanel.SetActive(false);
        roleText.text = "等待遊戲開始...";
        statusText.text = "";

        // 訂閱 GameManager 的事件
        SpyGameManager.Instance.OnGameStarted += HandleGameStarted;
        SpyGameManager.Instance.OnRoundStarted += HandleRoundStarted;
        SpyGameManager.Instance.OnVotingPhaseStarted += HandleVotingStarted;
        SpyGameManager.Instance.OnGameEnded += HandleGameEnded;
    }

    private void OnDestroy()
    {
        // 記得註銷事件，避免報錯
        if (SpyGameManager.Instance != null)
        {
            SpyGameManager.Instance.OnGameStarted -= HandleGameStarted;
            SpyGameManager.Instance.OnRoundStarted -= HandleRoundStarted;
            SpyGameManager.Instance.OnVotingPhaseStarted -= HandleVotingStarted;
            SpyGameManager.Instance.OnGameEnded -= HandleGameEnded;
        }
    }

    private void HandleGameStarted()
    {
        // 從 GameManager 取得自己的身分
        Role myRole = SpyGameManager.Instance.players[myPlayerId].role;
        roleText.text = myRole == Role.BadGuy ? "我是壞人" : "我是好人";

        // 如果是壞人，可以用顏色標示一下
        roleText.color = myRole == Role.BadGuy ? Color.red : Color.blue;
    }

    private void HandleRoundStarted(int minTarget, int maxTarget)
    {
        numberPanel.SetActive(true);
        votePanel.SetActive(false);
        statusText.text = "請選擇數字...";

        foreach (Transform child in numberPanel.transform) child.gameObject.SetActive(true);
    }

    private void HandleVotingStarted()
    {
        numberPanel.SetActive(false);
        votePanel.SetActive(true);
        statusText.text = "請投票抓出壞人！";

        foreach (Transform child in votePanel.transform) child.gameObject.SetActive(true);
    }

    private void HandleGameEnded(GameResult result)
    {
        numberPanel.SetActive(false);
        votePanel.SetActive(false);
        statusText.text = "遊戲結束。";
    }

    // ==========================================
    // ▼ 給按鈕綁定的 OnClick 事件 ▼
    // ==========================================

    // 將 1~5 的按鈕分別綁定這個方法，並在 OnClick 的參數填入 1, 2, 3, 4, 5
    public void OnNumberButtonClicked(int number)
    {
        statusText.text = $"已選擇 {number}，等待其他人...";
        numberPanel.SetActive(false); // 選完後先隱藏按鈕，防止重複點擊

        SpyGameManager.Instance.SubmitNumber(myPlayerId, number);
    }

    // 將四個投票按鈕分別綁定這個方法，並在 OnClick 的參數填入 0, 1, 2, 3
    public void OnVoteButtonClicked(int targetPlayerId)
    {
        statusText.text = $"已投票給 P{targetPlayerId}，等待結算...";
        votePanel.SetActive(false); // 投完先隱藏

        SpyGameManager.Instance.SubmitVote(myPlayerId, targetPlayerId);
    }
}
