using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject yellowPlayersPrefab;
    public GameObject bluePlayersPrefab;
    public GameObject greenPlayersPrefab;
    public GameObject redPlayersPrefab;
    public List<Player> playersInfo = new List<Player>();

    private bool gameStarted = false;

    void Awake()
    {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
            return;
        }
    }


    void Start()
    {
        // Debug.Log("等待玩家註冊...");
    }

    // 當從網頁接收到玩家資訊時，呼叫這個函式
    public ItemManager itemManager;
    public void StartGame(List<Player> players)
    {
        if (gameStarted) return;

        playersInfo = players;
        // 分配出生點
        AssignSpawnPoints();
        // 生成玩家
        //SpawnPlayers();
        SpawnPlayersForTest();
        // 生成道具
        itemManager.StartSpawnItems();

        gameStarted = true;
        // Debug.Log("遊戲開始!");
    }

    // 預先設定好的出生點
    public Transform[] spawnPoints;
    public float playerSpawnSpacing = 1.5f;
    private void AssignSpawnPoints()
    {
        List<Transform> availablePoints = new List<Transform>(spawnPoints);
        System.Random rnd = new System.Random();

        foreach (var player in playersInfo)
        {
            int index = rnd.Next(availablePoints.Count);
            player.spawnPoint = availablePoints[index].position;
            availablePoints.RemoveAt(index);
        }
    }

    private void SpawnPlayersForTest()
    {
        foreach (var player in playersInfo)
        {
            GameObject go = null;
            switch (player.skin) {
                case "yellow":
                    go = Instantiate(yellowPlayersPrefab, player.spawnPoint, Quaternion.identity);
                    go.GetComponent<PlayerController>().isP1 = true;
                    break;
                case "blue":
                    go = Instantiate(bluePlayersPrefab, player.spawnPoint, Quaternion.identity);
                    go.GetComponent<PlayerController>().isP2 = true;
                    break;
                case "green":
                    go = Instantiate(greenPlayersPrefab, player.spawnPoint, Quaternion.identity);
                    Destroy(go.GetComponent<PlayerController>());
                    break;
                case "red":
                    go = Instantiate(redPlayersPrefab, player.spawnPoint, Quaternion.identity);
                    Destroy(go.GetComponent<PlayerController>());
                    break;
                default:
                    break;
            }
        }
    }

    private void SpawnPlayers()
    {
        foreach (var player in playersInfo)
        {
            switch (player.skin) {
                case "yellow":
                    Instantiate(yellowPlayersPrefab, player.spawnPoint, Quaternion.identity);
                    break;
                case "blue":
                    Instantiate(bluePlayersPrefab, player.spawnPoint, Quaternion.identity);
                    break;
                case "green":
                    Instantiate(greenPlayersPrefab, player.spawnPoint, Quaternion.identity);
                    break;
                case "red":
                    Instantiate(redPlayersPrefab, player.spawnPoint, Quaternion.identity);
                    break;
                default:
                    break;
            }
        }
    }
}



// 定義隊伍資料結構
[System.Serializable]
public class Player
{
    public string name = "";
    public string skin = "";
    public Vector3 spawnPoint;
}
