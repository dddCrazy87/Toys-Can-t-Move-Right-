using UnityEngine;
using UnityEngine.UI; 
using TMPro; 
using System.Collections.Generic;

public class LobbyUI : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    
    public GameObject playerCardPrefab;
    
    public Transform cardContainer;
    
    public GameObject playerScrollViewObject; 

    [Header("Player Card Sprites")]
    public Sprite redAvatar;
    
    public Sprite blueAvatar;
    public Sprite yellowAvatar;
    public Sprite greenAvatar;
    public Sprite defaultAvatar;

    private int currentDisplayedPlayerCount = -1;
    
    private Dictionary<string, Sprite> avatarMap;

    private void Awake()
    {
        avatarMap = new Dictionary<string, Sprite>
        {
            { "red", redAvatar },
            { "blue", blueAvatar },
            { "yellow", yellowAvatar },
            { "green", greenAvatar }
        };
    }

    void Update()
    {
        if (gameManager == null || playerScrollViewObject == null) return;

        int playerCount = gameManager.playersInfo.Count;

        if (playerCount == 0)
        {
            playerScrollViewObject.SetActive(false);
        }
        else
        {
            playerScrollViewObject.SetActive(true);
        }
        
        RebuildPlayerList(); 
    }

    void RebuildPlayerList()
    {
        
        if (cardContainer.childCount != gameManager.playersInfo.Count)
        {
            ForceRebuild();
        }
        else
        {
            for (int i = 0; i < gameManager.playersInfo.Count; i++)
            {
                Player player = gameManager.playersInfo[i];
                Transform card = cardContainer.GetChild(i);
                
                TextMeshProUGUI nameText = card.GetComponentInChildren<TextMeshProUGUI>();
                
                if (nameText.text != player.name)
                {
                    ForceRebuild();
                    break; 
                }
            }
        }
    }

    void ForceRebuild()
    {
        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }
        
        if (gameManager == null) return;

        foreach (Player player in gameManager.playersInfo)
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
                if (avatarMap.TryGetValue(player.skin.ToLower(), out Sprite avatarSprite))
                {
                    avatarImage.sprite = avatarSprite;
                }
                else
                {
                    avatarImage.sprite = defaultAvatar;
                }
            }
        }
        
        currentDisplayedPlayerCount = gameManager.playersInfo.Count;
    }
}