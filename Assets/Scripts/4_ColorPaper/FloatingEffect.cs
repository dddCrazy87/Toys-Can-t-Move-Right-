using UnityEngine;

public class FloatingEffect : MonoBehaviour
{
    [Header("飄浮設定")]
    [SerializeField] private float floatHeight = 0.3f;      // 上下飄動的幅度
    [SerializeField] private float floatSpeed = 2f;         // 飄動速度
    [SerializeField] private float heightOffset = 1.5f;     // 離角色頭頂的高度

    [Header("旋轉設定")]
    [SerializeField] private float rotateSpeed = 60f;       // 旋轉速度（度/秒）
    [SerializeField] private Vector3 rotateAxis = Vector3.up;  // 旋轉軸

    [Header("縮放動畫（出現/消失）")]
    [SerializeField] private float scaleInDuration = 0.3f;  // 出現動畫時間
    [SerializeField] private float targetScale = 1f;        // 目標大小

    private float timeOffset;
    private float currentScale = 0f;

    void Start()
    {
        // 隨機起始相位，多個橡皮擦不會同步飄動
        timeOffset = Random.Range(0f, Mathf.PI * 2f);

        // 設定初始位置（角色頭頂上方）
        transform.localPosition = new Vector3(0f, heightOffset, 0f);

        // 從 0 開始放大（出現動畫）
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        // 出現動畫（逐漸放大）
        if (currentScale < targetScale)
        {
            currentScale += Time.deltaTime / scaleInDuration * targetScale;
            currentScale = Mathf.Min(currentScale, targetScale);
            transform.localScale = Vector3.one * currentScale;
        }

        // 上下飄動
        float newY = heightOffset + Mathf.Sin((Time.time + timeOffset) * floatSpeed) * floatHeight;
        transform.localPosition = new Vector3(0f, newY, 0f);

        // 緩慢旋轉
        transform.Rotate(rotateAxis, rotateSpeed * Time.deltaTime, Space.Self);
    }
}
