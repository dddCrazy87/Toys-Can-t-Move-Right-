using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SimpleWebRTC;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public bool isGameStart = false;
    public Dictionary<int, PlayerController> playerControllers = new();
    [Header("Level Selection")]
    public string selectedLevel = "4_ColorPaper";  // 預設關卡
    public static readonly string[] AvailableLevels = { "4_ColorPaper", "4_Toybox", "4_TapEat", "4_SpyGame" };

    [Header("Player Data")]
    public List<Player> playersInfo = new();
    [Header("Player Prefab")]
    public List<SkinColorMapping> skinColorsMapping = new();
    [Header("Postcard")]
    public bool hasPostcard = false;

    [Header("Is testing game")]
    [SerializeField] bool isTesting = false;
    [SerializeField] string p1Skin, p2Skin;

    public void StartGame()
    {
        if (playersInfo.Count <= 0)
        {
            if (isTesting)
            {
                playersInfo.Add(new Player { name = "p1", skin = p1Skin, color = "blue" });
                playersInfo.Add(new Player { name = "p2", skin = p2Skin, color = "yellow" });
            }
            else
            {
                Debug.Log("No player");
                return;
            }
        }
        // if (playersInfo.Count == 1)
        // {
        //     playersInfo[0].skin = playersInfo[0].name;
        // }
        AssignPlayerIndex();
        AssignSpawnPoints();
        SpawnPlayers();
        isGameStart = true;
    }

    // ---------Player Movement --------

    // 保留原方法供其他場景使用（不需要按壓的場景）
    public void OnRemotePlayerMove(int index, float x, float y)
    {
        OnRemotePlayerMove(index, x, y, false);
    }

    // 支援按壓狀態的重載（ColorPaper 場景使用）
    public void OnRemotePlayerMove(int index, float x, float y, bool isPress)
    {
        if (!isGameStart) return;
        if (playerControllers.TryGetValue(index, out PlayerController pc))
        {
            pc.SetNetworkInput(x, y, isPress);
        }
    }

    // --------- Load Player Data ---------

    public void UpdatePlayerInfo(List<Player> players)
    {
        playersInfo = players;
    }

    // ------------- Manage Player Point --------------
    public void IncreasePlayerPoint(int playerIndex, int number)
    {
        playersInfo[playerIndex].point += number;
    }

    public void SetPlayerPoint(int playerIndex, int point)
    {
        playersInfo[playerIndex].point = point;
    }

    public int GetPlayerPoint(int playerIndex)
    {
        return playersInfo[playerIndex].point;
    }

    public List<Player> GetNo1Player()
    {
        List<Player> ps = new();

        if (playersInfo == null || !playersInfo.Any()) return ps;

        foreach (var player in playersInfo)
        {
            if (ps.Count == 0) ps.Add(player);
            else if (player.point < ps[0].point) continue;
            else if (player.point == ps[0].point) ps.Add(player);
            else { ps.Clear(); ps.Add(player); }
        }

        return ps;
    }

    // ------------- Reset -------------

    public void ResetGameData()
    {
        Debug.Log("ResetGameData()");

        isGameStart = false;
        playersInfo.Clear();
    }

    // ------- Assign Player Index -------
    void AssignPlayerIndex()
    {
        for (int i = 0; i < playersInfo.Count; i++)
        {
            playersInfo[i].index = i;
        }
    }

    // --------- Spawn Players -----------

    List<Transform> playerSpawnPoints = new();
    private void AssignSpawnPoints()
    {
        playerSpawnPoints.Clear();
        GameObject go = GameObject.Find("PlayerSpwanPoint");
        if (go != null)
        {
            Transform tf = go.transform;
            for (int i = 0; i < tf.childCount; ++i)
            {
                playerSpawnPoints.Add(tf.GetChild(i));
            }
        }
        else
        {
            print("No Player Spwan Point");
            return;
        }

        List<Transform> availablePoints = new(playerSpawnPoints);
        System.Random rnd = new();

        foreach (var player in playersInfo)
        {
            int index = rnd.Next(availablePoints.Count);
            player.spawnPoint = availablePoints[index].position;
            availablePoints.RemoveAt(index);
        }
    }

    private void SpawnPlayers()
    {
        foreach (var player in playersInfo)
        {
            List<ColorPrefabMapping> prefabMappingList = skinColorsMapping.FirstOrDefault(x => x.skin == player.skin).prefabMapping;
            if (prefabMappingList == null) continue;
            ColorPrefabMapping mapping = prefabMappingList.FirstOrDefault(x => x.color == player.color);
            if (mapping == null) continue;

            GameObject go;
            if (SceneManager.GetActiveScene().name != "4_Toybox")
            {
                go = Instantiate(mapping.prefab, player.spawnPoint, Quaternion.identity);
            }
            else
            {
                Vector3 spawnPosition = new(
                player.spawnPoint.x,
                mapping.prefab.transform.position.y,
                player.spawnPoint.z
                );

                go = Instantiate(mapping.prefab, spawnPosition, Quaternion.identity);
            }

            PlayerController pc = go.GetComponent<PlayerController>();
            pc.Initialize(player.name, player.index, player.color);
            playerControllers[player.index] = pc;

            if (isTesting)
            {
                PlayerGamingTest pgt = go.AddComponent<PlayerGamingTest>();
                pgt.playerId = player.index;
            }
        }
    }

    // -------------------- ESC Menu ------------------

    private bool isGamePause = false;
    [Header("ESC Menu")]
    [SerializeField] private GameObject escMenu;
    [SerializeField] private GameObject warnMsg;
    [SerializeField] private TextMeshProUGUI warnMsgTxt;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isGamePause = !isGamePause;
            escMenu.SetActive(isGamePause);
            if (isGamePause) Time.timeScale = 0f;
            if (!isGamePause) Time.timeScale = 1f;

            BgmPlayer bgmPlayer = FindFirstObjectByType<BgmPlayer>();
            if (isGamePause && bgmPlayer) bgmPlayer.PauseBGM();
            if (!isGamePause && bgmPlayer) bgmPlayer.PlayBGM();
        }
    }

    public enum ESCMenuOp { BackToHome, BackToLobby, BackToLobbyAndReset, CloseGame }
    ESCMenuOp curOp = ESCMenuOp.BackToHome;
    public void ESCMenuAction(string op)
    {
        curOp = op switch
        {
            "回到首頁" => ESCMenuOp.BackToHome,
            "修改成員" => ESCMenuOp.BackToLobby,
            "重置隊伍" => ESCMenuOp.BackToLobbyAndReset,
            "結束遊戲" => ESCMenuOp.CloseGame,
            _ => ESCMenuOp.BackToHome
        };

        warnMsgTxt.text = curOp switch
        {
            ESCMenuOp.BackToHome => "回到首頁",
            ESCMenuOp.BackToLobby => "修改成員",
            ESCMenuOp.BackToLobbyAndReset => "重置隊伍",
            ESCMenuOp.CloseGame => "結束遊戲",
            _ => "非法操作"
        };
        warnMsg.SetActive(true);
    }

    public void ComfirmESCMenuAction(bool check)
    {
        if (!check) { warnMsg.SetActive(false); return; }

        NetworkManager networkManager = FindFirstObjectByType<NetworkManager>();

        isGameStart = false;
        hasPostcard = false;
        playerControllers.Clear();

        isGamePause = false;
        warnMsg.SetActive(false);
        escMenu.SetActive(false);

        Time.timeScale = 1f;

        StartCoroutine(ExecuteESCAction(curOp, networkManager));

        Invoke(nameof(ToChangeBGM), 0.5f);
    }

    private IEnumerator ExecuteESCAction(ESCMenuOp op, NetworkManager networkManager)
    {
        if (networkManager != null)
        {
            if (op == ESCMenuOp.BackToLobby || op == ESCMenuOp.BackToHome || op == ESCMenuOp.BackToLobbyAndReset)
            {
                networkManager.BroadcastNavigateToLobby();
            }
        }

        yield return new WaitForSeconds(0.5f);

        switch (op)
        {
            case ESCMenuOp.BackToHome:
                if (networkManager != null)
                {
                    networkManager.webRTCConnection.Disconnect();
                    Destroy(networkManager.gameObject);
                }
                playersInfo.Clear();
                SceneManager.LoadScene("1_GameStart");
                break;

            case ESCMenuOp.BackToLobby:
                foreach (var p in playersInfo) p.point = 0;
                SceneManager.LoadScene("2_Setting");
                break;

            case ESCMenuOp.BackToLobbyAndReset:
                if (networkManager != null)
                {
                    networkManager.webRTCConnection.Disconnect();
                    Destroy(networkManager.gameObject);
                }
                playersInfo.Clear();
                SceneManager.LoadScene("2_Setting");
                break;

            case ESCMenuOp.CloseGame:
                if (networkManager != null)
                {
                    networkManager.webRTCConnection.Disconnect();
                }
                Application.Quit();
                break;

            default:
                break;
        }
    }

    private void ToChangeBGM()
    {
        BgmPlayer bgmPlayer = FindFirstObjectByType<BgmPlayer>();
        if (bgmPlayer) bgmPlayer.ChangeBgm();
    }



    // ------------- Dont Destroy On Load -------------

    private static GameManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}



