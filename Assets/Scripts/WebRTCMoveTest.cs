using UnityEngine;
using System;
using SimpleWebRTC;

[Serializable]
public class MoveMessage
{
    public string type;
    public Vector2 vector;
}


public class WebRTCMoveTest : MonoBehaviour
{
    public float moveSpeed = 3f;
    private Vector2 inputVector;

    void Start()
    {
        WebRTCManager.OnDataMessageReceived_Static += OnDataReceived;
    }

    void OnDestroy()
    {
        WebRTCManager.OnDataMessageReceived_Static -= OnDataReceived;
    }

    private void OnDataReceived(string message)
    {
        try
        {
            MoveMessage msg = JsonUtility.FromJson<MoveMessage>(message);
            if (msg.type == "move")
            {
                inputVector = msg.vector;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("解析向量訊息失敗: " + ex.Message);
        }
    }

    void Update()
    {
        Vector3 move = new Vector3(inputVector.x, 0, inputVector.y);
        transform.Translate(move * moveSpeed * Time.deltaTime, Space.World);
    }
}