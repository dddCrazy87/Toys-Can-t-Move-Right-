using UnityEngine;

public class ItemFollow : MonoBehaviour
{
    public float maxDistance = 2;
    public float followSpeed = 1.0f;
    public Transform follow = null;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (follow == null) return;

        float actualDistance = Vector3.Distance(transform.position, follow.position);
        if (actualDistance > maxDistance)
        {
            var followToCurrent = (transform.position - follow.position).normalized * maxDistance;
            Vector3 newPos = follow.position + followToCurrent;

            // 使用 MovePosition，確保不穿牆、平滑移動
            rb.MovePosition(newPos);
        }

        // 平滑朝向跟隨方向
        Vector3 targetDirection = follow.position - transform.position;
        targetDirection.y = 0f;
        targetDirection.Normalize();

        float singleStep = followSpeed * Time.fixedDeltaTime;
        Vector3 newDirection = Vector3.RotateTowards(transform.forward, targetDirection, singleStep, 0.0f);

        Quaternion targetRotation = Quaternion.LookRotation(newDirection);
        rb.MoveRotation(targetRotation);
    }
}
