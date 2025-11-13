using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ItemController : MonoBehaviour
{
    private new Renderer renderer;

    [Header("道具顏色設定")]
    public Color redColor;
    public Color greenColor;
    public Color blueColor;
    public Color yellowColor;

    void Awake()
    {
        renderer = GetComponent<Renderer>();
    }

    public void ChangeColor(string colorString)
    {
        if (renderer == null)
            renderer = GetComponent<Renderer>();

        Color targetColor = colorString.ToLower() switch
        {
            "red" => redColor,
            "green" => greenColor,
            "blue" => blueColor,
            "yellow" => yellowColor,
            _ => Color.white
        };

        renderer.material.color = targetColor;
    }

    public void SetRandomColor()
    {
        string[] colors = { "red", "green", "yellow" };
        string randomColor = colors[Random.Range(0, colors.Length)];

        ChangeColor(randomColor);
    }
}
