using UnityEngine;

/// <summary>
/// Demo 相機控制器
/// 按 F1 切換相機，WASD/QE 移動，滑鼠右鍵拖曳旋轉
/// </summary>
public class DemoCameraController : MonoBehaviour
{
    [Header("相機設定")]
    [SerializeField] private Camera demoCamera;
    [SerializeField] private Camera gameCamera;

    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float fastMoveSpeed = 30f;
    [SerializeField] private float rotationSpeed = 3f;

    [Header("快捷鍵")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;           // Tab 切換相機
    [SerializeField] private KeyCode resetKey = KeyCode.BackQuote;      // ` 重置位置（數字1左邊）

    private bool isDemoCameraActive = false;
    private float rotationX = 0f;
    private float rotationY = 0f;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Awake()
    {
        // 如果沒有設定 demoCamera，嘗試用自己
        if (demoCamera == null)
        {
            demoCamera = GetComponent<Camera>();
        }

        // 立即停用 Demo 相機，確保不會搶走畫面
        if (demoCamera != null)
        {
            demoCamera.enabled = false;
        }
    }

    void Start()
    {
        // 如果沒有設定 gameCamera，嘗試找 Main Camera
        if (gameCamera == null)
        {
            gameCamera = Camera.main;
        }

        // 儲存初始位置
        if (demoCamera != null)
        {
            initialPosition = demoCamera.transform.position;
            initialRotation = demoCamera.transform.rotation;
            rotationX = initialRotation.eulerAngles.y;
            rotationY = initialRotation.eulerAngles.x;
        }

        // 初始狀態：Demo 相機關閉
        SetDemoCameraActive(false);
    }

    void Update()
    {
        // 偵測任意按鍵（Debug 用）
        if (Input.anyKeyDown)
        {
            Debug.Log($"[DemoCamera] 偵測到按鍵");
        }

        // Tab 切換相機
        if (Input.GetKeyDown(toggleKey))
        {
            Debug.Log($"[DemoCamera] 按下切換鍵！");
            ToggleCamera();
        }

        // F2 重置 Demo 相機位置
        if (Input.GetKeyDown(resetKey) && isDemoCameraActive)
        {
            ResetDemoCamera();
        }

        // Demo 相機控制
        if (isDemoCameraActive && demoCamera != null)
        {
            HandleMovement();
            HandleRotation();
        }
    }

    void ToggleCamera()
    {
        isDemoCameraActive = !isDemoCameraActive;
        SetDemoCameraActive(isDemoCameraActive);
        Debug.Log($"[DemoCamera] {(isDemoCameraActive ? "啟用 Demo 相機" : "切回遊戲相機")}");
    }

    void SetDemoCameraActive(bool active)
    {
        if (demoCamera != null)
        {
            demoCamera.enabled = active;
            // 設定 Demo 相機的 depth 比遊戲相機高
            demoCamera.depth = active ? 10 : -10;
        }

        if (gameCamera != null && gameCamera != demoCamera)
        {
            gameCamera.enabled = !active;
        }

        // 控制滑鼠鎖定
        if (active)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void HandleMovement()
    {
        float speed = Input.GetKey(KeyCode.LeftShift) ? fastMoveSpeed : moveSpeed;
        Vector3 movement = Vector3.zero;

        // WASD 前後左右
        if (Input.GetKey(KeyCode.W)) movement += demoCamera.transform.forward;
        if (Input.GetKey(KeyCode.S)) movement -= demoCamera.transform.forward;
        if (Input.GetKey(KeyCode.A)) movement -= demoCamera.transform.right;
        if (Input.GetKey(KeyCode.D)) movement += demoCamera.transform.right;

        // QE 上下
        if (Input.GetKey(KeyCode.E)) movement += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) movement -= Vector3.up;

        demoCamera.transform.position += movement.normalized * speed * Time.deltaTime;
    }

    void HandleRotation()
    {
        // 滑鼠右鍵拖曳旋轉
        if (Input.GetMouseButton(1))
        {
            rotationX += Input.GetAxis("Mouse X") * rotationSpeed;
            rotationY -= Input.GetAxis("Mouse Y") * rotationSpeed;
            rotationY = Mathf.Clamp(rotationY, -90f, 90f);

            demoCamera.transform.rotation = Quaternion.Euler(rotationY, rotationX, 0f);
        }
    }

    void ResetDemoCamera()
    {
        if (demoCamera != null)
        {
            demoCamera.transform.position = initialPosition;
            demoCamera.transform.rotation = initialRotation;
            rotationX = initialRotation.eulerAngles.y;
            rotationY = initialRotation.eulerAngles.x;
        }
        Debug.Log("[DemoCamera] 重置位置");
    }
}
