using UnityEngine;
using System;
using SimpleWebRTC;
using System.Collections.Generic; 


public class WebRTCMoveTest : MonoBehaviour
{
    [Header("Game Settings")]
    public float moveSpeed = 3f;
    public float timeoutSeconds = 0.3f;

    [Header("Character Settings")]

    public string characterToControl = "red";

    [Header("Network")]
    public WebRTCConnection webRTCConnection;

    private float lastReceiveTime = -999f;
    private Vector2 inputVector;

    private string controllingPeerId = null;

    private static string hostPeerId = null;
    private static WebRTCConnection staticWebRTCConnection = null;


    void Start()
    {
        if (webRTCConnection == null)
        {
            Debug.LogError("WebRTCMoveTest: WebRTCConnection 尚未設定！");
        }
        else if (staticWebRTCConnection == null)
        {
            staticWebRTCConnection = webRTCConnection;
        }
    }

    void OnDestroy()
    {
        WebRTCManager.OnDataMessageReceived_Static -= OnDataReceived;
    }

    private void OnDataReceived(string message, string senderPeerId)
    {
        try
        {
            BaseMessage data = JsonUtility.FromJson<BaseMessage>(message);

            if (data.type == "identify")
            {
                IdentityMessage identity = JsonUtility.FromJson<IdentityMessage>(message);

                if (hostPeerId == null)
                {
                    hostPeerId = senderPeerId;
                    Debug.Log($"{senderPeerId} ({identity.nickname}) is now the host.");

                    // 立刻廣播 Host 更新訊息
                    BroadcastHostUpdate();
                }

                if (identity.characterName == this.characterToControl)
                {
                    this.controllingPeerId = senderPeerId;
                    Debug.Log($"Received identify message from {senderPeerId} ({identity.nickname}) is now controlling {this.characterToControl}");
                }
            }

            else if (data.type == "move" || data.type == "manualMove")
            {
                if (senderPeerId == this.controllingPeerId)
                {
                    MoveMessage msg = JsonUtility.FromJson<MoveMessage>(message);
                    inputVector.x = msg.vector.x;
                    inputVector.y = msg.vector.y;
                    lastReceiveTime = Time.time;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"failed to parse message ({senderPeerId}): {ex.Message} - {message}");
        }
    }

    private void BroadcastHostUpdate()
    {
        if (hostPeerId == null || staticWebRTCConnection == null) return;

        HostUpdateMessage hostMessage = new HostUpdateMessage
        {
            type = "host_update",
            hostId = hostPeerId
        };
        string jsonMessage = JsonUtility.ToJson(hostMessage);

        staticWebRTCConnection.SendDataChannelMessage(jsonMessage);
        Debug.Log("廣播 Host 更新: " + jsonMessage);
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