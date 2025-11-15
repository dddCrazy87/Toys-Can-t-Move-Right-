using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ItemBoundsLimiter : MonoBehaviour
{
    public Transform[] polygonPoints;   // 五邊形的點，按順序
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // 自動抓取 MapBound 的五個點
        Transform mb = GameObject.Find("MapBound").transform;
        polygonPoints = new Transform[mb.childCount];
        for (int i = 0; i < mb.childCount; i++)
            polygonPoints[i] = mb.GetChild(i);
    }

    private void FixedUpdate()
    {
        Vector3 pos = transform.position;

        // 如果在外面 → 投影到最近邊界
        if (IsOutsidePolygon(pos))
        {
            Vector3 clamped = GetClosestPointOnPolygon(pos);

            // 強制造成"黏著牆壁"效果（永遠不可能穿出去）
            transform.position = clamped;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // ======================
    // 判斷是否在多邊形外
    // ======================
    private bool IsOutsidePolygon(Vector3 pos)
    {
        int count = polygonPoints.Length;
        int crossings = 0;

        Vector2 p = new Vector2(pos.x, pos.z);

        for (int i = 0; i < count; i++)
        {
            Vector2 a = new Vector2(polygonPoints[i].position.x, polygonPoints[i].position.z);
            Vector2 b = new Vector2(polygonPoints[(i + 1) % count].position.x, polygonPoints[(i + 1) % count].position.z);

            if (((a.y > p.y) != (b.y > p.y)) &&
                (p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x))
                crossings++;
        }

        return (crossings % 2 == 0); // 偶數 → 外面
    }

    // ======================
    // 計算位置被 clamp 在多邊形邊上
    // ======================
    private Vector3 GetClosestPointOnPolygon(Vector3 pos)
    {
        Vector3 bestPoint = Vector3.zero;
        float bestDist = Mathf.Infinity;

        for (int i = 0; i < polygonPoints.Length; i++)
        {
            Vector3 a = polygonPoints[i].position;
            Vector3 b = polygonPoints[(i + 1) % polygonPoints.Length].position;

            Vector3 candidate = ClosestPointOnSegment(a, b, pos);
            float dist = (candidate - pos).sqrMagnitude;

            if (dist < bestDist)
            {
                bestDist = dist;
                bestPoint = candidate;
            }
        }

        return bestPoint;
    }

    // 線段最近點
    private Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ap = p - a;
        Vector3 ab = b - a;
        float t = Vector3.Dot(ap, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);
        return a + ab * t;
    }
}
