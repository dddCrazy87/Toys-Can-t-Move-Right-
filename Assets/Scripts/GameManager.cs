using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SimpleWebRTC;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;




public class GameManager : MonoBehaviour
{
    public bool isGameStart = false;
    public Dictionary<string, PlayerController> playerControllers = new();


    public void StartGame()
    {
        if (playersInfo.Count <= 0)
        {
            print("No player registered");
            return;
        }
        // 設定玩家Id
        AssignPlayerIndex();
        // 生成分數UI
        RenderPointUI();
        // 分配出生點
        AssignSpawnPoints();
        // 生成玩家
        SpawnPlayers();
        // 生成道具
        ;
        if (itemManager = FindFirstObjectByType<ItemManager>())
        {
            itemManager.StartSpawnItems();
        }
        else
        {
            print("No Item Manager");
        }
        isGameStart = true;
    }

    // ------------- Increase Player Point --------------
    public void PlayerIncreasePointByNumber(int playerIndex, int number)
    {
        playersInfo[playerIndex].point += number;
        playerPointUI[playerIndex].GetChild(1).GetComponent<TextMeshProUGUI>().text = playersInfo[playerIndex].point.ToString();
    }

    // ------- Assign Player Index -------

    [Header("Player Data")]
    public List<Player> playersInfo = new();
    public GameObject yellowPlayersPrefab, bluePlayersPrefab, greenPlayersPrefab, redPlayersPrefab;
    void AssignPlayerIndex()
    {
        for (int i = 0; i < playersInfo.Count; i++)
        {
            playersInfo[i].index = i;
        }
    }

    // --------- Spawn Players -----------

    [Header("Player Spawn Setting")]
    public List<Transform> playerSpawnPoints = new();
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
            GameObject prefab = player.skin switch
            {
                "yellow" => yellowPlayersPrefab,
                "blue" => bluePlayersPrefab,
                "green" => greenPlayersPrefab,
                "red" => redPlayersPrefab,
                _ => null
            };

            if (prefab == null) continue;

            GameObject go = Instantiate(prefab, player.spawnPoint, Quaternion.identity);
            PlayerController pc = go.GetComponent<PlayerController>();
            pc.Initialize(player.name, player.index);
            playerControllers[player.skin] = pc;
        }
    }

    // ------------- Point UI Setting -------------

    [Header("UI Setting")]
    public List<Transform> pointUiTypes = new();
    public List<Sprite> pointUiSkins = new();
    List<Transform> playerPointUI = new();
    private void RenderPointUI()
    {
        if (playersInfo == null || playersInfo.Count < 2)
        {
            Debug.LogWarning($"Player ({playersInfo?.Count}) is less than 2 players.");
            foreach (var item in pointUiTypes) item.gameObject.SetActive(false);
            return;
        }

        int uiIndex = playersInfo.Count - 2;
        if (uiIndex < 0 || uiIndex >= pointUiTypes.Count)
        {
            Debug.LogError($"Can't find {playersInfo.Count} players' UI (index {uiIndex})");
            return;
        }

        pointUiTypes[uiIndex].gameObject.SetActive(true);
        for (int i = 0; i < playersInfo.Count; i++)
        {
            playerPointUI.Add(pointUiTypes[uiIndex].GetChild(i));
        }

        for (int i = 0; i < playersInfo.Count; i++)
        {
            string skin = playersInfo[i].skin;
            Image icon = playerPointUI[i].GetChild(0).GetComponent<Image>();
            icon.sprite = skin switch
            {
                "blue" => pointUiSkins[0],
                "yellow" => pointUiSkins[1],
                "green" => pointUiSkins[2],
                "red" => pointUiSkins[3],
                _ => icon.sprite
            };
            playerPointUI[i].GetChild(1).GetComponent<TextMeshProUGUI>().text = playersInfo[i].point.ToString();
        }
    }

    // ---------Player Movement --------

    public void OnRemotePlayerMove(string skin, float x, float y)
    {
        if (!isGameStart) return;
        if (playerControllers.TryGetValue(skin, out PlayerController pc))
        {
            pc.SetNetworkInput(x, y);
        }
    }

    // --------- Load Player Data ---------

    public void UpdatePlayerInfo(List<Player> players)
    {
        playersInfo = players;
    }

    // ----------- Item Script -----------

    [Header("Item Script")]
    public ItemManager itemManager;

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