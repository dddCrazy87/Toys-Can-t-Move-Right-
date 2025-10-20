using UnityEngine;

public class ItemFollow : MonoBehaviour
{
    public float maxDistance = 2;
    public float followSpeed = 1.0f;
    public Transform follow = null;

    private Rigidbody rb;
    private void Awake() {
        rb = GetComponent<Rigidbody>();
    }

    void Update() {
        if (follow == null) return;
        // change this object position only if the distance is greater than maxDistance
        float actualDistance = Vector3.Distance(transform.position, follow.position);
        if (actualDistance > maxDistance)
        {
            var followToCurrent = (transform.position - follow.position).normalized;
            followToCurrent.Scale(new Vector3(maxDistance, maxDistance, maxDistance));

            // set the new position
            Vector3 newPos = follow.position + followToCurrent;
            rb.MovePosition(newPos);    
        }

        // Rotate..
        Vector3 targetDirection = follow.position - transform.position;

        // 把 Y 分量歸零，這樣只在水平面轉動
        targetDirection.y = 0f;
        targetDirection.Normalize();

        float singleStep = followSpeed * Time.deltaTime;
        Vector3 newDirection = Vector3.RotateTowards(transform.forward, targetDirection, singleStep, 0.0f);

        // 只計算水平方向旋轉
        Quaternion targetRotation = Quaternion.LookRotation(newDirection);

        // 限制 rotation 只改變 Y 軸（其實因為 targetDirection.y = 0，結果也只會轉 Y 軸）
        rb.MoveRotation(targetRotation);

    }
}
