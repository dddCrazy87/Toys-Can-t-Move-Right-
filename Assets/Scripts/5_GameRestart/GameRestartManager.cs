using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class GameRestartManager : MonoBehaviour
{
    [SerializeField] GameObject rankingColumPrefab;
    [SerializeField] Transform rankingTable;
    [SerializeField] List<GameObject> podiumPrefabs = new();
    [SerializeField] List<Transform> podiumPrefabsPos = new();
    public List<SkinColorMapping> skinColorMapping = new();

    [Header("明信片提示")]
    [SerializeField] private ToastUI postcardToast;
    GameManager gameManager;
    NetworkManager networkManager;
    JsonScoreManager jsonScoreManager;
    BgmPlayer bgmPlayer;
    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        networkManager = FindFirstObjectByType<NetworkManager>();
        jsonScoreManager = FindFirstObjectByType<JsonScoreManager>();
        bgmPlayer = FindFirstObjectByType<BgmPlayer>();

        if (bgmPlayer) bgmPlayer.ChangeBgm();

        List<Player> sortedPlayers = new();

        if (gameManager)
        {
            sortedPlayers = gameManager.playersInfo.OrderByDescending(p => p.point).ToList();
        }
        else
        {
            sortedPlayers = new List<Player>{
                new() {
                    name = "p1",
                    point = 100,
                    skin = "deer",
                    color = "blue"
                },
                new() {
                    name = "p2",
                    point = 50,
                    skin = "wind-up",
                    color = "yellow"
                },
                new() {
                    name = "p3",
                    point = 50,
                    skin = "mouse",
                    color = "green"
                },
                // new() {
                //     name = "p4",
                //     point = 50,
                //     skin = "hat",
                //     color = "red"
                // }
            };
        }


        int prePoint = sortedPlayers[0].point, ppIndex = 0;
        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            var item = sortedPlayers[i];

            // update leaderboard ranking panel
            Transform tr = Instantiate(rankingColumPrefab, rankingTable).transform;
            tr.GetChild(2).GetComponent<TextMeshProUGUI>().text = item.name;
            tr.GetChild(3).GetComponent<TextMeshProUGUI>().text = item.point.ToString();
            tr = null;

            // update leaderboard
            List<ColorAvatarMapping> avatarMappingList = skinColorMapping.FirstOrDefault(x => x.skin == item.skin).avatarMapping;
            if (avatarMappingList == null) return;
            ColorAvatarMapping mapping = avatarMappingList.FirstOrDefault(x => x.color == item.color);
            if (mapping == null) return;

            if (item.point != prePoint)
            {
                prePoint = item.point;
                ppIndex++;
            }

            tr = Instantiate(podiumPrefabs[ppIndex], podiumPrefabsPos[i]).transform;
            Image img = tr.GetChild(2).GetChild(0).GetComponent<Image>();
            TextMeshProUGUI txt = tr.GetChild(2).GetChild(1).GetComponent<TextMeshProUGUI>(); ;

            img.sprite = mapping.avatarSprite;
            txt.text = item.name;

            // update ranker
            if (gameManager) jsonScoreManager.AddPlayerRecord(item.name, item.point, item.skin, item.color);
        }

        // 顯示明信片 Toast 提示
        if (postcardToast != null && gameManager != null && gameManager.hasPostcard)
        {
            postcardToast.Show();
        }
    }

    [SerializeField] SceneFadeInFadeOut sceneFadeInFadeOut;
    public void RestartGame()
    {
        // 通知 Web 端跳轉到選關頁面
        if (networkManager != null)
        {
            networkManager.BroadcastNavigateToLobby();
        }

        // 重置 GameManager（清分數等，但不銷毀）
        if (gameManager != null)
        {
            gameManager.isGameStart = false;
            gameManager.hasPostcard = false;
            gameManager.playerControllers.Clear();
            foreach (var p in gameManager.playersInfo)
            {
                p.point = 0;
            }
        }

        sceneFadeInFadeOut.LoadNextSceneWithFadeOut();
    }

}
