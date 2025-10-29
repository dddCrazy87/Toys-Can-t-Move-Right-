using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class GameRestartManager : MonoBehaviour
{
    public List<SkinColorMapping> skinColorMapping = new();
    public GameManager gameManager;
    [SerializeField] private TextMeshProUGUI winnerName;
    [SerializeField] private Image winnerCover;
    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        FindFirstObjectByType<BgmPlayer>().ChangeBgm();
        Player winner = new();
        foreach (var item in gameManager.playersInfo)
        {
            if (item.point > winner.point) winner = item;
        }
        List<ColorAvatarMapping> avatarMappingList = skinColorMapping.FirstOrDefault(x => x.skin == winner.skin).avatarMapping;
        if (avatarMappingList == null) return;
        ColorAvatarMapping mapping = avatarMappingList.FirstOrDefault(x => x.color == winner.color);
        if (mapping == null) return;
        winnerCover.sprite = mapping.avatarSprite;
        winnerName.text = winner.name;
    }

}
