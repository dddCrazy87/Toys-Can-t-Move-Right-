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
    Rigidbody rb;
    PlayerBoundsLimiter boundsLimiter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        boundsLimiter = GetComponent<PlayerBoundsLimiter>();
    }
    KeyCode[][] keyCodes = new KeyCode[2][];

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
        boundsLimiter = GetComponent<PlayerBoundsLimiter>();

        keyCodes[0] = new KeyCode[] { KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D };
        keyCodes[1] = new KeyCode[] { KeyCode.I, KeyCode.K, KeyCode.J, KeyCode.L };
    }

    Vector3 movement;
    float x = 0, z = 0;

    void Update()
    {
        x = 0; z = 0;

        if (Input.GetKey(keyCodes[playerId][0])) z = 1;
        if (Input.GetKey(keyCodes[playerId][1])) z = -1;
        if (Input.GetKey(keyCodes[playerId][2])) x = -1;
        if (Input.GetKey(keyCodes[playerId][3])) x = 1;

        movement = new Vector3(x, 0, z).normalized;
    }

    void FixedUpdate()
    {
        if (playerController.isKnockback) return;

        if (movement.sqrMagnitude > 0.001f)
        {
            Vector3 newPos = rb.position + movement * playerController.moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPos);

            Quaternion targetRot = Quaternion.LookRotation(movement);
            rb.MoveRotation(
                Quaternion.Slerp(rb.rotation, targetRot, playerController.rotateSpeed * Time.fixedDeltaTime)
            );
        }
    }
}
