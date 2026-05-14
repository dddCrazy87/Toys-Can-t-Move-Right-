using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerBoundsLimiter))]
public class PlayerGamingTest : MonoBehaviour
{
    public int playerId = 0;

    PlayerController playerController;

    // 定義兩組玩家的移動按鍵
    KeyCode[][] keyCodes = new KeyCode[2][];

    void Start()
    {
        playerController = GetComponent<PlayerController>();

        // 玩家 0: 上, 下, 左, 右 (WASD)
        keyCodes[0] = new KeyCode[] { KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D };

        // 玩家 1: 上, 下, 左, 右 (IKJL)
        keyCodes[1] = new KeyCode[] { KeyCode.I, KeyCode.K, KeyCode.J, KeyCode.L };
    }

    void Update()
    {
        if (playerController == null) return;

        float x = 0, z = 0;

        // 偵測移動輸入
        if (Input.GetKey(keyCodes[playerId][0])) z = 1;
        if (Input.GetKey(keyCodes[playerId][1])) z = -1;
        if (Input.GetKey(keyCodes[playerId][2])) x = -1;
        if (Input.GetKey(keyCodes[playerId][3])) x = 1;

        // 偵測技能/畫畫按鈕 (模擬手機端按壓)
        bool isPress = false;
        if (playerId == 0 && Input.GetKey(KeyCode.Space)) isPress = true;
        if (playerId == 1 && Input.GetKey(KeyCode.RightShift)) isPress = true;

        // 將所有輸入統一交給 PlayerController 處理
        // 這樣做才能完全模擬真實手機連線時的狀態，並觸發 OnPressStateChanged 事件
        playerController.SetNetworkInput(x, z, isPress);
    }

    // 💡 備註：原本這裡的 FixedUpdate 被移除了。
    // 因為現在輸入已經交給 SetNetworkInput，PlayerController 本身就會在它的 FixedUpdate 幫你處理好物理移動跟限制邊界了！
}