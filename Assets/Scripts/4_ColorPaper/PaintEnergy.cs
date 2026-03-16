using UnityEngine;
using System;

/// <summary>
/// 玩家顏料能量系統
/// 滿格 5 格，預設 3 格
/// 一格 = 5 秒繪製時間（每秒消耗 0.2 格）
/// </summary>
public class PaintEnergy : MonoBehaviour
{
    [Header("能量設定")]
    [SerializeField] private float maxEnergy = 5f;        // 最大格數
    [SerializeField] private float initialEnergy = 3f;    // 初始格數
    [SerializeField] private float consumeRate = 0.2f;    // 每秒消耗格數（1格/5秒 = 0.2格/秒）

    private float currentEnergy;
    private bool isInitialized = false;

    // 能量變化事件（供 UI 訂閱）
    public event Action<float, float> OnEnergyChanged;  // (當前能量, 最大能量)

    /// <summary>
    /// 初始化能量系統
    /// </summary>
    public void Initialize()
    {
        currentEnergy = initialEnergy;
        isInitialized = true;
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        Debug.Log($"[PaintEnergy] 初始化完成：{currentEnergy}/{maxEnergy}");
    }

    /// <summary>
    /// 初始化能量系統（自訂初始值）
    /// </summary>
    public void Initialize(float startEnergy)
    {
        currentEnergy = Mathf.Clamp(startEnergy, 0f, maxEnergy);
        isInitialized = true;
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    /// <summary>
    /// 是否有足夠能量繪製
    /// </summary>
    public bool HasEnergy()
    {
        return currentEnergy > 0f;
    }

    /// <summary>
    /// 能量是否已滿
    /// </summary>
    public bool IsFull()
    {
        return currentEnergy >= maxEnergy;
    }

    /// <summary>
    /// 消耗能量（每幀呼叫）
    /// </summary>
    public void ConsumeEnergy(float deltaTime)
    {
        if (!isInitialized || currentEnergy <= 0f) return;

        float consumed = consumeRate * deltaTime;
        currentEnergy = Mathf.Max(0f, currentEnergy - consumed);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    /// <summary>
    /// 補充能量
    /// </summary>
    public void AddEnergy(float amount)
    {
        if (!isInitialized) return;

        float oldEnergy = currentEnergy;
        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + amount);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        Debug.Log($"[PaintEnergy] 補充 {amount} 格：{oldEnergy:F2} -> {currentEnergy:F2}");
    }

    /// <summary>
    /// 取得當前能量
    /// </summary>
    public float GetCurrentEnergy() => currentEnergy;

    /// <summary>
    /// 取得最大能量
    /// </summary>
    public float GetMaxEnergy() => maxEnergy;

    /// <summary>
    /// 取得能量百分比 (0-1)
    /// </summary>
    public float GetEnergyPercent() => maxEnergy > 0 ? currentEnergy / maxEnergy : 0f;

    /// <summary>
    /// 取得整數格數（向下取整）
    /// </summary>
    public int GetEnergyBars() => Mathf.FloorToInt(currentEnergy);

    /// <summary>
    /// 取得當前格的填充比例 (0-1)
    /// </summary>
    public float GetCurrentBarFill() => currentEnergy - Mathf.Floor(currentEnergy);
}
