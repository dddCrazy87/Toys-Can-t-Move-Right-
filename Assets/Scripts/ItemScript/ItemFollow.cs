using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ItemFollow : MonoBehaviour
{
    public Transform follow;
    public float maxDistance = 1.2f;
    public float followSpeed = 15f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        // ----------------------------------------
        // ❗ follow 不存在 → 直接 return
        // ----------------------------------------
        if (follow == null)
            return;

        float currentDistance = Vector3.Distance(transform.position, follow.position);

        if (currentDistance > maxDistance)
        {
            Vector3 dir = (transform.position - follow.position).normalized;
            Vector3 targetPos = follow.position + dir * maxDistance;

            rb.MovePosition(targetPos);
        }

        Vector3 targetDir = follow.position - transform.position;
        targetDir.y = 0f;
        targetDir.Normalize();

        float step = followSpeed * Time.fixedDeltaTime;
        Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, step, 0f);
        Quaternion newRot = Quaternion.LookRotation(newDir);

        rb.MoveRotation(newRot);
    }
}
