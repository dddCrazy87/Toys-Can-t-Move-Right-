using UnityEngine;
using TMPro;
using System.Collections.Generic;
using SimpleWebRTC;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Video;
public enum TutorialStep { Calibrate, TiltLeft, TiltRight, MiniGame, Finished }

[System.Serializable]
public class TutorialStepMessage : BaseMessage
{
    public string step; // "forward", "left", "right"
}

[System.Serializable]
public class TutorialInstructionMessage : BaseMessage
{
    public string step;   // forward, left, right, backward, complete
    public string message;
}

public class PlayerTutorialProgress
{
    public Player playerInfo;
    public bool completedCalibration = false;
    public bool completedForward = false;
    public bool completedLeft = false;
    public bool completedRight = false;
    public bool completedBackward = false;

    // 檢查是否完成所有步驟
    public bool IsAllStepsCompleted()
    {
        return completedForward && completedLeft && completedRight && completedBackward && completedCalibration;
    }
}

public class TutorialManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("拖入主要的提示文字 TextMeshProUGUI")]
    public TextMeshProUGUI instructionText;

    [Tooltip("拖入玩家進度列表的容器 (Vertical Layout Group)")]
    public Transform playerProgressContainer;

    [Tooltip("示意動畫的 Animator")]
    public Animator demoAnimator;

    [Tooltip("玩家進度卡片的 Prefab")]
    public GameObject playerProgressCardPrefab;
    [Tooltip("顯示示意圖的 Image 組件")]
    public Image demoImage;

    [Header("Video Player Settings")]
    [Tooltip("拖入 Video Player 組件")]
    public VideoPlayer videoPlayer;

    [Tooltip("拖入顯示影片的 RawImage")]
    public RawImage videoRawImage;

    [Tooltip("各步驟的示範影片")]
    public VideoClip calibrateVideo;
    public VideoClip forwardVideo;
    public VideoClip leftVideo;
    public VideoClip rightVideo;
    public VideoClip backwardVideo;


    [Header("Tutorial Settings")]
    [Tooltip("觸發動作需要的傾斜程度 (0 到 1)")]
    [Range(0.1f, 1.0f)]
    public float tiltThreshold = 0.7f;

    [Tooltip("每個步驟的示意圖")]
    public Sprite[] stepDemoImages; // [0]=向前傾斜, [1]=向左傾斜, [2]=向右傾斜

    // --- 私有變數 ---
    private GameManager gameManager;
    // 儲存 peerId 和教學進度的綁定
    private Dictionary<string, PlayerTutorialProgress> playerProgressMap = new Dictionary<string, PlayerTutorialProgress>();
    // 儲存 peerId 和 UI 卡片的綁定
    private Dictionary<string, PlayerCardUI> playerCardUIMap = new Dictionary<string, PlayerCardUI>();


    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("TutorialManager 找不到 GameManager！");
            return;
        }

        WebRTCManager.OnDataMessageReceived_Static += OnDataReceived;

        if (videoPlayer != null)
        {
            videoPlayer.targetTexture = null; // 使用 API Only 模式
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.loopPointReached += OnVideoFinished;
        }

        InitializePlayerProgress();
    }

    void OnDestroy()
    {
        WebRTCManager.OnDataMessageReceived_Static -= OnDataReceived;

        // 清理 Video Player 事件
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }

    void InitializePlayerProgress()
    {
        // 清除舊的 UI
        foreach (Transform child in playerProgressContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (Player p in gameManager.playersInfo)
        {
            // 找到這個 player 對應的 peerId
            string peerId = gameManager.peerIdToPlayer.FirstOrDefault(x => x.Value == p).Key;
            if (peerId != null)
            {
                // 建立進度追蹤
                PlayerTutorialProgress progress = new PlayerTutorialProgress { playerInfo = p };
                playerProgressMap[peerId] = progress;

                // 建立 UI 卡片
                GameObject cardGO = Instantiate(playerProgressCardPrefab, playerProgressContainer);
                PlayerCardUI cardUI = cardGO.GetComponent<PlayerCardUI>();

                // 綁定 UI 和進度
                playerCardUIMap[peerId] = cardUI;

                // 第一次更新 UI
                cardUI.UpdateProgress(progress);
            }
        }

        // 第一次更新主要提示文字
        UpdateInstructionText();
    }

    private void OnDataReceived(string message, string senderPeerId)
    {
        // 檢查這個訊息是否來自我們正在追蹤的玩家
        if (!playerProgressMap.ContainsKey(senderPeerId)) return;

        try
        {
            BaseMessage data = JsonUtility.FromJson<BaseMessage>(message);

            if (data.type == "move")
            {
                MoveMessage msg = JsonUtility.FromJson<MoveMessage>(message);

                // 取得這個玩家的進度
                PlayerTutorialProgress progress = playerProgressMap[senderPeerId];

                // 如果已經完成，就不再檢查
                if (progress.IsAllStepsCompleted()) return;
                // 如果還沒校正，先標記為已校正
                if (!progress.completedCalibration)
                {
                    progress.completedCalibration = true;
                    Debug.Log($"{progress.playerInfo.name} 已完成校正！");
                    playerCardUIMap[senderPeerId].UpdateProgress(progress); // 更新該玩家的卡片
                    UpdateInstructionText(); // 更新主要提示文字\
                }
                // 檢查是否完成「向前傾斜」
                else if (!progress.completedForward && msg.vector.y < -tiltThreshold)
                {
                    progress.completedForward = true;
                    Debug.Log($"{progress.playerInfo.name} 完成了向前傾斜！");
                    playerCardUIMap[senderPeerId].UpdateProgress(progress); // 更新該玩家的卡片
                    UpdateInstructionText(); // 更新主要提示文字
                    CheckForAllPlayersFinished(); // 檢查是否全部完成
                }

                // 檢查是否完成「向左傾斜」 (必須先完成上一步)
                else if (progress.completedForward && !progress.completedLeft && msg.vector.x < -tiltThreshold)
                {
                    progress.completedLeft = true;
                    Debug.Log($"{progress.playerInfo.name} 完成了向左傾斜！");
                    playerCardUIMap[senderPeerId].UpdateProgress(progress);
                    UpdateInstructionText();
                    CheckForAllPlayersFinished();
                }

                // 檢查是否完成「向右傾斜」 (必須先完成上一步)
                else if (progress.completedLeft && !progress.completedRight && msg.vector.x > tiltThreshold)
                {
                    progress.completedRight = true;
                    Debug.Log($"{progress.playerInfo.name} 完成了向右傾斜！");
                    playerCardUIMap[senderPeerId].UpdateProgress(progress);
                    UpdateInstructionText();
                    CheckForAllPlayersFinished();
                }
                else if (progress.completedRight && !progress.completedBackward && msg.vector.y > tiltThreshold)
                {
                    progress.completedBackward = true;
                    Debug.Log($"{progress.playerInfo.name} 完成了向後傾斜！");
                    playerCardUIMap[senderPeerId].UpdateProgress(progress);
                    UpdateInstructionText();
                    CheckForAllPlayersFinished();
                }
            }
            else if (data.type == "tutorial_step_complete")
            {
                // 手機端明確告知完成某步驟
                TutorialStepMessage msg = JsonUtility.FromJson<TutorialStepMessage>(message);
                PlayerTutorialProgress progress = playerProgressMap[senderPeerId];

                if (msg.step == "forward" && !progress.completedForward)
                {
                    progress.completedForward = true;
                    Debug.Log($"{progress.playerInfo.name} 完成了向前傾斜！");
                    playerCardUIMap[senderPeerId].UpdateProgress(progress);
                    UpdateInstructionText();
                    CheckForAllPlayersFinished();
                }
                else if (msg.step == "left" && progress.completedForward && !progress.completedLeft)
                {
                    progress.completedLeft = true;
                    Debug.Log($"{progress.playerInfo.name} 完成了向左傾斜！");
                    playerCardUIMap[senderPeerId].UpdateProgress(progress);
                    UpdateInstructionText();
                    CheckForAllPlayersFinished();
                }
                else if (msg.step == "right" && progress.completedLeft && !progress.completedRight)
                {
                    progress.completedRight = true;
                    Debug.Log($"{progress.playerInfo.name} 完成了向右傾斜！");
                    playerCardUIMap[senderPeerId].UpdateProgress(progress);
                    UpdateInstructionText();
                    CheckForAllPlayersFinished();
                }
            }
        }
        catch (System.Exception) { /* 忽略格式不符的訊息 */ }
    }

    // 更新主要的教學提示文字
    void UpdateInstructionText()
    {
        string step = "";
        if (playerProgressMap.Values.All(p => p.completedBackward))
        {
            instructionText.text = "太棒了！所有人都完成了訓練！";
            step = "complete";
            if (demoAnimator != null) demoAnimator.SetInteger("StepIndex", -1);
        }
        else if (playerProgressMap.Values.All(p => p.completedRight))
        {
            instructionText.text = "最後一步！向後傾斜手機";
            step = "backward";
            if (demoAnimator != null) demoAnimator.SetInteger("StepIndex", 3);
            PlayStepVideo(step);
        }
        else if (playerProgressMap.Values.All(p => p.completedLeft))
        {
            instructionText.text = "做得好！向右傾斜手機";
            step = "right";
            if (demoAnimator != null) demoAnimator.SetInteger("StepIndex", 2);
            PlayStepVideo(step);
        }
        else if (playerProgressMap.Values.All(p => p.completedForward))
        {
            instructionText.text = "很好！向左傾斜手機";
            step = "left";
            if (demoAnimator != null) demoAnimator.SetInteger("StepIndex", 1);
            PlayStepVideo(step);
        }
        else if (playerProgressMap.Values.All(p => p.completedCalibration))
        {
            instructionText.text = "很棒！向前傾斜手機";
            step = "forward";
            if (demoAnimator != null) demoAnimator.SetInteger("StepIndex", 0);
            PlayStepVideo(step);
        }
        else
        {
            instructionText.text = "請將手機置於平面，按下校正按鈕！";
            step = "calibrate";
            if (demoAnimator != null) demoAnimator.SetInteger("StepIndex", -1);
            PlayStepVideo(step);
        }

        BroadcastTutorialStep(step, instructionText.text);
    }

    void BroadcastTutorialStep(string step, string message)
    {
        TutorialInstructionMessage tutorialMsg = new TutorialInstructionMessage
        {
            type = "tutorial_instruction",
            step = step,
            message = message
        };

        string jsonMessage = JsonUtility.ToJson(tutorialMsg);
        gameManager.webRTCConnection.SendDataChannelMessage(jsonMessage);
        Debug.Log($"Broadcast tutorial step: {step}");
    }

    void UpdateDemoImage(int stepIndex)
    {
        if (demoImage != null && stepDemoImages != null && stepIndex < stepDemoImages.Length)
        {
            demoImage.sprite = stepDemoImages[stepIndex];
            demoImage.gameObject.SetActive(true);
        }
    }

    void CheckForAllPlayersFinished()
    {
        // 檢查是否「所有」玩家都完成了「所有」步驟
        bool allFinished = playerProgressMap.Values.All(p => p.IsAllStepsCompleted());

        if (allFinished)
        {
            Debug.Log("所有玩家都完成了教學！準備進入遊戲...");

            gameManager.BroadcastNavigateToPlaying();
            // 呼叫 GameManager 載入真正的遊戲場景
            // (你也可以在這裡加一個延遲，讓玩家看到「全部完成」的訊息)
            gameManager.StartCoroutine(gameManager.LoadGameSceneAndStart("Toybox"));
        }
    }

    // Video
    void PlayStepVideo(string step)
    {
        if (videoPlayer == null) return;

        VideoClip clipToPlay = null;
        
        switch (step)
        {
            case "calibrate":
                clipToPlay = calibrateVideo;
                break;
            case "forward":
                clipToPlay = forwardVideo;
                break;
            case "left":
                clipToPlay = leftVideo;
                break;
            case "right":
                clipToPlay = rightVideo;
                break;
            case "backward":
                clipToPlay = backwardVideo;
                break;
        }

        if (clipToPlay != null)
        {
            videoPlayer.clip = clipToPlay;
            videoPlayer.Prepare(); // 準備影片
        }
    }

    // 影片準備完成後自動播放
    void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log("影片準備完成，開始播放");
        
        // 將影片 texture 指定給 RawImage
        if (videoRawImage != null)
        {
            videoRawImage.texture = vp.texture;
        }
        
        vp.Play();
    }

    // 影片播放完畢
    void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log("影片播放完畢");
        // 這裡之後可以加入「縮小 DemoArea」的邏輯
    }
}
