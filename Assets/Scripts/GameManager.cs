using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private bool gameStarted = false;

    [Header("Player Data")]
    public List<Player> playersInfo = new List<Player>();
    public GameObject yellowPlayersPrefab, bluePlayersPrefab, greenPlayersPrefab, redPlayersPrefab;
    public void StartGame(List<Player> players)
    {
        if (gameStarted) return;

        playersInfo = players;
        if (playersInfo.Count < 2 || playersInfo.Count > 4) {
            Debug.LogWarning("超過限制");
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
        itemManager.StartSpawnItems();

        gameStarted = true;
    }

    void AssignPlayerIndex() {
        for(int i = 0; i < playersInfo.Count; i ++) {
            playersInfo[i].index = i;
        }
    }

    [Header("Item Script")]
    public ItemManager itemManager;
    [Header("Player Spawn Setting")]
    public Transform[] spawnPoints;
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

    private void SpawnPlayers()
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
            go.GetComponent<PlayerController>().Initialize(player.name, player.index);
        }
    }

    [Header("UI Setting")]
    public List<Transform> pointUiTypes = new();
    public List<Sprite> pointUiSkins = new();
    List<Transform> playerPointUI = new();
    private void RenderPointUI() {
        foreach (var item in pointUiTypes) {
            item.gameObject.SetActive(false);
        }
        pointUiTypes[playersInfo.Count-2].gameObject.SetActive(true);
        for (int i = 0; i < playersInfo.Count; i++) {
            playerPointUI.Add(pointUiTypes[playersInfo.Count-2].GetChild(i));
        }
        for (int i = 0; i < playersInfo.Count; i ++) {
            switch (playersInfo[i].skin) {
                case "blue":
                    playerPointUI[i].GetChild(0).GetComponent<Image>().sprite = pointUiSkins[0];
                    break;
                case "yellow":
                    playerPointUI[i].GetChild(0).GetComponent<Image>().sprite = pointUiSkins[1];
                    break;
                case "green":
                    playerPointUI[i].GetChild(0).GetComponent<Image>().sprite = pointUiSkins[2];
                    break;
                case "red":
                    playerPointUI[i].GetChild(0).GetComponent<Image>().sprite = pointUiSkins[3];
                    break;
                default:
                    break;
            }
            playerPointUI[i].GetChild(1).GetComponent<TextMeshProUGUI>().text = playersInfo[i].point.ToString();
        }
    }
    public void PlayerIncreasePointByNumber(int playerIndex, int number) {
        playersInfo[playerIndex].point += number;
        playerPointUI[playerIndex].GetChild(1).GetComponent<TextMeshProUGUI>().text = playersInfo[playerIndex].point.ToString();
    }
}



[System.Serializable]
public class Player {
    public string name = "";
    public string skin = "";
    public Vector3 spawnPoint;
    public int point = 0;
    public int index;
}
