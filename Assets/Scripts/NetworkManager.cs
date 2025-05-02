using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    void Start()
    {
        // 模擬手動設定隊伍資訊
        List<Team> testTeams = new List<Team>
        {
            new Team { teamID = "TeamA", players = new List<string> { "Player1" } },
            // new Team { teamID = "TeamA", players = new List<string> { "Player1", "Player2", "Player1", "Player2" } },
            // new Team { teamID = "TeamB", players = new List<string> { "Player3", "Player4" } }
        };

        // 傳送資料給 GameManager
        GameManager.Instance.StartGame(testTeams);
    }
}
