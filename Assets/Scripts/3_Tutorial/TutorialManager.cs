using UnityEngine;
using TMPro;
using System.Collections.Generic;
using SimpleWebRTC;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

[System.Serializable]
public class StepAudioMapping
{
    public string stepName; // forward, left, right, backward, complete
    public AudioClip soundEffect;
}

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

    [Header("Audio")]
    [Tooltip("在更換步驟提示時播放的音效")]
    public List<StepAudioMapping> stepAudioMap;

    [Tooltip("播放音效用的 AudioSource")]
    public AudioSource audioSource;

    [Header("SceneFadeInFadeOut")]
    [SerializeField] private TutorialSceneFadeOut tutorialSceneFadeOut;


    // --- 私有變數 ---
    private NetworkManager networkManager;
    private GameManager gameManager;
    // 儲存 peerId 和教學進度的綁定
    private Dictionary<string, PlayerTutorialProgress> playerProgressMap = new Dictionary<string, PlayerTutorialProgress>();
    // 儲存 peerId 和 UI 卡片的綁定
    private Dictionary<string, PlayerCardUI> playerCardUIMap = new Dictionary<string, PlayerCardUI>();

    [Tooltip("追蹤目前的教學階段")]
    private string currentTutorialPhase = "calibrate";

    [Tooltip("用來防止重複觸發延遲協程")]
    private bool isAdvancing = false;

    // 非陀螺儀關卡的簡化教學模式
    private bool isSimplifiedTutorial = false;

    void Start()
    {
        networkManager = FindFirstObjectByType<NetworkManager>();
        gameManager = FindFirstObjectByType<GameManager>();
        if (networkManager == null)
        {
            Debug.LogError("TutorialManager 找不到 networkManager！");
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogWarning("TutorialManager 找不到 AudioSource，將自動添加一個。");
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        WebRTCManager.OnDataMessageReceived_Static += OnDataReceived;

        if (videoPlayer != null)
        {
            videoPlayer.targetTexture = null; // 使用 API Only 模式
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.loopPointReached += OnVideoFinished;
        }

        InitializePlayerProgress();

        // 非陀螺儀關卡走簡化教學
        if (!IsGyroLevel())
        {
            StartCoroutine(RunSimplifiedTutorial());
        }
    }

    /// <summary>
    /// 判斷目前選擇的關卡是否為陀螺儀控制
    /// </summary>
    private bool IsGyroLevel()
    {
        string level = gameManager?.selectedLevel ?? "4_ColorPaper";
        return level == "4_ColorPaper" || level == "4_Toybox";
    }

    /// <summary>
    /// 非陀螺儀關卡的簡化教學流程（如 TapEat 點擊關卡）
    /// </summary>
    private IEnumerator RunSimplifiedTutorial()
    {
        isSimplifiedTutorial = true;

        // 更新大螢幕 UI
        instructionText.text = "請在手機上練習點擊";
        if (demoImage != null) demoImage.gameObject.SetActive(false);

        // 廣播教學步驟，觸發 Web 端顯示教學
        BroadcastTutorialStep("calibrate", "請在手機上練習點擊");

        // 等待所有玩家送回 tutorial_step_complete: calibrate
        yield return new WaitUntil(() =>
            playerProgressMap.Values.All(p => p.completedCalibration));

        // 播放完成音效
        if (stepAudioMap != null && audioSource != null)
        {
            StepAudioMapping mapping = stepAudioMap.FirstOrDefault(m => m.stepName == "complete");
            if (mapping != null && mapping.soundEffect != null)
            {
                audioSource.PlayOneShot(mapping.soundEffect);
            }
        }

        yield return new WaitForSeconds(1.0f);

        // 完成
        instructionText.text = "準備開始遊戲！";

        yield return new WaitForSeconds(1.0f);

        Debug.Log("[TutorialManager] 非陀螺儀關卡教學完成，準備進入遊戲...");
        networkManager.BroadcastNavigateToPlaying();
        gameManager.UpdatePlayerInfo(networkManager.playersInfo);

        string selectedLevel = gameManager.selectedLevel;
        Debug.Log($"[TutorialManager] 載入選擇的關卡: {selectedLevel}");
        tutorialSceneFadeOut.SetNextScene(selectedLevel);
        tutorialSceneFadeOut.LoadNextSceneWithFadeOut();
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

        foreach (Player p in networkManager.playersInfo)
        {
            // 找到這個 player 對應的 peerId
            string peerId = networkManager.peerIdToPlayer.FirstOrDefault(x => x.Value == p).Key;
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
                cardUI.SetupCard(progress);
                cardUI.SetStepStatus(false);
            }
        }

        // 第一次更新主要提示文字
        UpdateInstructionText();
    }

    private void OnDataReceived(string message, string senderPeerId)
    {
        // 檢查這個訊息是否來自我們正在追蹤的玩家
        if (!playerProgressMap.ContainsKey(senderPeerId)) return;

        if (isAdvancing) return;

        try
        {
            BaseMessage data = JsonUtility.FromJson<BaseMessage>(message);
            PlayerTutorialProgress progress = playerProgressMap[senderPeerId];
            bool progressMade = false;

            if (data.type == "move")
            {
                MoveMessage msg = JsonUtility.FromJson<MoveMessage>(message);

                // 如果已經完成，就不再檢查
                if (progress.IsAllStepsCompleted()) return;

                if (currentTutorialPhase == "calibrate" && !progress.completedCalibration)
                {
                    progress.completedCalibration = true;
                    Debug.Log($"{progress.playerInfo.name} 已完成校正！");
                    playerCardUIMap[senderPeerId].SetStepStatus(true);
                    progressMade = true;
                }
                else if (currentTutorialPhase == "forward" && !progress.completedForward && msg.vector.y < -tiltThreshold)
                {
                    progress.completedForward = true;
                    Debug.Log($"{progress.playerInfo.name} 完成了向前傾斜！");
                    playerCardUIMap[senderPeerId].SetStepStatus(true);
                    progressMade = true;
                }
                else if (currentTutorialPhase == "left" && !progress.completedLeft && msg.vector.x < -tiltThreshold)
                {
                    progress.completedLeft = true;
                    Debug.Log($"{progress.playerInfo.name} 完成了向左傾斜！");
                    playerCardUIMap[senderPeerId].SetStepStatus(true);
                    progressMade = true;
                }
                else if (currentTutorialPhase == "right" && !progress.completedRight && msg.vector.x > tiltThreshold)
                {
                    progress.completedRight = true;
                    Debug.Log($"{progress.playerInfo.name} 完成了向右傾斜！");
                    playerCardUIMap[senderPeerId].SetStepStatus(true);
                    progressMade = true;
                }
                else if (currentTutorialPhase == "backward" && !progress.completedBackward && msg.vector.y > tiltThreshold)
                {
                    progress.completedBackward = true;
                    Debug.Log($"{progress.playerInfo.name} 完成了向後傾斜！");
                    playerCardUIMap[senderPeerId].SetStepStatus(true);
                    progressMade = true;
                }

                if (progressMade)
                {
                    CheckForStepAdvancement();
                }
            }
            else if (data.type == "tutorial_step_complete")
            {
                // 手機端明確告知完成某步驟
                TutorialStepMessage msg = JsonUtility.FromJson<TutorialStepMessage>(message);

                if (msg.step == "calibrate" && currentTutorialPhase == "calibrate" && !progress.completedCalibration)
                {
                    progress.completedCalibration = true;
                    Debug.Log($"{progress.playerInfo.name} 完成了 (手機訊號) 校正！");
                    playerCardUIMap[senderPeerId].SetStepStatus(true);
                    progressMade = true;
                }
                else if (msg.step == "forward" && currentTutorialPhase == "forward" && !progress.completedForward)
                {
                    progress.completedForward = true;
                    Debug.Log($"{progress.playerInfo.name} 完成了 (手機訊號) 向前傾斜！");
                    playerCardUIMap[senderPeerId].SetStepStatus(true);
                    progressMade = true;
                }
                else if (msg.step == "left" && currentTutorialPhase == "left" && !progress.completedLeft)
                {
                    progress.completedLeft = true;
                    Debug.Log($"{progress.playerInfo.name} 完成了 (手機訊號) 向左傾斜！");
                    playerCardUIMap[senderPeerId].SetStepStatus(true);
                    progressMade = true;
                }
                else if (msg.step == "right" && currentTutorialPhase == "right" && !progress.completedRight)
                {
                    progress.completedRight = true;
                    Debug.Log($"{progress.playerInfo.name} 完成了 (手機訊號) 向右傾斜！");
                    playerCardUIMap[senderPeerId].SetStepStatus(true);
                    progressMade = true;
                }
                else if (msg.step == "backward" && currentTutorialPhase == "backward" && !progress.completedBackward)
                {
                    // (你原本的 code 裡沒有 backward，我猜也需要，所以加上了)
                    progress.completedBackward = true;
                    Debug.Log($"{progress.playerInfo.name} 完成了 (手機訊號) 向後傾斜！");
                    playerCardUIMap[senderPeerId].SetStepStatus(true);
                    progressMade = true;
                }
                if (progressMade)
                {
                    CheckForStepAdvancement();
                }
            }
        }
        catch (System.Exception) { /* 忽略格式不符的訊息 */ }
    }

    // 更新主要的教學提示文字
    void UpdateInstructionText()
    {
        string step = currentTutorialPhase; // 直接使用目前的階段狀態
        string text = "";
        int animatorIndex = -1;

        switch (step)
        {
            case "calibrate":
                text = "請將手機置於平面，按下校正按鈕！";
                animatorIndex = -1;
                break;
            case "forward":
                text = "很棒！向前傾斜手機";
                animatorIndex = 0;
                break;
            case "left":
                text = "很好！向左傾斜手機";
                animatorIndex = 1;
                break;
            case "right":
                text = "做得好！向右傾斜手機";
                animatorIndex = 2;
                break;
            case "backward":
                text = "最後一步！向後傾斜手機";
                animatorIndex = 3;
                break;
            case "complete":
                text = "太棒了！所有人都完成了訓練！";
                animatorIndex = -1;
                break;
        }

        instructionText.text = text;
        
        if (demoImage != null && stepDemoImages != null && animatorIndex >= 0 && animatorIndex < stepDemoImages.Length)
        {
            demoImage.sprite = stepDemoImages[animatorIndex];
            demoImage.gameObject.SetActive(true);
        }
        else if (demoImage != null)
        {
            // demoImage.gameObject.SetActive(false); // 或者隱藏
        }

        // 更新 Animator
        if (demoAnimator != null)
        {
            demoAnimator.SetInteger("StepIndex", animatorIndex);
        }

        PlayStepVideo(step); // 播放對應影片
        BroadcastTutorialStep(step, text); // 廣播給手機
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
        networkManager.webRTCConnection.SendDataChannelMessage(jsonMessage);
        Debug.Log($"Broadcast tutorial step: {step}");
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
    }
    
    void CheckForStepAdvancement()
    {
        if (isAdvancing || isSimplifiedTutorial) return;

        if (currentTutorialPhase == "calibrate" && playerProgressMap.Values.All(p => p.completedCalibration))
        {
            StartCoroutine(AdvanceToNextStep("forward"));
        }
        else if (currentTutorialPhase == "forward" && playerProgressMap.Values.All(p => p.completedForward))
        {
            StartCoroutine(AdvanceToNextStep("left"));
        }
        else if (currentTutorialPhase == "left" && playerProgressMap.Values.All(p => p.completedLeft))
        {
            StartCoroutine(AdvanceToNextStep("right"));
        }
        else if (currentTutorialPhase == "right" && playerProgressMap.Values.All(p => p.completedRight))
        {
            StartCoroutine(AdvanceToNextStep("backward"));
        }
        else if (currentTutorialPhase == "backward" && playerProgressMap.Values.All(p => p.completedBackward))
        {
            StartCoroutine(AdvanceToNextStep("complete"));
        }
    }

    System.Collections.IEnumerator AdvanceToNextStep(string nextPhase)
    {
        isAdvancing = true;

        AudioClip soundToPlay = null;
        if (stepAudioMap != null && audioSource != null)
        {
            StepAudioMapping mapping = stepAudioMap.FirstOrDefault(m => m.stepName == nextPhase);
            if (mapping != null)
            {
                soundToPlay = mapping.soundEffect;
            }
        }

        if (soundToPlay != null)
        {
            audioSource.PlayOneShot(soundToPlay);
        }

        yield return new WaitForSeconds(1.0f);

        currentTutorialPhase = nextPhase;

        UpdateInstructionText();

        if (nextPhase == "complete")
        {
            Debug.Log("所有玩家都完成了教學！準備進入遊戲...");
            networkManager.BroadcastNavigateToPlaying();
            gameManager.UpdatePlayerInfo(networkManager.playersInfo);

            // 根據選擇的關卡載入對應場景
            string selectedLevel = gameManager.selectedLevel;
            Debug.Log($"[TutorialManager] 載入選擇的關卡: {selectedLevel}");
            tutorialSceneFadeOut.SetNextScene(selectedLevel);
            tutorialSceneFadeOut.LoadNextSceneWithFadeOut();
        }
        else
        {
            
            foreach (PlayerCardUI card in playerCardUIMap.Values)
            {
                card.SetStepStatus(false);
            }
        }

        isAdvancing = false;
    }
}
