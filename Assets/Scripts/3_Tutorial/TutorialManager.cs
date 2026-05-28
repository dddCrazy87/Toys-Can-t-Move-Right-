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
    public string stepName;
    public AudioClip soundEffect;
}

[System.Serializable]
public class TutorialStepMessage : BaseMessage
{
    public string step;
}

// 擴充原本的 Instruction Message，加入 slideIndex
[System.Serializable]
public class TutorialInstructionMessage : BaseMessage
{
    public string step;
    public string message;
    public int slideIndex;
}

// 泛用化的幻燈片類別
[System.Serializable]
public class TutorialSlide
{
    public Sprite slideImage;
    [TextArea(2, 5)]
    public string instructionText;
}

// 支援的校正模式
public enum CalibrationType { None, Gyro, TapSwipe, Timer }

// 每一關專屬的教學設定
[System.Serializable]
public class LevelTutorialConfig
{
    public string levelName;
    public List<TutorialSlide> slides = new List<TutorialSlide>();
    public CalibrationType calibrationType = CalibrationType.Gyro;
}

public class PlayerTutorialProgress
{
    public Player playerInfo;
    public bool completedCalibration = false;
    public bool completedForward = false;
    public bool completedLeft = false;
    public bool completedRight = false;
    public bool completedBackward = false;

    public bool IsAllStepsCompleted()
    {
        return completedForward && completedLeft && completedRight && completedBackward && completedCalibration;
    }
}

