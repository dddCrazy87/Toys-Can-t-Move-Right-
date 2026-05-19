using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TapEatGameManager : MonoBehaviour
{
    [Header("Settings")]
    public float gameDuration = 60f;
    public int bitesPerFood = 8;

    [Header("References")]
    public CountDownUI countdownUI;                // 遊戲中 60 秒倒數 UI
    public SceneFadeInFadeOut sceneFadeInFadeOut;   // 場景轉場（可選）
    [SerializeField] private AudioClip gameOverSound;   // 遊戲結束音效

    [Header("上傳系統")]
    public ImgBBUploader imgUploader;               // 截圖上傳（可選）

    [Header("Players")]
    public FoodController[] foodControllers;        // 每位玩家的食物控制器 (長度 4)
    public float playerScale = 2f;                  // 角色放大倍率
    public Transform[] playerPositions;              // 每位玩家的位置（在 Inspector 拖入）

    [Header("音效")]
    public AudioClip[] biteSounds;                   // 咬一口音效（多個，隨機播放）
    public AudioClip plateCompleteSound;            // 吃完一盤音效
    public AudioClip goldenCompleteSound;           // 吃完金色食物音效
    public AudioClip trashEatSound;                 // 吃到垃圾音效
    public AudioClip discardSound;                  // 丟棄食物音效
    public AudioClip bgmClip;                       // 背景音樂
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;

    private AudioSource sfxSource;                  // 音效播放器
    private AudioSource bgmSource;                  // 背景音樂播放器
    private NetworkManager networkManager;
    private GameManager gameManager;
    private Dictionary<int, PlayerEatState> playerStates = new();
    private Dictionary<int, EatAnimator> eatAnimators = new();
    private Dictionary<int, BiteParticle> biteParticles = new();
    private PlayerPointUiManager pointUiManager;
    private bool isGameActive = false;

    private class PlayerEatState
    {
        public int totalBites = 0;
        public int platesCompleted = 0;
        public int score = 0;
    }

    void Start()
    {
        networkManager = FindFirstObjectByType<NetworkManager>();
        gameManager = FindFirstObjectByType<GameManager>();

        // 初始化音效播放器
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.spatialBlend = 0f;
        bgmSource.loop = true;

        // 暫停主 BGM（跟 ToyBox 一樣）
        BgmPlayer mainBgm = FindFirstObjectByType<BgmPlayer>();
        if (mainBgm != null) mainBgm.PauseBGM();

        // 先顯示 GameStartCountDown 3-2-1，然後開始遊戲
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
        if (gameManager == null || networkManager == null) return;

        // TapEat 自己處理角色生成（不用 GameManager.StartGame 的隨機 SpawnPoint）
        gameManager.isGameStart = true;

        // 設定玩家 index
        int playerIdx = 0;
        foreach (var player in gameManager.playersInfo)
        {
            player.index = playerIdx++;
        }

        // 根據玩家數量，把角色生成在對應的 FoodController 旁邊
        Camera cam = Camera.main;
        int i = 0;
        foreach (var player in gameManager.playersInfo)
        {
            if (i >= foodControllers.Length || foodControllers[i] == null)
            {
                i++;
                continue;
            }

            // 找到角色 prefab 並生成
            var prefabMappingList = gameManager.skinColorsMapping
                .Find(x => x.skin == player.skin)?.prefabMapping;
            if (prefabMappingList == null) { i++; continue; }
            var mapping = prefabMappingList.Find(x => x.color == player.color);
            if (mapping == null) { i++; continue; }

            // 生成位置：從 playerPositions 陣列取得（在 Inspector 手動設定）
            Vector3 spawnPos = (playerPositions != null && i < playerPositions.Length && playerPositions[i] != null)
                ? playerPositions[i].position
                : foodControllers[i].transform.position;

            GameObject go = Instantiate(mapping.prefab, spawnPos, Quaternion.identity);
            PlayerController pc = go.GetComponent<PlayerController>();
            pc.Initialize(player.name, player.index, player.color);
            gameManager.playerControllers[player.index] = pc;

            // 停用移動、旋轉、物理和碰撞（這關不需要走路）
            pc.moveSpeed = 0f;
            pc.rotateSpeed = 0f;
            pc.enabled = false; // 停用 PlayerController 的 Update/FixedUpdate
            Collider col = pc.GetComponent<Collider>();
            if (col != null) col.enabled = false;
            Rigidbody rb = pc.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // 放大角色
            pc.transform.localScale *= playerScale;

            // 讓角色面朝相機
            if (cam != null)
            {
                Vector3 lookDir = cam.transform.position - spawnPos;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    pc.transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
                }
            }

            // 強制歸位（確保放大和旋轉沒有影響位置）
            pc.transform.position = spawnPos;

            // 加上吃東西動畫
            EatAnimator eat = pc.gameObject.AddComponent<EatAnimator>();
            eatAnimators[player.index] = eat;

            // 初始化玩家吃東西狀態
            playerStates[player.index] = new PlayerEatState();

            // 設定 FoodController
            foodControllers[i].playerIndex = player.index;

            // 加上碎屑粒子效果
            BiteParticle bp = foodControllers[i].gameObject.AddComponent<BiteParticle>();
            biteParticles[player.index] = bp;

            i++;
        }

        // 隱藏未使用的玩家位置（盤子 + 食物）
        for (int j = 0; j < foodControllers.Length; j++)
        {
            if (!playerStates.ContainsKey(j) && foodControllers[j] != null)
            {
                foodControllers[j].transform.parent.gameObject.SetActive(false);
            }
        }

        // 初始化分數 UI（跟 Toybox 一樣）
        pointUiManager = FindFirstObjectByType<PlayerPointUiManager>();
        if (pointUiManager != null)
        {
            pointUiManager.InitialPlayerPointUi();
        }

        // 開始 60 秒倒數
        if (countdownUI != null)
        {
            countdownUI.StartCountdown(gameDuration, OnTimerFinished);
        }

        // 開始播放背景音樂
        if (bgmClip != null && bgmSource != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.volume = bgmVolume;
            bgmSource.Play();
        }

        isGameActive = true;
        Debug.Log("[TapEat] 遊戲開始！");
    }


    void OnTimerFinished()
    {
        isGameActive = false;
        OnGameEnd();
    }

    /// <summary>
    /// 收到玩家的點擊動作（由 NetworkManager 呼叫）
    /// </summary>
    public void OnTapAction(int playerIndex)
    {
        if (!isGameActive) return;
        if (!playerStates.ContainsKey(playerIndex)) return;
        if (foodControllers == null || playerIndex >= foodControllers.Length || foodControllers[playerIndex] == null) return;

        var fc = foodControllers[playerIndex];
        if (fc.isServing) return; // 換菜動畫中，忽略點擊

        var state = playerStates[playerIndex];

        // 如果點到不能吃的食物 → 扣分 + 播放垃圾完成音效
        if (fc.IsTrash())
        {
            state.score += fc.GetCurrentScore(); // 負分
            if (trashEatSound != null && sfxSource != null)
                sfxSource.PlayOneShot(trashEatSound, sfxVolume);
            Debug.Log($"[TapEat] Player {playerIndex} 吃到垃圾！扣分！");

            // 播放吃東西動畫（表示吃了）
            if (eatAnimators.ContainsKey(playerIndex) && eatAnimators[playerIndex] != null)
                eatAnimators[playerIndex].PlayEat();

            fc.ServeNextFood();
            UpdateScore(playerIndex, state);
            return;
        }

        state.totalBites++;

        // 播放咬一口音效（隨機選一個）
        if (biteSounds != null && biteSounds.Length > 0 && sfxSource != null)
        {
            AudioClip clip = biteSounds[Random.Range(0, biteSounds.Length)];
            if (clip != null) sfxSource.PlayOneShot(clip, sfxVolume);
        }

        // 播放吃東西動畫
        if (eatAnimators.ContainsKey(playerIndex) && eatAnimators[playerIndex] != null)
        {
            eatAnimators[playerIndex].PlayEat();
        }

        // 噴出碎屑
        if (biteParticles.ContainsKey(playerIndex) && biteParticles[playerIndex] != null)
        {
            biteParticles[playerIndex].Play();
        }

        // 食物被咬
        bool finished = fc.Bite();

        if (finished)
        {
            // 播放吃完音效（金色食物用不同音效）
            if (fc.currentFood.type == FoodType.Golden && goldenCompleteSound != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(goldenCompleteSound, sfxVolume);
            }
            else if (plateCompleteSound != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(plateCompleteSound, sfxVolume);
            }
            state.platesCompleted++;
            state.score += fc.GetCurrentScore();
            fc.ServeNextFood();
            Debug.Log($"[TapEat] Player {playerIndex} 吃完第 {state.platesCompleted} 盤！得 {fc.GetCurrentScore()} 分");
        }

        UpdateScore(playerIndex, state);
    }

    /// <summary>
    /// 收到玩家的滑動丟棄動作（由 NetworkManager 呼叫）
    /// </summary>
    public void OnDiscardAction(int playerIndex)
    {
        if (!isGameActive) return;
        if (!playerStates.ContainsKey(playerIndex)) return;
        if (foodControllers == null || playerIndex >= foodControllers.Length || foodControllers[playerIndex] == null) return;

        var fc = foodControllers[playerIndex];
        if (fc.isServing) return; // 換菜動畫中，忽略滑動

        Debug.Log($"[TapEat] Player {playerIndex} 丟棄了食物（{fc.currentFood?.type}）");
        if (discardSound != null && sfxSource != null)
            sfxSource.PlayOneShot(discardSound, sfxVolume);
        fc.Discard();
    }

    private void UpdateScore(int playerIndex, PlayerEatState state)
    {
        if (gameManager != null)
        {
            gameManager.playersInfo[playerIndex].point = Mathf.Max(0, state.score);

            if (pointUiManager != null)
            {
                pointUiManager.UpdatePlayerPointUi(playerIndex);
            }
        }
    }

    private void OnGameEnd()
    {
        Debug.Log("[TapEat] 時間到！");

        // 停止背景音樂
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }

        // 播放遊戲結束音效
        if (gameOverSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(gameOverSound);
        }

        StartCoroutine(EndGameRoutine());
    }

    private IEnumerator EndGameRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        string uploadedUrl = "";

        // 截圖上傳（如果有設定）
        if (imgUploader != null)
        {
            yield return StartCoroutine(imgUploader.UploadToImgBB((url) =>
            {
                uploadedUrl = url;
            }));

            if (!string.IsNullOrEmpty(uploadedUrl))
            {
                if (gameManager) gameManager.hasPostcard = true;
            }
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (networkManager != null)
        {
            networkManager.BroadcastTerminate(uploadedUrl);
        }

        // 場景轉場（如果有設定）
        if (sceneFadeInFadeOut != null)
        {
            sceneFadeInFadeOut.LoadNextSceneWithFadeOut();
        }
    }

}
