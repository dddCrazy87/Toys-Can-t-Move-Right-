using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerPointUiManager : MonoBehaviour
{
    [SerializeField] private GameObject pointUi_4Player, pointUi_3Player, pointUi_2Player;
    [SerializeField] private List<Sprite> pointUi_Covers = new();
    GameManager gameManager;
    Transform curPointUiType;
    void Start()
    {
        pointUi_2Player.SetActive(false);
        pointUi_3Player.SetActive(false);
        pointUi_4Player.SetActive(false);
        gameManager = FindFirstObjectByType<GameManager>();
        curPointUiType = null;
    }
    public void InitialPlayerPointUi()
    {
        int playerCount = gameManager.playersInfo.Count;
        if (playerCount <= 2) { pointUi_2Player.SetActive(true); curPointUiType = pointUi_2Player.transform; }
        if (playerCount == 3) { pointUi_3Player.SetActive(true); curPointUiType = pointUi_3Player.transform; }
        if (playerCount == 4) { pointUi_4Player.SetActive(true); curPointUiType = pointUi_4Player.transform; }

        List<Player> players = gameManager.playersInfo;
        for (int i = 0; i < players.Count; i++)
        {
            string skin = players[i].skin;
            Image icon = curPointUiType.GetChild(i).GetChild(0).GetComponent<Image>();
            icon.sprite = skin switch
            {
                "blue" => pointUi_Covers[0],
                "yellow" => pointUi_Covers[1],
                "green" => pointUi_Covers[2],
                "red" => pointUi_Covers[3],
                _ => icon.sprite
            };
            UpdatePlayerPointUi(i);
        }
    }
    public void UpdatePlayerPointUi(int playerId)
    {
        int point = gameManager.GetPlayerPoint(playerId);
        curPointUiType.GetChild(1).GetComponent<TextMeshProUGUI>().text = point.ToString();
    }
}
