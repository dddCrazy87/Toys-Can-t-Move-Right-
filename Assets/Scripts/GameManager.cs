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
    ItemManager itemManager;

    public void StartGame()
    {
        if (playersInfo.Count <= 0)
        {
            print("No player registered");
            return;
        }
        // 設定玩家Id
        AssignPlayerIndex();
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

    // ------------- Manage Player Point --------------
    public void IncreasePlayerPoint(int playerIndex, int number)
    {
        playersInfo[playerIndex].point += number;
    }

    public int GetPlayerPoint(int playerIndex)
    {
        return playersInfo[playerIndex].point;
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