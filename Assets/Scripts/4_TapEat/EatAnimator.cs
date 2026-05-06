using System.Collections;
using UnityEngine;

/// <summary>
/// 吃東西動畫：角色往前撲（旋轉 + 位移），然後彈回來
/// 角色的中心點在正前方，所以用旋轉讓他「低頭吃」
/// </summary>
public class EatAnimator : MonoBehaviour
{
    [Header("動畫參數")]
    public float dipAngle = -25f;        // 往後低頭的角度（負值 = 朝食物方向撲）
    public float dipDistance = 0.15f;    // 往食物方向移動的距離
    public float dipDuration = 0.08f;   // 低頭時間
    public float returnDuration = 0.15f; // 回彈時間

    private Quaternion originalRotation;
    private Vector3 originalPosition;
    private Coroutine currentAnim;
    private bool isInitialized = false;

    void Start()
    {
        originalRotation = transform.localRotation;
        originalPosition = transform.localPosition;
        isInitialized = true;
    }

    /// <summary>
    /// 播放吃一口的動畫
    /// </summary>
    public void PlayEat()
    {
        if (!isInitialized)
        {
            originalRotation = transform.localRotation;
            originalPosition = transform.localPosition;
            isInitialized = true;
        }

        if (currentAnim != null)
        {
            StopCoroutine(currentAnim);
            // 立刻回到原位再開始新動畫
            transform.localRotation = originalRotation;
            transform.localPosition = originalPosition;
        }
        currentAnim = StartCoroutine(EatRoutine());
    }

    private IEnumerator EatRoutine()
    {
        // 往前撲：旋轉 + 位移
        Quaternion dipRotation = originalRotation * Quaternion.Euler(dipAngle, 0f, 0f);
        // 角色已旋轉 180 度，forward 指向相機，所以用 -forward 指向食物
        Vector3 dipPosition = originalPosition - transform.forward * dipDistance;

        // 快速低頭
        float elapsed = 0f;
        while (elapsed < dipDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dipDuration;
            // 加速曲線（快進）
            float ease = t * t;
            transform.localRotation = Quaternion.Slerp(originalRotation, dipRotation, ease);
            transform.localPosition = Vector3.Lerp(originalPosition, dipPosition, ease);
            yield return null;
        }

        transform.localRotation = dipRotation;
        transform.localPosition = dipPosition;

        // 彈回來（帶彈性）
        elapsed = 0f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;
            // 彈性曲線（overshoot 再回來）
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            // 輕微 overshoot
            float overshoot = 1f + Mathf.Sin(t * Mathf.PI) * 0.15f;
            transform.localRotation = Quaternion.Slerp(dipRotation, originalRotation, ease * overshoot);
            transform.localPosition = Vector3.Lerp(dipPosition, originalPosition, ease);
            yield return null;
        }

        transform.localRotation = originalRotation;
        transform.localPosition = originalPosition;
        currentAnim = null;
    }
}
