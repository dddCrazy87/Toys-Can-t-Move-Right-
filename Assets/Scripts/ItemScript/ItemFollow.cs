using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ItemFollow : MonoBehaviour
{
    public Transform follow;          // 跟隨的目標
    public float maxDistance = 1.2f;  // 繩子最大距離
    public float followSpeed = 15f;   // 旋轉速度

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        // ---------------------------
        // follow 為 null → 不動 → 不吸到 (0,0,0)
        // ---------------------------
        if (follow == null)
            return;

        float currentDistance = Vector3.Distance(transform.position, follow.position);

        // 被繩子拉住 → 不會穿牆
        if (currentDistance > maxDistance)
        {
            Vector3 dir = (transform.position - follow.position).normalized;
            Vector3 targetPos = follow.position + dir * maxDistance;

            rb.MovePosition(targetPos);
        }

        // 平滑旋轉
        Vector3 targetDir = follow.position - transform.position;
        targetDir.y = 0f;
        targetDir.Normalize();

        float step = followSpeed * Time.fixedDeltaTime;
        Quaternion newRot = Quaternion.LookRotation(
            Vector3.RotateTowards(transform.forward, targetDir, step, 0f)
        );

        rb.MoveRotation(newRot);
    }
}
