using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ItemFollow : MonoBehaviour
{
    [Tooltip("要跟隨的目標（玩家 followPoint 或前一顆道具的 FollowAnchor）")]
    public Transform follow;

    [Tooltip("最大可拉伸距離（你的 maxDistance）")]
    public float maxDistance = 1.2f;

    [Tooltip("旋轉速度（你的 followSpeed）")]
    public float followSpeed = 15f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        if (follow == null) return;

        float currentDistance = Vector3.Distance(transform.position, follow.position);

        // 超過距離 → 拉回到 maxDistance 範圍
        if (currentDistance > maxDistance)
        {
            Vector3 dir = (transform.position - follow.position).normalized;
            Vector3 targetPos = follow.position + dir * maxDistance;

            rb.MovePosition(targetPos);
        }

        // ---- 平滑旋轉朝向 follow ----
        Vector3 targetDir = follow.position - transform.position;
        targetDir.y = 0f;
        targetDir.Normalize();

        float step = followSpeed * Time.fixedDeltaTime;

        Vector3 newDir = Vector3.RotateTowards(
            transform.forward,
            targetDir,
            step,
            0f
        );

        Quaternion newRot = Quaternion.LookRotation(newDir);
        rb.MoveRotation(newRot);
    }
}
