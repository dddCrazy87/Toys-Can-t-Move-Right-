using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerBoundsLimiter : MonoBehaviour
{
    public Transform[] polygonPoints;
    private Rigidbody rb;

    // 用於記錄多邊形的中心點，作為往內推的參考方向
    private Vector3 polygonCenter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        GameObject mbObj = GameObject.Find("MapBound");
        if (mbObj != null)
        {
            Transform mb = mbObj.transform;
            polygonPoints = new Transform[mb.childCount];
            for (int i = 0; i < mb.childCount; i++)
            {
                polygonPoints[i] = mb.GetChild(i);
                polygonCenter += polygonPoints[i].position; // 累加座標
            }

            // 計算多邊形中心點
            if (mb.childCount > 0)
            {
                polygonCenter /= mb.childCount;
            }
        }
        else
        {
            polygonPoints = new Transform[0];
            Debug.LogWarning("[PlayerBoundsLimiter] 找不到 MapBound！");
        }
    }

    // 判斷是否在多邊形外 (保持不變)
    private bool IsOutsidePolygon(Vector3 pos)
    {
        int count = polygonPoints.Length;
        if (count < 3) return false;

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

        return (crossings % 2 == 0);
    }

    // 計算最近點 (保持不變)
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

    private Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ap = p - a;
        Vector3 ab = b - a;
        float t = Vector3.Dot(ap, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);
        return a + ab * t;
    }

    public void ClampPositionImmediately()
    {
        // 取得即將移動到的位置 (使用 rb.position 而不是 transform.position)
        Vector3 pos = rb.position;

        if (IsOutsidePolygon(pos))
        {
            Vector3 clamped = GetClosestPointOnPolygon(pos);
            clamped.y = pos.y;

            // 【關鍵修復 1】加入緩衝區：將角色稍微往地圖中心推一點點 (0.05單位)
            // 避免下一幀浮點數計算又判定在線上/線外
            Vector3 pushToCenterDir = (polygonCenter - clamped).normalized;
            pushToCenterDir.y = 0; // 確保只在水平推
            clamped += pushToCenterDir * 0.05f;

            // 【關鍵修復 2】統一使用 Rigidbody API，不要用 transform.position
            rb.position = clamped;

            // 【關鍵修復 3】移除暴力的動量清除！
            // 註解掉下面這兩行，避免角色碰到牆壁就失去所有速度跟擊退力
            // rb.linearVelocity = Vector3.zero;
            // rb.angularVelocity = Vector3.zero;
        }
    }
}