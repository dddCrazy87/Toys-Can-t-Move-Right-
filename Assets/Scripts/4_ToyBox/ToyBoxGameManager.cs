using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ToyBoxGameManager : MonoBehaviour
{
    [SerializeField] private CountDownUI countdownUI;
    [SerializeField] private SceneFadeInFadeOut sceneFadeInFadeOut;
    [SerializeField] private float gameTimeLimit = 90f;
    [SerializeField] private AudioSource gameOverAudio;

    [Header("遊戲說明")]
    [SerializeField] private GameInstructionUI instructionUI;

    [Header("上傳系統")]
    [SerializeField] private ImgBBUploader imgUploader;

    BgmPlayer bgmPlayer;
    NetworkManager networkManager;
    GameManager gameManager;

    void Start()
    {
        bgmPlayer = FindFirstObjectByType<BgmPlayer>();
        gameManager = FindFirstObjectByType<GameManager>();
        networkManager = FindFirstObjectByType<NetworkManager>();

        // 倒數期間先暫停 BGM
        if (bgmPlayer) bgmPlayer.PauseBGM();

        // 先顯示說明圖，完成後再開始倒數
        if (instructionUI != null)
        {
            Debug.Log("[ToyBoxGameManager] 顯示說明圖...");
            instructionUI.ShowInstruction(OnInstructionComplete);
        }
        else
        {
            // 沒有說明圖，直接開始倒數
            Debug.LogWarning("[ToyBoxGameManager] instructionUI 未設定，跳過說明圖");
            OnInstructionComplete();
        }
    }

    void OnInstructionComplete()
    {
        var gameStartCountDown = FindFirstObjectByType<GameStartCountDown>();
        if (gameStartCountDown != null)
        {
            gameStartCountDown.CountDownAndStartGame(OnCountDownFinished);
        }
        else
        {
            OnCountDownFinished();
        }
    }

    void OnCountDownFinished()
    {
        gameManager.StartGame();

        // 在 ToyBoxGameManager.cs 的 OnCountDownFinished() 中：

        // ++ 未來的完全自動化數值灌入方案 ++
        foreach (var kvp in gameManager.playerControllers)
        {
            PlayerController player = kvp.Value;
            if (player == null) continue;

            ToyBoxPlayer toyBoxPlayer = player.gameObject.GetComponent<ToyBoxPlayer>();
            toyBoxPlayer.Initialize();
        }

        if (bgmPlayer) bgmPlayer.ChangeBgm();

        ItemSpawner itemSpawner = FindFirstObjectByType<ItemSpawner>();
        if (itemSpawner != null)
        {
            itemSpawner.StartSpawnItems();
        }
        // ++++++++++++++++++++++++++++++++++++++++++++++

        FindFirstObjectByType<PlayerPointUiManager>().InitialPlayerPointUi();
        countdownUI.StartCountdown(gameTimeLimit, OnCountdownFinished);
    }

    void OnCountdownFinished()
    {
        if (bgmPlayer) bgmPlayer.PauseBGM();
        gameOverAudio.Play();
        StartCoroutine(EndGameRoutine());
    }

    private IEnumerator EndGameRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        string uploadedUrl = "";

        if (imgUploader != null)
        {
            yield return StartCoroutine(imgUploader.UploadToImgBB((url) =>
            {
                uploadedUrl = url;
            }));

            if (!string.IsNullOrEmpty(uploadedUrl))
            {
                Debug.Log($"[ToyBoxGameManager] 圖片網址: {uploadedUrl}");
                if (gameManager) gameManager.hasPostcard = true;
            }
            else
            {
                Debug.LogWarning("[ToyBoxGameManager] 上傳失敗");
            }
        }
        else
        {
            Debug.LogWarning("[ToyBoxGameManager] imgUploader 未設定，跳過截圖上傳");
            yield return new WaitForSeconds(0.5f);
        }

        if (networkManager) networkManager.BroadcastTerminate(uploadedUrl);
        sceneFadeInFadeOut.LoadNextSceneWithFadeOut();
    }
}
