using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic; 
using System.Linq; 

[System.Serializable]
public class SkinAvatarMapping
{
    public string skinName;
    public Sprite avatarSprite;
}

public class PlayerCardUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI playerNameText;
    public Image playerAvatarImage;

    [Header("Avatar Mapping")]
    public List<SkinAvatarMapping> skinAvatarMap;

    [Header("Step Icons")]
    public GameObject incompleteIcon;

    public GameObject completeIcon;
    public void SetupCard(PlayerTutorialProgress progress)
    {
        playerNameText.text = progress.playerInfo.name;
        string playerSkin = progress.playerInfo.skin;
        Sprite avatarToShow = null;

        if (skinAvatarMap != null)
        {
            SkinAvatarMapping mapping = skinAvatarMap.FirstOrDefault(m => m.skinName == playerSkin);
            if (mapping != null)
            {
                avatarToShow = mapping.avatarSprite;
            }
        }

        if (avatarToShow != null)
        {
            playerAvatarImage.sprite = avatarToShow;
            playerAvatarImage.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"PlayerCardUI: 找不到 skin '{playerSkin}' 對應的頭像。");
            // playerAvatarImage.gameObject.SetActive(false); 
        }
    }
    
    public void SetStepStatus(bool isComplete)
    {
        if (incompleteIcon != null)
        {
            incompleteIcon.SetActive(!isComplete);
        }
        if (completeIcon != null)
        {
            completeIcon.SetActive(isComplete);
        }
    }
}