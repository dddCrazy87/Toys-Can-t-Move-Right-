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

public enum TutorialStep { Calibrate, TiltLeft, TiltRight, MiniGame, Finished }

[System.Serializable]
public class TutorialStepMessage : BaseMessage
{
    public string step;
}

[System.Serializable]
public class TutorialInstructionMessage : BaseMessage
{
    public string step;
    public string message;
}

[System.Serializable]
public class SpyTutorialSlide
{
    public Sprite slideImage;
    [TextArea(2, 5)]
    public string instructionText;
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

    [Header("Tutorial Settings")]
    [Range(0.1f, 1.0f)]
    public float tiltThreshold = 0.7f;
    public Sprite[] stepDemoImages;

    [Header("Audio")]
    public List<StepAudioMapping> stepAudioMap;
    public AudioSource audioSource;

    [Header("SceneFadeInFadeOut")]
    [SerializeField] private TutorialSceneFadeOut tutorialSceneFadeOut;

    [Header("Spy Game Tutorial Settings")]
    public List<SpyTutorialSlide> spyTutorialSlides = new List<SpyTutorialSlide>();

    [Tooltip("請把剛剛在 Canvas 底下新建的 SpySlideImage 拖進來")]
    public Image spySlideImage;

    private int currentSpySlideIndex = 0;

    // ▼ 新增：用來記錄「哪些玩家已經點過下一頁」的點名簿 ▼
    private HashSet<string> spyNextReadyPeers = new HashSet<string>();

    // --- 私有變數 ---
    private NetworkManager networkManager;
    private GameManager gameManager;
    private Dictionary<string, PlayerTutorialProgress> playerProgressMap = new Dictionary<string, PlayerTutorialProgress>();
    private Dictionary<string, PlayerCardUI> playerCardUIMap = new Dictionary<string, PlayerCardUI>();

    private string currentTutorialPhase = "calibrate";
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

