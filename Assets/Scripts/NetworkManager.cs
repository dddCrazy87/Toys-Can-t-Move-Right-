using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    void Start()
    {
        // 模擬手動設定隊伍資訊
        List<Player> playersInfo = new List<Player>
        {
            new Player { name = "yellowP", skin = "yellow" },
            new Player { name = "blueP",   skin = "blue" },
            //new Player { name = "redP",    skin = "red" },
            //new Player { name = "greenP",  skin = "green" },
        };

        // 傳送資料給 GameManager
        GameManager.Instance.StartGame(playersInfo);
    }
}
