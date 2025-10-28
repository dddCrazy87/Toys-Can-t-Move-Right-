using UnityEngine;

public class ItemController : MonoBehaviour
{
    public Material blue, red, green, yellow;
    private new Renderer renderer;
    void Start()
    {
        renderer = GetComponent<Renderer>();
    }
    public void ChangeMaterial(string color)
    {
        renderer.material = color switch
        {
            "blue" => blue,
            "red" => red,
            "yellow" => yellow,
            "green" => green,
            _ => renderer.material
        };
    }
}
