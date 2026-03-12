using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerBoundsLimiter))]
public class PlayerController : MonoBehaviour
{
    public TextMeshProUGUI playerIDUI;
    [HideInInspector] public string playerName;
    [HideInInspector] public int playerIndex;
    [HideInInspector] public string playerColor;

    public void Initialize(string name, int id, string color)
    {
        playerName = name;
        playerIndex = id;
        playerColor = color;
    }

    [Header("基礎玩家數值")]
    public float moveSpeed = 10f;
    public float bounceForce = 100f;
    public float bounceDuration = 0.5f;
    public float knockbackSpinSpeed = 1.5f;
    [Tooltip("1~10")]
    public int stealSkill = 5;
    [Tooltip("1~10")]
    public int defenceWeakness = 5;
    [HideInInspector] public float rotateSpeed = 15f;

    [Header("筆刷設定")]
    [Tooltip("角色專屬筆刷貼圖（在 Prefab 設定）")]
    public Texture2D brushTexture;


    private Rigidbody rb;
    private PlayerBoundsLimiter boundsLimiter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        boundsLimiter = GetComponent<PlayerBoundsLimiter>();
    }

    [HideInInspector] public bool isKnockback = false;
    [HideInInspector] public Coroutine knockbackCoroutine;

    private Vector3 networkMovement;
    private Vector3 movement;

    // 由手機 WebRTC 傳入
    public void SetNetworkInput(float x, float y)
    {
        if (isKnockback) return;

        Vector3 raw = new(x, 0f, y);

        if (raw.sqrMagnitude < 0.01f)
            networkMovement = Vector3.zero;
        else
            networkMovement = raw.normalized;
    }

    void Update()
    {
        if (isKnockback) return;
        movement = networkMovement;
    }

    void FixedUpdate()
    {
        if (isKnockback) return;

        if (movement.sqrMagnitude > 0.001f)
        {
            Vector3 newPos = rb.position + moveSpeed * Time.fixedDeltaTime * movement;
            rb.MovePosition(newPos);
            boundsLimiter.ClampPositionImmediately();

            Quaternion targetRot = Quaternion.LookRotation(movement);
            rb.MoveRotation(
                Quaternion.Slerp(rb.rotation, targetRot, rotateSpeed * Time.fixedDeltaTime)
            );
        }
    }

    public void StartKnockback(Vector3 direction)
    {
        if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
        knockbackCoroutine = StartCoroutine(KnockbackRoutine(direction));
    }

    private IEnumerator KnockbackRoutine(Vector3 direction)
    {
        isKnockback = true;

        Vector3 knockbackVelocity = direction.normalized * bounceForce;
        rb.linearVelocity = knockbackVelocity;

        float timer = 0f;

        while (timer < bounceDuration)
        {
            timer += Time.deltaTime;

            rb.linearVelocity = knockbackVelocity;

            Quaternion spin = Quaternion.Euler(0f, knockbackSpinSpeed * Time.deltaTime, 0f);
            rb.MoveRotation(rb.rotation * spin);
            boundsLimiter.ClampPositionImmediately();

            yield return null;
        }

        ForceStopMotion();
        isKnockback = false;
        knockbackCoroutine = null;
    }



    // 停止所有動量
    public void ForceStopMotion()
    {
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

    [Header("玩家互撞設定")]
    [Tooltip("啟用玩家互相碰撞彈開")]
    public bool enablePlayerCollision = false;  // 預設關閉，由各場景的 GameManager 啟用
    [Tooltip("玩家碰撞後的彈開力道")]
    public float playerBounceForce = 80f;
    [Tooltip("碰撞後的免疫時間（防止連續碰撞）")]
    public float collisionCooldown = 0.5f;
    private bool isCollisionCooldown = false;

    // 玩家碰到道具
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Collectable"))
        {
            ItemManager.Instance.RequestCollect(transform, other.transform, stealSkill);
        }
    }

    // 玩家互相碰撞
    private void OnCollisionEnter(Collision collision)
    {
        if (!enablePlayerCollision) return;
        if (isCollisionCooldown) return;

        PlayerController otherPlayer = collision.gameObject.GetComponent<PlayerController>();
        if (otherPlayer == null) return;

        // 計算碰撞方向（從對方指向自己）
        Vector3 bounceDirection = (transform.position - otherPlayer.transform.position).normalized;
        bounceDirection.y = 0f;  // 只在水平面彈開

        // 播放碰撞音效（只有一方播放，避免重複）
        if (playerIndex < otherPlayer.playerIndex)
        {
            GameSoundEffect sfx = FindFirstObjectByType<GameSoundEffect>();
            if (sfx != null) sfx.PlayPlayerCollisionSound();
        }

        // 對自己施加彈開
        StartPlayerBounce(bounceDirection);
    }

    private void StartPlayerBounce(Vector3 direction)
    {
        if (isCollisionCooldown) return;

        // 啟動碰撞冷卻
        StartCoroutine(CollisionCooldownRoutine());

        // 使用現有的 knockback 系統，但用玩家碰撞專屬的力道
        if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
        knockbackCoroutine = StartCoroutine(PlayerBounceRoutine(direction));
    }

    private IEnumerator PlayerBounceRoutine(Vector3 direction)
    {
        isKnockback = true;

        Vector3 knockbackVelocity = direction.normalized * playerBounceForce;
        rb.linearVelocity = knockbackVelocity;

        float timer = 0f;
        float duration = bounceDuration * 0.6f;  // 玩家碰撞的彈開時間較短

        while (timer < duration)
        {
            timer += Time.deltaTime;
            rb.linearVelocity = knockbackVelocity * (1f - timer / duration);  // 逐漸減速
            boundsLimiter.ClampPositionImmediately();
            yield return null;
        }

        ForceStopMotion();
        isKnockback = false;
        knockbackCoroutine = null;
    }

    private IEnumerator CollisionCooldownRoutine()
    {
        isCollisionCooldown = true;
        yield return new WaitForSeconds(collisionCooldown);
        isCollisionCooldown = false;
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
        if (score <= 0) return;
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
