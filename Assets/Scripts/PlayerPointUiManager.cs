using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class PlayerPointUiManager : MonoBehaviour
{
    [SerializeField] private GameObject pointUi_4Player, pointUi_3Player, pointUi_2Player;

    [Header("Player Prefab")]
    public List<SkinColorMapping> skinColorsMapping = new();
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
            List<ColorAvatarMapping> avatarMappingList = skinColorsMapping.FirstOrDefault(x => x.skin == players[i].skin).avatarMapping;
            if (avatarMappingList == null) continue;
            ColorAvatarMapping mapping = avatarMappingList.FirstOrDefault(x => x.color == players[i].color);
            if (mapping == null) continue;
            Image icon = curPointUiType.GetChild(i).GetChild(0).GetComponent<Image>();
            icon.sprite = mapping.avatarSprite;
            UpdatePlayerPointUi(i);
        }
    }
    public void UpdatePlayerPointUi(int playerId)
    {
        int point = gameManager.GetPlayerPoint(playerId);
        curPointUiType.GetChild(playerId).GetChild(1).GetComponent<TextMeshProUGUI>().text = point.ToString();
    }
}
