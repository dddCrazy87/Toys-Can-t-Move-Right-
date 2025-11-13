using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public TextMeshProUGUI playerIDUI;
    public string playerName;
    public int playerIndex;
    public string playerColor;
    public void Initialize(string name, int id, string color)
    {
        // if (playerIDUI != null) playerIDUI.text = name;
        playerName = name;
        playerIndex = id;
        playerColor = color;
    }

    [Header("移動設置")]
    [Tooltip("玩家移動速度")]
    public float moveSpeed = 5f;

    [Tooltip("玩家旋轉速度")]
    public float rotateSpeed = 15f;

    private Rigidbody rb;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    [HideInInspector] public bool avilibleMovement = true;
    private Vector3 movement;
    private Vector3 networkMovement;
    private Vector3 smoothedMovement;

    public void SetNetworkInput(float x, float y)
    {
        networkMovement = new Vector3(x, 0f, y).normalized;
    }

    void Update()
    {
        if (!avilibleMovement) return;
        movement = networkMovement;
    }

    void FixedUpdate()
    {
        if (!avilibleMovement || movement == Vector3.zero) return;
        if (rb.IsSleeping()) rb.WakeUp();

        smoothedMovement = Vector3.Lerp(smoothedMovement, movement, 0.3f);

        if (smoothedMovement.sqrMagnitude > 0.001f)
        {
            Vector3 targetPos = rb.position + smoothedMovement * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(targetPos);

            Quaternion targetRot = Quaternion.LookRotation(smoothedMovement);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotateSpeed * Time.fixedDeltaTime));
        }
    }

    public void ForceStopMotion()
    {
        // 清空速度與角速度，確保之後能穩定移動
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.Sleep();
        rb.WakeUp();
    }


    [Header("道具跟隨點（掛在玩家身上的一個子物件）")]
    public Transform followPoint;

    [Header("被搶奪後的免疫時間")]
    public float immunityStolenDuration = 0.3f;
    [HideInInspector] public bool isImmuneStolen = false;

    private Coroutine immunityRoutine;

    // 玩家碰到道具
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Collectable"))
        {
            ItemManager.Instance.RequestCollect(transform, other.transform);
        }
    }

    // 被搶成功時，ItemManager 會叫這個
    public void ActivateImmunityStolen()
    {
        if (isImmuneStolen) return;

        isImmuneStolen = true;

        if (immunityRoutine != null)
            StopCoroutine(immunityRoutine);

        immunityRoutine = StartCoroutine(ImmunityCountdown());
    }

    private IEnumerator ImmunityCountdown()
    {
        yield return new WaitForSeconds(immunityStolenDuration);
        isImmuneStolen = false;
        immunityRoutine = null;
    }

    public void CompleteItemCollection()
    {
        var items = ItemManager.Instance.TakeAllItemsFromPlayer(transform);

        int score = items.Count;
        FindFirstObjectByType<GameManager>().IncreasePlayerPoint(playerIndex, score);
        FindFirstObjectByType<PlayerPointUiManager>().UpdatePlayerPointUi(playerIndex);
        FindFirstObjectByType<GameSoundEffect>().PlayGetPointSound();

        ItemSpawner spawner = FindFirstObjectByType<ItemSpawner>();
        foreach (var t in items)
        {
            if (spawner != null)
                spawner.ItemCollected(t.gameObject);

            Destroy(t.gameObject);
        }
    }
}
