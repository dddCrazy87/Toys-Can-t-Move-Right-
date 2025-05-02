using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject playerPrefab;
    public GameObject caslePrefab;
    public List<Team> teams = new List<Team>();

    private bool gameStarted = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }


    void Start()
    {
        Debug.Log("等待玩家註冊...");
    }

    // 當從網頁接收到玩家資訊時，呼叫這個函式
    public ItemManager itemManager;
    public void StartGame(List<Team> receivedTeams)
    {
        if (gameStarted) return;

        teams = receivedTeams;
        // 分配出生點
        AssignSpawnPoints();
        // 生成玩家
        SpawnPlayers();
        // 生成道具
        itemManager.StartSpawnItems();

        gameStarted = true;
        Debug.Log("遊戲開始!");
    }

    // 預先設定好的出生點
    public Transform[] spawnPoints;
    public float playerSpawnSpacing = 1.5f;
    private void AssignSpawnPoints()
    {
        List<Transform> availablePoints = new List<Transform>(spawnPoints);
        System.Random rnd = new System.Random();

        foreach (var team in teams)
        {
            int index = rnd.Next(availablePoints.Count);
            team.spawnPoint = availablePoints[index];
            availablePoints.RemoveAt(index);
        }
    }

    private void SpawnPlayers()
    {
        foreach (var team in teams)
        {
            // 給該隊伍隨機顏色
            Color teamColor = Random.ColorHSV();
            int playerCount = team.players.Count;

            GameObject player = Instantiate(playerPrefab);
            player.transform.position = team.spawnPoint.position + new Vector3(0,0,1.38f);
            Instantiate(caslePrefab, team.spawnPoint.position - new Vector3(0,0.2f,0), Quaternion.identity);
            player.GetComponent<PlayerController>().Initialize("Player1", teamColor);
            // for (int i = 0; i < playerCount; i++)
            // {
            //     // 計算玩家的生成位置 (圓形排列)
            //     Vector2 offset = GetCircularOffset(i, playerCount);
            //     Vector3 spawnPos = team.spawnPoint.position + new Vector3(offset.x, 0, offset.y);
            //     GameObject player = Instantiate(playerPrefab, spawnPos, playerPrefab.transform.rotation);
            //     print(spawnPos);
            //     player.transform.position = spawnPos;
            //     player.GetComponent<PlayerController>().Initialize(team.players[i], teamColor);
            // }
        }
    }

    private Vector2 GetCircularOffset(int index, int total)
    {
        float angle = (index / (float)total) * Mathf.PI * 2; // 均勻分布角度
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * playerSpawnSpacing;
    }
}



// 定義隊伍資料結構
[System.Serializable]
public class Team
{
    public string teamID;
    public List<string> players;
    public Transform spawnPoint;
}
