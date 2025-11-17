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
    ItemSpawner itemSpawner;

    [Header("Player Data")]
    public List<Player> playersInfo = new();
    [Header("Player Prefab")]
    public List<SkinColorMapping> skinColorsMapping = new();
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
        // 設定玩家Id
        AssignPlayerIndex();
        // 分配出生點
        AssignSpawnPoints();
        // 生成玩家
        SpawnPlayers();
        // 生成道具
        ;

        if (itemSpawner = FindFirstObjectByType<ItemSpawner>())
        {
            itemSpawner.StartSpawnItems();
        }
        else
        {
            print("No Item Manager");
        }
        isGameStart = true;
    }

    // ---------Player Movement --------

    public void OnRemotePlayerMove(int index, float x, float y)
    {
        if (!isGameStart) return;
        if (playerControllers.TryGetValue(index, out PlayerController pc))
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
            GameObject go = Instantiate(mapping.prefab, player.spawnPoint, Quaternion.identity);
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