        if (!IsGyroLevel())
        {
            StartCoroutine(RunSimplifiedTutorial());
        }
    }

    private bool IsGyroLevel()
    {
        string level = gameManager?.selectedLevel ?? "4_ColorPaper";
        return level == "4_ColorPaper" || level == "4_Toybox";
    }

    private IEnumerator RunSimplifiedTutorial()
    {
        isSimplifiedTutorial = true;
        string selectedLevel = gameManager.selectedLevel;

        if (selectedLevel == "4_SpyGame")
        {
            yield return StartCoroutine(RunSpyTutorial());
            yield break;
        }

        if (demoImage != null) demoImage.gameObject.SetActive(false);
        if (demoAnimator != null) demoAnimator.gameObject.SetActive(false);
        if (videoRawImage != null) videoRawImage.gameObject.SetActive(true);
        if (videoPlayer != null)
        {
            videoPlayer.gameObject.SetActive(true);
            videoPlayer.isLooping = true;
        }

        currentTutorialPhase = "right";
        instructionText.text = "請在手機上練習點擊";
        if (tapTutorialVideo != null) PlayStepVideo("tap");
        BroadcastTutorialStep("right", "請在手機上練習點擊");

        yield return new WaitUntil(() => playerProgressMap.Values.All(p => p.completedRight));

        PlayStepSound("forward");
        foreach (PlayerCardUI card in playerCardUIMap.Values) card.SetStepStatus(false);
        yield return new WaitForSeconds(0.5f);

        currentTutorialPhase = "backward";
        instructionText.text = "請在手機上練習滑動丟棄";
        if (swipeTutorialVideo != null) PlayStepVideo("swipe");
        BroadcastTutorialStep("backward", "請在手機上練習滑動丟棄");

        yield return new WaitUntil(() => playerProgressMap.Values.All(p => p.completedBackward));

        PlayStepSound("complete");
        yield return new WaitForSeconds(1.0f);
        instructionText.text = "準備開始遊戲！";
        yield return new WaitForSeconds(1.0f);

        networkManager.BroadcastNavigateToPlaying();
        gameManager.UpdatePlayerInfo(networkManager.playersInfo);
        tutorialSceneFadeOut.SetNextScene(selectedLevel);
        tutorialSceneFadeOut.LoadNextSceneWithFadeOut();
    }

    private IEnumerator RunSpyTutorial()
    {
        currentSpySlideIndex = 0;

        // 隱藏不相干的背景影片容器
        if (videoRawImage != null && videoRawImage.transform.parent != null)
        {
            videoRawImage.transform.parent.gameObject.SetActive(false);
        }

        if (spyTutorialSlides == null || spyTutorialSlides.Count == 0)
        {
            Debug.LogWarning("[TutorialManager] 投影片數量為 0，將略過抓內鬼教學。");
        }
        else
        {
            if (spySlideImage != null) spySlideImage.gameObject.SetActive(true);

            while (currentSpySlideIndex < spyTutorialSlides.Count)
            {
                SpyTutorialSlide currentSlide = spyTutorialSlides[currentSpySlideIndex];
                instructionText.text = currentSlide.instructionText;

                if (spySlideImage != null && currentSlide.slideImage != null)
                {
                    spySlideImage.sprite = currentSlide.slideImage;
                }

                // 換頁時，所有玩家的卡片重置為「未完成」
                foreach (PlayerCardUI card in playerCardUIMap.Values)
                {
                    card.SetStepStatus(false);
                }

                spyNextReadyPeers.Clear();

                // 等待所有玩家點擊
                yield return new WaitUntil(() => spyNextReadyPeers.Count >= networkManager.playersInfo.Count);

                // ▼ 新增：除了最後一頁以外，大家點完換頁時播放過場提示音 (比照其他關卡) ▼
                if (currentSpySlideIndex < spyTutorialSlides.Count - 1)
                {
                    PlayStepSound("forward");
                }

                // 停頓 0.5 秒讓大家看到全員打勾，再切換下一頁
                yield return new WaitForSeconds(0.5f);
                currentSpySlideIndex++;
            }
        }


        PlayStepSound("complete");

        currentTutorialPhase = "complete";
        instructionText.text = "所有人都看完規則，開始遊戲！";
        BroadcastTutorialStep("complete", "準備開始遊戲！");

        if (spySlideImage != null) spySlideImage.gameObject.SetActive(false);
        if (videoRawImage != null && videoRawImage.transform.parent != null)
        {
            videoRawImage.transform.parent.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(1.0f);


        networkManager.BroadcastNavigateToPlaying();
        gameManager.UpdatePlayerInfo(networkManager.playersInfo);
        tutorialSceneFadeOut.SetNextScene(gameManager.selectedLevel);
        tutorialSceneFadeOut.LoadNextSceneWithFadeOut();
    }

    private void OnPeerDisconnectedInTutorial(string peerId)
    {
        if (!playerProgressMap.ContainsKey(peerId)) return;

        Debug.Log($"[TutorialManager] 玩家 {peerId} 在教學中斷線，自動完成所有步驟（保留玩家資料等待重連）");

        // 自動完成所有教學步驟，避免 WaitUntil 卡住
        PlayerTutorialProgress progress = playerProgressMap[peerId];
        progress.completedCalibration = true;
        progress.completedForward = true;
        progress.completedLeft = true;
        progress.completedRight = true;
        progress.completedBackward = true;

        // UI 卡片標記為完成
        if (playerCardUIMap.ContainsKey(peerId))
        {
            playerCardUIMap[peerId].SetStepStatus(true);
        }
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

        if (IsGyroLevel())
        {
            UpdateInstructionText();
        }
        else
        {
            if (videoPlayer != null) videoPlayer.Stop();
        }
    }

    private void OnDataReceived(string message, string senderPeerId)
    {
        try
        {
            BaseMessage baseData = JsonUtility.FromJson<BaseMessage>(message);
            if (baseData.type == "tutorial_spy_next")
            {
                if (!spyNextReadyPeers.Contains(senderPeerId))
                {
                    spyNextReadyPeers.Add(senderPeerId);
                    Debug.Log($"[TutorialManager] 玩家 {senderPeerId} 準備好切換下一頁 ({spyNextReadyPeers.Count}/{networkManager.playersInfo.Count})");

                    // ▼ 新增：收到訊號時，把該名玩家的卡片設為「已完成」(打勾) ▼
                    if (playerCardUIMap.ContainsKey(senderPeerId))
                    {
                        playerCardUIMap[senderPeerId].SetStepStatus(true);
                    }
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
                if (msg.step == "calibrate" && currentTutorialPhase == "calibrate" && !progress.completedCalibration) { progress.completedCalibration = true; playerCardUIMap[senderPeerId].SetStepStatus(true); progressMade = true; }
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

    void BroadcastTutorialStep(string step, string message)
    {
        TutorialInstructionMessage tutorialMsg = new TutorialInstructionMessage { type = "tutorial_instruction", step = step, message = message };
        networkManager.webRTCConnection.SendDataChannelMessage(JsonUtility.ToJson(tutorialMsg));
    }

    void PlayStepVideo(string step)
    {
        Debug.Log($"[Video] PlayStepVideo called: step={step}, videoPlayer={videoPlayer != null}");
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
        Debug.Log($"[Video] clip={clipToPlay?.name ?? "NULL"}");
        if (clipToPlay != null)
        {
            videoPlayer.clip = clipToPlay;
            videoPlayer.Prepare();
            Debug.Log($"[Video] Preparing clip: {clipToPlay.name}");
        }
        else
        {
            Debug.LogWarning($"[Video] No clip found for step: {step}");
        }
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log($"[Video] OnVideoPrepared: texture={vp.texture != null}, rawImage={videoRawImage != null}");
        if (videoRawImage != null)
        {
            Debug.Log($"[Video] rawImage.active={videoRawImage.gameObject.activeSelf}, parent.active={videoRawImage.transform.parent?.gameObject.activeSelf}");
            videoRawImage.texture = vp.texture;
        }
        vp.Play();
        Debug.Log($"[Video] Playing! isPlaying={vp.isPlaying}");
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


