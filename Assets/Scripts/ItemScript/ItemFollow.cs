using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ItemFollow : MonoBehaviour
{
    public Transform follow;
    public float maxDistance = 1.2f;
    public float moveSpeed = 15f;
    public float rotateSpeed = 20f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        if (follow == null)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 pos = transform.position;
        Vector3 target = follow.position;

        Vector3 offset = pos - target;
        float dist = offset.magnitude;

        Vector3 movement = Vector3.zero;

        if (dist > maxDistance)
        {
            Vector3 pullDir = offset.normalized;

            Vector3 desiredPosition = target + pullDir * maxDistance;

            Vector3 moveDir = (desiredPosition - pos);
            movement = moveDir.normalized;
            rb.linearVelocity = movement * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector3.zero;
        }

        Vector3 dir = (target - pos);
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
            rb.MoveRotation(
                Quaternion.Slerp(rb.rotation, targetRot, rotateSpeed * Time.fixedDeltaTime)
            );
        }
    }
}
