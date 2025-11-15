using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class WorldHintCanvasFloating : MonoBehaviour
{
    public float floatSpeed = 0.5f;  // 向上速度
    public float lifetime = 1f;    // 存活時間

    private float timer;

    RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        // 往上飄（世界座標）
        rectTransform.position += floatSpeed * Time.deltaTime * Vector3.up;

        // 計時刪除
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
