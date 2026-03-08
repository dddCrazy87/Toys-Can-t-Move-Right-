using UnityEngine;

public class PatternManager : MonoBehaviour
{
    [Header("顯示設定")]
    [SerializeField] private MeshRenderer patternQuad;  // 用 Quad 顯示線稿

    [Header("底圖列表（放入所有可用的底圖）")]
    [SerializeField] private Texture2D[] patterns;

    [Header("測試用")]
    [SerializeField] private int forcePatternIndex = -1;  // -1 = 隨機, 0+ = 強制使用該索引

    private Material patternMaterial;
    private int currentPatternIndex = -1;

    void Awake()
    {
        if (patternQuad == null)
        {
            Debug.LogError("[PatternManager] 請設定 Pattern Quad！");
            return;
        }

        if (patterns == null || patterns.Length == 0)
        {
            Debug.LogWarning("[PatternManager] 沒有設定任何底圖！");
            return;
        }

        InitializeMaterial();
        SelectPattern();
    }

    /// <summary>
    /// 初始化材質
    /// </summary>
    void InitializeMaterial()
    {
        // 使用 Unlit 透明 Shader
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null)
        {
            // 備用：標準 Unlit
            unlitShader = Shader.Find("Unlit/Transparent");
        }

        if (unlitShader == null)
        {
            Debug.LogError("[PatternManager] 找不到 Unlit Shader！");
            return;
        }

        patternMaterial = new Material(unlitShader);

        // 設定透明模式
        patternMaterial.SetFloat("_Surface", 1);  // Transparent
        patternMaterial.SetFloat("_Blend", 0);    // Alpha blend
        patternMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        patternMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        patternMaterial.SetInt("_ZWrite", 0);
        patternMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        patternMaterial.renderQueue = 3000;

        patternQuad.material = patternMaterial;
    }

    /// <summary>
    /// 選擇底圖（隨機或強制）
    /// </summary>
    void SelectPattern()
    {
        if (patterns.Length == 0) return;

        if (forcePatternIndex >= 0 && forcePatternIndex < patterns.Length)
        {
            currentPatternIndex = forcePatternIndex;
        }
        else
        {
            currentPatternIndex = Random.Range(0, patterns.Length);
        }

        ApplyPattern(currentPatternIndex);
    }

    /// <summary>
    /// 套用指定索引的底圖
    /// </summary>
    void ApplyPattern(int index)
    {
        if (index < 0 || index >= patterns.Length) return;
        if (patterns[index] == null)
        {
            Debug.LogWarning($"[PatternManager] patterns[{index}] 是 null！");
            return;
        }

        // URP Unlit 使用 _BaseMap
        patternMaterial.SetTexture("_BaseMap", patterns[index]);
        Debug.Log($"[PatternManager] 套用底圖: {patterns[index].name}");
    }

    /// <summary>
    /// 取得目前底圖名稱
    /// </summary>
    public string GetCurrentPatternName()
    {
        if (currentPatternIndex >= 0 && currentPatternIndex < patterns.Length && patterns[currentPatternIndex] != null)
        {
            return patterns[currentPatternIndex].name;
        }
        return "無";
    }

    /// <summary>
    /// 重新隨機選擇底圖
    /// </summary>
    public void RandomizePattern()
    {
        forcePatternIndex = -1;
        currentPatternIndex = Random.Range(0, patterns.Length);
        ApplyPattern(currentPatternIndex);
    }

    /// <summary>
    /// 設定指定底圖
    /// </summary>
    public void SetPattern(int index)
    {
        if (index >= 0 && index < patterns.Length)
        {
            currentPatternIndex = index;
            ApplyPattern(index);
        }
    }

    void OnDestroy()
    {
        if (patternMaterial != null)
        {
            Destroy(patternMaterial);
        }
    }
}
