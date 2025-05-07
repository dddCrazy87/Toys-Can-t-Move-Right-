using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    void Start()
    {
        List<Player> playersInfo = new List<Player>
        {
            new Player { name = "yellowP", skin = "yellow" },
            new Player { name = "blueP",   skin = "blue" },
            new Player { name = "redP",    skin = "red" },
            new Player { name = "greenP",  skin = "green" },
        };

        // 傳資料到 GameManager 後才會開始遊戲
        FindFirstObjectByType<GameManager>().StartGame(playersInfo);
    }
}