public class TutorialManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI instructionText;
    public Transform playerProgressContainer;
    public Animator demoAnimator;
    public GameObject playerProgressCardPrefab;
    public Image demoImage;

    [Tooltip("用於顯示 Slide 的 UI Image (原 SpySlideImage)")]
    public Image slideImageDisplay;

    [Header("Video Player Settings")]
    public VideoPlayer videoPlayer;
    public RawImage videoRawImage;
    public VideoClip calibrateVideo;
    public VideoClip forwardVideo;
    public VideoClip leftVideo;
    public VideoClip rightVideo;
    public VideoClip backwardVideo;
    public VideoClip tapTutorialVideo;
    public VideoClip swipeTutorialVideo;

    [Header("Tutorial Configs Per Level")]
    public List<LevelTutorialConfig> levelConfigs = new List<LevelTutorialConfig>();

    [Header("Tutorial Settings")]
    [Range(0.1f, 1.0f)]
    public float tiltThreshold = 0.7f;
    public Sprite[] stepDemoImages;

    [Header("Audio")]
    public List<StepAudioMapping> stepAudioMap;
    public AudioSource audioSource;

    [Header("SceneFadeInFadeOut")]
    [SerializeField] private TutorialSceneFadeOut tutorialSceneFadeOut;

    private HashSet<string> slideNextReadyPeers = new HashSet<string>();

    // --- 私有變數 ---
    private NetworkManager networkManager;
    private GameManager gameManager;
    private Dictionary<string, PlayerTutorialProgress> playerProgressMap = new Dictionary<string, PlayerTutorialProgress>();
    private Dictionary<string, PlayerCardUI> playerCardUIMap = new Dictionary<string, PlayerCardUI>();

    private string currentTutorialPhase = "init";
    private bool isAdvancing = false;
    private bool isSimplifiedTutorial = false;

    void Start()
    {
        networkManager = FindFirstObjectByType<NetworkManager>();
        gameManager = FindFirstObjectByType<GameManager>();
        if (networkManager == null) return;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        WebRTCManager.OnDataMessageReceived_Static += OnDataReceived;
        WebRTCManager.OnPeerDisconnected_Static += OnPeerDisconnectedInTutorial;

        if (videoPlayer != null)
        {
            videoPlayer.targetTexture = null;
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.loopPointReached += OnVideoFinished;
        }

        InitializePlayerProgress();

        // 統一由協程控制教學流程
        StartCoroutine(RunTutorialSequence());
    }

    private bool IsGyroLevel()
    {
        string level = gameManager?.selectedLevel ?? "4_ColorPaper";
        return level == "4_ColorPaper" || level == "4_Toybox";
    }

    private IEnumerator RunTutorialSequence()
    {
        string selectedLevel = gameManager?.selectedLevel ?? "4_ColorPaper";
        LevelTutorialConfig config = levelConfigs.FirstOrDefault(c => c.levelName == selectedLevel);

        // --- 階段 1：Slide 簡報教學 ---
        bool hasSlides = config != null && config.slides != null && config.slides.Count > 0;
        if (hasSlides)
        {
            currentTutorialPhase = "slide";
            instructionText.text = "";

            if (slideImageDisplay != null) slideImageDisplay.gameObject.SetActive(true);
            if (videoRawImage != null && videoRawImage.transform.parent != null)
                videoRawImage.transform.parent.gameObject.SetActive(false);

            for (int i = 0; i < config.slides.Count; i++)
            {
                TutorialSlide slide = config.slides[i];

                if (slideImageDisplay != null)
                    slideImageDisplay.sprite = slide.slideImage; // 自動替換或清空

                // 重置點名簿與卡片狀態
                foreach (PlayerCardUI card in playerCardUIMap.Values) card.SetStepStatus(false);
                slideNextReadyPeers.Clear();

                // 加入不斷廣播機制，確保沒人漏接
                while (slideNextReadyPeers.Count < networkManager.playersInfo.Count)
                {
                    BroadcastSlideState(i, slide.instructionText);
                    // 每秒發送一次當前狀態，直到所有人都準備好
                    yield return new WaitForSeconds(1.0f);
                }

                if (i < config.slides.Count - 1) PlayStepSound("forward");
                yield return new WaitForSeconds(0.5f); // 停頓讓大家看到全員打勾
            }

            if (slideImageDisplay != null) slideImageDisplay.gameObject.SetActive(false);
            if (videoRawImage != null && videoRawImage.transform.parent != null)
                videoRawImage.transform.parent.gameObject.SetActive(true);
        }

        foreach (PlayerCardUI card in playerCardUIMap.Values) card.SetStepStatus(false);

        // --- 階段 2：操作校正教學 ---
        CalibrationType calType = config != null ? config.calibrationType : (IsGyroLevel() ? CalibrationType.Gyro : CalibrationType.Timer);

        if (calType == CalibrationType.Gyro)
        {
            currentTutorialPhase = "calibrate";
            UpdateInstructionText();
            // 持續廣播 calibrate 直到至少一個玩家完成校正，確保所有人都收到指令
            while (!playerProgressMap.Values.Any(p => p.completedCalibration))
            {
                BroadcastTutorialStep("calibrate", "請將手機拿直向，按下校正按鈕！");
                yield return new WaitForSeconds(2f);
            }
            // 之後由 OnDataReceived 接手後續步驟推進
            yield break;
        }
        else if (calType == CalibrationType.TapSwipe)
        {
            isSimplifiedTutorial = true;
            if (demoImage != null) demoImage.gameObject.SetActive(false);
            if (demoAnimator != null) demoAnimator.gameObject.SetActive(false);
            if (videoRawImage != null) videoRawImage.gameObject.SetActive(true);
            if (videoPlayer != null) { videoPlayer.gameObject.SetActive(true); videoPlayer.isLooping = true; }

            currentTutorialPhase = "right";
            instructionText.text = "請在手機上練習點擊";
            if (tapTutorialVideo != null) PlayStepVideo("tap");
            // 持續廣播直到所有人完成點擊練習
            while (!playerProgressMap.Values.All(p => p.completedRight))
            {
                BroadcastTutorialStep("right", "請在手機上練習點擊");
                yield return new WaitForSeconds(2f);
            }

            PlayStepSound("forward");
            foreach (PlayerCardUI card in playerCardUIMap.Values) card.SetStepStatus(false);
            yield return new WaitForSeconds(0.5f);

            currentTutorialPhase = "backward";
            instructionText.text = "請在手機上練習滑動丟棄";
            if (swipeTutorialVideo != null) PlayStepVideo("swipe");
            // 持續廣播直到所有人完成滑動練習
            while (!playerProgressMap.Values.All(p => p.completedBackward))
            {
                BroadcastTutorialStep("backward", "請在手機上練習滑動丟棄");
                yield return new WaitForSeconds(2f);
            }
        }
        else if (calType == CalibrationType.Timer)
        {
            isSimplifiedTutorial = true;
            currentTutorialPhase = "calibrate";
            // 持續廣播直到所有人完成
            while (!playerProgressMap.Values.All(p => p.completedCalibration))
            {
                BroadcastTutorialStep("timer", "請看大螢幕指示");
                yield return new WaitForSeconds(2f);
            }
        }

        // --- 階段 3：完成與轉場 ---
        PlayStepSound("complete");
        currentTutorialPhase = "complete";
        instructionText.text = "準備開始遊戲！";
        BroadcastTutorialStep("complete", "準備開始遊戲！");
        yield return new WaitForSeconds(1.0f);

        networkManager.BroadcastNavigateToPlaying();
        gameManager.UpdatePlayerInfo(networkManager.playersInfo);
        tutorialSceneFadeOut.SetNextScene(selectedLevel);
        tutorialSceneFadeOut.LoadNextSceneWithFadeOut();
    }

    private void OnPeerDisconnectedInTutorial(string peerId)
    {
        if (!playerProgressMap.ContainsKey(peerId)) return;

        Debug.Log($"[TutorialManager] 玩家 {peerId} 在教學中斷線，自動完成所有步驟");
        PlayerTutorialProgress progress = playerProgressMap[peerId];
        progress.completedCalibration = true;
        progress.completedForward = true;
        progress.completedLeft = true;
        progress.completedRight = true;
        progress.completedBackward = true;

        if (playerCardUIMap.ContainsKey(peerId)) playerCardUIMap[peerId].SetStepStatus(true);
        if (!slideNextReadyPeers.Contains(peerId)) slideNextReadyPeers.Add(peerId); // 防呆：斷線視為已點擊 Slide
    }

    void OnDestroy()
    {
        WebRTCManager.OnPeerDisconnected_Static -= OnPeerDisconnectedInTutorial;
        WebRTCManager.OnDataMessageReceived_Static -= OnDataReceived;
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }

    void InitializePlayerProgress()
    {
        foreach (Transform child in playerProgressContainer) Destroy(child.gameObject);

        foreach (Player p in networkManager.playersInfo)
        {
            string peerId = networkManager.peerIdToPlayer.FirstOrDefault(x => x.Value == p).Key;
            if (peerId != null)
            {
                PlayerTutorialProgress progress = new PlayerTutorialProgress { playerInfo = p };
                playerProgressMap[peerId] = progress;

                GameObject cardGO = Instantiate(playerProgressCardPrefab, playerProgressContainer);
                PlayerCardUI cardUI = cardGO.GetComponent<PlayerCardUI>();
                playerCardUIMap[peerId] = cardUI;
                cardUI.SetupCard(progress);
                cardUI.SetStepStatus(false);
            }
        }
        if (videoPlayer != null) videoPlayer.Stop();
    }

    private void OnDataReceived(string message, string senderPeerId)
    {
        try
        {
            BaseMessage baseData = JsonUtility.FromJson<BaseMessage>(message);

            // 處理 Slide 換頁訊號
            if (baseData.type == "tutorial_slide_next")
            {
                if (!slideNextReadyPeers.Contains(senderPeerId))
                {
                    slideNextReadyPeers.Add(senderPeerId);
                    Debug.Log($"[TutorialManager] 玩家 {senderPeerId} 準備好切換下一頁 ({slideNextReadyPeers.Count}/{networkManager.playersInfo.Count})");

                    if (playerCardUIMap.ContainsKey(senderPeerId))
                        playerCardUIMap[senderPeerId].SetStepStatus(true);
                }
                return;
            }
        }
        catch (System.Exception) { }

        if (!playerProgressMap.ContainsKey(senderPeerId) || isAdvancing) return;

        try
        {
            BaseMessage data = JsonUtility.FromJson<BaseMessage>(message);
            PlayerTutorialProgress progress = playerProgressMap[senderPeerId];
            bool progressMade = false;

            if (data.type == "move")
            {
                MoveMessage msg = JsonUtility.FromJson<MoveMessage>(message);
                if (progress.IsAllStepsCompleted()) return;

                if (currentTutorialPhase == "calibrate" && !progress.completedCalibration) { progress.completedCalibration = true; playerCardUIMap[senderPeerId].SetStepStatus(true); progressMade = true; }
                else if (currentTutorialPhase == "forward" && !progress.completedForward && msg.vector.y < -tiltThreshold) { progress.completedForward = true; playerCardUIMap[senderPeerId].SetStepStatus(true); progressMade = true; }
                else if (currentTutorialPhase == "left" && !progress.completedLeft && msg.vector.x < -tiltThreshold) { progress.completedLeft = true; playerCardUIMap[senderPeerId].SetStepStatus(true); progressMade = true; }
                else if (currentTutorialPhase == "right" && !progress.completedRight && msg.vector.x > tiltThreshold) { progress.completedRight = true; playerCardUIMap[senderPeerId].SetStepStatus(true); progressMade = true; }
                else if (currentTutorialPhase == "backward" && !progress.completedBackward && msg.vector.y > tiltThreshold) { progress.completedBackward = true; playerCardUIMap[senderPeerId].SetStepStatus(true); progressMade = true; }
                if (progressMade) CheckForStepAdvancement();
            }
            else if (data.type == "tutorial_step_complete")
            {
                TutorialStepMessage msg = JsonUtility.FromJson<TutorialStepMessage>(message);
                if (msg.step == "calibrate" && (currentTutorialPhase == "calibrate" || currentTutorialPhase == "timer") && !progress.completedCalibration) { progress.completedCalibration = true; playerCardUIMap[senderPeerId].SetStepStatus(true); progressMade = true; }
                else if (msg.step == "forward" && currentTutorialPhase == "forward" && !progress.completedForward) { progress.completedForward = true; playerCardUIMap[senderPeerId].SetStepStatus(true); progressMade = true; }
                else if (msg.step == "left" && currentTutorialPhase == "left" && !progress.completedLeft) { progress.completedLeft = true; playerCardUIMap[senderPeerId].SetStepStatus(true); progressMade = true; }
                else if (msg.step == "right" && currentTutorialPhase == "right" && !progress.completedRight) { progress.completedRight = true; playerCardUIMap[senderPeerId].SetStepStatus(true); progressMade = true; }
                else if (msg.step == "backward" && currentTutorialPhase == "backward" && !progress.completedBackward) { progress.completedBackward = true; playerCardUIMap[senderPeerId].SetStepStatus(true); progressMade = true; }
                if (progressMade) CheckForStepAdvancement();
            }
        }
        catch (System.Exception) { }
    }

    void UpdateInstructionText()
    {
        string step = currentTutorialPhase;
        string text = "";
        int animatorIndex = -1;

        switch (step)
        {
            case "calibrate": text = "請將手機拿直向，按下校正按鈕！"; animatorIndex = -1; break;
            case "forward": text = "很棒！向前傾斜手機"; animatorIndex = 0; break;
            case "left": text = "很好！向左傾斜手機"; animatorIndex = 1; break;
            case "right": text = "做得好！向右傾斜手機"; animatorIndex = 2; break;
            case "backward": text = "最後一步！向後傾斜手機"; animatorIndex = 3; break;
            case "complete": text = "太棒了！所有人都完成了訓練！"; animatorIndex = -1; break;
        }

        instructionText.text = text;

        if (demoImage != null && stepDemoImages != null && animatorIndex >= 0 && animatorIndex < stepDemoImages.Length)
        {
            demoImage.sprite = stepDemoImages[animatorIndex];
            demoImage.gameObject.SetActive(true);
        }

        if (demoAnimator != null) demoAnimator.SetInteger("StepIndex", animatorIndex);
        PlayStepVideo(step);
        BroadcastTutorialStep(step, text);
    }

    void BroadcastSlideState(int index, string message)
    {
        TutorialInstructionMessage tutorialMsg = new TutorialInstructionMessage
        {
            type = "tutorial_instruction",
            step = "slide",
            message = message,
            slideIndex = index
        };
        networkManager.webRTCConnection.SendDataChannelMessage(JsonUtility.ToJson(tutorialMsg));
    }

    void BroadcastTutorialStep(string step, string message)
    {
        TutorialInstructionMessage tutorialMsg = new TutorialInstructionMessage { type = "tutorial_instruction", step = step, message = message };
        string json = JsonUtility.ToJson(tutorialMsg);
        // 重試 5 次確保所有玩家收到
        StartCoroutine(BroadcastTutorialStepWithRetry(json, 5, 0.5f));
    }

    private IEnumerator BroadcastTutorialStepWithRetry(string json, int retryCount, float interval)
    {
        for (int i = 0; i < retryCount; i++)
        {
            networkManager.webRTCConnection.SendDataChannelMessage(json);
            if (i < retryCount - 1) yield return new WaitForSeconds(interval);
        }
    }

    void PlayStepVideo(string step)
    {
        if (videoPlayer == null) return;
        VideoClip clipToPlay = null;
        switch (step)
        {
            case "calibrate": clipToPlay = calibrateVideo; break;
            case "forward": clipToPlay = forwardVideo; break;
            case "left": clipToPlay = leftVideo; break;
            case "right": clipToPlay = rightVideo; break;
            case "backward": clipToPlay = backwardVideo; break;
            case "tap": clipToPlay = tapTutorialVideo; break;
            case "swipe": clipToPlay = swipeTutorialVideo; break;
        }
        if (clipToPlay != null)
        {
            videoPlayer.clip = clipToPlay;
            videoPlayer.Prepare();
        }
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        if (videoRawImage != null) videoRawImage.texture = vp.texture;
        vp.Play();
    }

    void OnVideoFinished(VideoPlayer vp) { }

    private void PlayStepSound(string stepName)
    {
        if (stepAudioMap == null || audioSource == null) return;
        StepAudioMapping mapping = stepAudioMap.FirstOrDefault(m => m.stepName == stepName);
        if (mapping != null && mapping.soundEffect != null) audioSource.PlayOneShot(mapping.soundEffect);
    }

    void CheckForStepAdvancement()
    {
        if (isAdvancing || isSimplifiedTutorial) return;
        if (currentTutorialPhase == "calibrate" && playerProgressMap.Values.All(p => p.completedCalibration)) StartCoroutine(AdvanceToNextStep("forward"));
        else if (currentTutorialPhase == "forward" && playerProgressMap.Values.All(p => p.completedForward)) StartCoroutine(AdvanceToNextStep("left"));
        else if (currentTutorialPhase == "left" && playerProgressMap.Values.All(p => p.completedLeft)) StartCoroutine(AdvanceToNextStep("right"));
        else if (currentTutorialPhase == "right" && playerProgressMap.Values.All(p => p.completedRight)) StartCoroutine(AdvanceToNextStep("backward"));
        else if (currentTutorialPhase == "backward" && playerProgressMap.Values.All(p => p.completedBackward)) StartCoroutine(AdvanceToNextStep("complete"));
    }

    System.Collections.IEnumerator AdvanceToNextStep(string nextPhase)
    {
        isAdvancing = true;
        PlayStepSound(nextPhase);
        yield return new WaitForSeconds(1.0f);
        currentTutorialPhase = nextPhase;
        UpdateInstructionText();

        if (nextPhase == "complete")
        {
            networkManager.BroadcastNavigateToPlaying();
            gameManager.UpdatePlayerInfo(networkManager.playersInfo);
            tutorialSceneFadeOut.SetNextScene(gameManager.selectedLevel);
            tutorialSceneFadeOut.LoadNextSceneWithFadeOut();
        }
        else foreach (PlayerCardUI card in playerCardUIMap.Values) card.SetStepStatus(false);

        isAdvancing = false;
    }
}

