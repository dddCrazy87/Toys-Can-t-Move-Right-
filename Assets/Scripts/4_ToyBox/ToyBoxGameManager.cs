using UnityEngine;

public class ToyBoxGameManager : MonoBehaviour
{
    void Start()
    {
        FindFirstObjectByType<BgmPlayer>().ChangeBgm("Toybox");
        FindFirstObjectByType<GameStartCountDown>().CountDownAndStartGame(OnCountDownFinished);
    }

    void OnCountDownFinished()
    {
        FindFirstObjectByType<GameManager>().StartGame();
        FindFirstObjectByType<PlayerPointUiManager>().InitialPlayerPointUi();
    }
}
