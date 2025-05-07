using UnityEngine;

public class ItemController : MonoBehaviour
{
    public Material blue, red, green, yellow;
    private new Renderer renderer;
    void Start() {
        renderer = GetComponent<Renderer>();
    }
    public void ChangeMaterial(string color) {
        switch (color) {
            case "blue":
                renderer.material = blue;
                break;
            case "red":
                renderer.material = red;
                break;
            case "yellow":
                renderer.material = yellow;
                break;
            case "green":
                renderer.material = green;
                break;
            default:
                break;
        }
    }
}
