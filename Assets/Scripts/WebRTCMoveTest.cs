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
    public float timeoutSeconds = 0.3f;
    public string localPeerId = "";
    private float lastReceiveTime = -999f;
    private Vector2 inputVector;

    void Start()
    {
        WebRTCManager.OnDataMessageReceived_Static += OnDataReceived;
    }

    void OnDestroy()
    {
        WebRTCManager.OnDataMessageReceived_Static -= OnDataReceived;
    }

    private void OnDataReceived(string message, string id)
    {
        if (id != localPeerId) return;
        try
        {
            MoveMessage msg = JsonUtility.FromJson<MoveMessage>(message);
            if (msg.type == "move")
            {
                inputVector = msg.vector;
                lastReceiveTime = Time.time;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("解析向量訊息失敗: " + ex.Message);
        }
    }

    void Update()
    {
        if (Time.time - lastReceiveTime > timeoutSeconds)
        {
            inputVector = Vector2.zero;
        }
        Vector3 move = new Vector3(inputVector.x, 0, inputVector.y);
        transform.Translate(move * moveSpeed * Time.deltaTime, Space.World);
    }
}