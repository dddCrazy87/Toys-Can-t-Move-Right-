using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class LobbyUI : MonoBehaviour
{
    [Header("References")]

    public GameObject playerCardPrefab;

    public Transform cardContainer;

    public GameObject playerScrollViewObject;

    [Header("Player Prefab")]
    public List<SkinColorMapping> skinColorsMapping = new();

    private int currentDisplayedPlayerCount = -1;
    private NetworkManager networkManager;

    private void Awake()
    {
        networkManager = FindFirstObjectByType<NetworkManager>();
    }

    // void Update()
    // {
    //     if (networkManager == null || playerScrollViewObject == null) return;

    //     int playerCount = networkManager.playersInfo.Count;

    //     if (playerCount == 0)
    //     {
    //         playerScrollViewObject.SetActive(false);
    //     }
    //     else
    //     {
    //         playerScrollViewObject.SetActive(true);
    //     }

    //     RebuildPlayerList();
    // }

    public void UpdateLobbyUI()
    {
        if (networkManager == null || playerScrollViewObject == null) return;
        playerScrollViewObject.SetActive(true);
        RebuildPlayerList();
    }

    // 用來記錄每張卡片對應的 skin，檢測角色是否變更
    private List<string> displayedSkins = new List<string>();

    void RebuildPlayerList()
    {
        bool needsRebuild = false;

        if (cardContainer.childCount != networkManager.playersInfo.Count)
        {
            needsRebuild = true;
        }
        else
        {
            for (int i = 0; i < networkManager.playersInfo.Count; i++)
            {
                Player player = networkManager.playersInfo[i];
                Transform card = cardContainer.GetChild(i);

                TextMeshProUGUI nameText = card.GetComponentInChildren<TextMeshProUGUI>();

                // 檢查名字或角色是否變更
                if (nameText.text != player.name ||
                    i >= displayedSkins.Count ||
                    displayedSkins[i] != player.skin)
                {
                    needsRebuild = true;
                    break;
                }
            }
        }

        if (needsRebuild)
        {
            ForceRebuild();

            // 更新記錄的 skin 列表
            displayedSkins.Clear();
            foreach (Player player in networkManager.playersInfo)
            {
                displayedSkins.Add(player.skin);
            }
        }
    }

    void ForceRebuild()
    {
        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }

        if (networkManager == null) return;

        foreach (Player player in networkManager.playersInfo)
        {
            GameObject card = Instantiate(playerCardPrefab, cardContainer);

            TextMeshProUGUI nameText = card.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = player.name;
            }

            Image avatarImage = card.GetComponentInChildren<Image>();
            if (avatarImage != null)
            {
                List<ColorAvatarMapping> avatarMappingList = skinColorsMapping.FirstOrDefault(x => x.skin == player.skin).avatarMapping;
                if (avatarMappingList == null) continue;
                ColorAvatarMapping mapping = avatarMappingList.FirstOrDefault(x => x.color == player.color);
                if (mapping == null) continue;
                avatarImage.sprite = mapping.avatarSprite;
            }
        }

        currentDisplayedPlayerCount = networkManager.playersInfo.Count;
    }
}