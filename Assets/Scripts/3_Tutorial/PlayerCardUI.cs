using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class PlayerCardUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI playerNameText;
    public Image playerAvatarImage;

    [Header("Player Prefab")]
    public List<SkinColorMapping> skinColorsMapping = new();

    [Header("Step Icons")]
    public GameObject incompleteIcon;

    public GameObject completeIcon;
    public void SetupCard(PlayerTutorialProgress progress)
    {
        playerNameText.text = progress.playerInfo.name;
        string playerSkin = progress.playerInfo.skin;
        string playerColor = progress.playerInfo.color;
        Sprite avatarToShow = null;

        List<ColorAvatarMapping> avatarMappingList = skinColorsMapping.FirstOrDefault(x => x.skin == playerSkin).avatarMapping;
        if (avatarMappingList == null) return;
        ColorAvatarMapping mapping = avatarMappingList.FirstOrDefault(x => x.color == playerColor);
        if (mapping == null) return;
        avatarToShow = mapping.avatarSprite;

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