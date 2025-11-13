using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerController))]
public class PlayerGamingTest : MonoBehaviour
{
    public int playerId = 0;
    PlayerController playerController;
    float moveSpeed = 10f;
    float rotateSpeed = 15f;
    Rigidbody rb;
    KeyCode[][] keyCodes = new KeyCode[2][];
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
        keyCodes[0] = new KeyCode[] { KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D };
        keyCodes[1] = new KeyCode[] { KeyCode.I, KeyCode.K, KeyCode.J, KeyCode.L };
    }

    private Vector3 movement;
    private Vector3 smoothedMovement;
    float x = 0, z = 0;
    void Update()
    {
        x = 0; z = 0;
        if (Input.GetKey(keyCodes[playerId][0])) z = 1;
        if (Input.GetKey(keyCodes[playerId][1])) z = -1;
        if (Input.GetKey(keyCodes[playerId][2])) x = -1;
        if (Input.GetKey(keyCodes[playerId][3])) x = 1;
        movement = new Vector3(x, 0, z);
    }

    void FixedUpdate()
    {
        if (!playerController.avilibleMovement || movement == Vector3.zero) return;
        if (rb.IsSleeping()) rb.WakeUp();

        smoothedMovement = Vector3.Lerp(smoothedMovement, movement, 0.3f);

        if (smoothedMovement.sqrMagnitude > 0.001f)
        {
            Vector3 targetPos = rb.position + smoothedMovement * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(targetPos);

            Quaternion targetRot = Quaternion.LookRotation(smoothedMovement);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotateSpeed * Time.fixedDeltaTime));
        }
    }
}
