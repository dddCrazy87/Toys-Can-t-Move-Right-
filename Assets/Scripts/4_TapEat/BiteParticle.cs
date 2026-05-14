using UnityEngine;

/// <summary>
/// 吃東西碎屑粒子效果
/// 掛在 Food 物件上，或由 TapEatGameManager 動態加上
/// </summary>
public class BiteParticle : MonoBehaviour
{
    [Header("粒子設定")]
    public Color[] particleColors = {
        new Color(0.96f, 0.87f, 0.70f),  // 麵包色
        new Color(0.91f, 0.59f, 0.48f),  // 肉色
        new Color(0.80f, 0.86f, 0.22f),  // 生菜色
        new Color(1f, 0.84f, 0f),         // 起司色
    };
    public int particleCount = 8;          // 每次噴出幾個碎屑
    public float particleSpeed = 2f;       // 碎屑飛出速度
    public float particleSize = 0.08f;     // 碎屑大小
    public float particleLifetime = 0.6f;  // 碎屑存活時間

    private ParticleSystem ps;

    void Start()
    {
        SetupParticleSystem();
    }

    void SetupParticleSystem()
    {
        // 建立 ParticleSystem
        ps = gameObject.AddComponent<ParticleSystem>();

        // 停止自動播放
        var main = ps.main;
        main.playOnAwake = false;
        main.startLifetime = particleLifetime;
        main.startSpeed = particleSpeed;
        main.startSize = particleSize;
        main.gravityModifier = 1.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 50;

        // 形狀：從中心往四周噴
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        // 發射：每次手動觸發，不連續發射
        var emission = ps.emission;
        emission.rateOverTime = 0;

        // 顏色：隨機從 particleColors 中選
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        // 用 startColor 隨機顏色
        var mainModule = ps.main;
        if (particleColors.Length > 0)
        {
            mainModule.startColor = new ParticleSystem.MinMaxGradient(
                particleColors[0],
                particleColors[particleColors.Length - 1]
            );
        }

        // 大小隨時間縮小
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0.2f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // 渲染設定
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        // 用預設粒子材質
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.color = Color.white;
    }

    /// <summary>
    /// 噴出碎屑
    /// </summary>
    public void Play()
    {
        if (ps != null)
        {
            ps.Emit(particleCount);
        }
    }

    /// <summary>
    /// 用指定顏色噴出碎屑（可配合食物顏色）
    /// </summary>
    public void PlayWithColor(Color color)
    {
        if (ps != null)
        {
            var emitParams = new ParticleSystem.EmitParams();
            for (int i = 0; i < particleCount; i++)
            {
                // 隨機偏移顏色讓碎屑更自然
                float r = Random.Range(-0.1f, 0.1f);
                emitParams.startColor = new Color(
                    Mathf.Clamp01(color.r + r),
                    Mathf.Clamp01(color.g + r),
                    Mathf.Clamp01(color.b + r),
                    1f
                );
                ps.Emit(emitParams, 1);
            }
        }
    }
}
