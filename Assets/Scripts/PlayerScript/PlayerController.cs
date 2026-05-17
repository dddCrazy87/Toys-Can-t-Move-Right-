using System;
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

    // 按壓狀態（用於 ColorPaper 場景的繪圖控制）
    private bool isPressing = false;
    public event Action<bool> OnPressStateChanged;

    // 取得目前按壓狀態
    public bool IsPressing() => isPressing;

    // 由手機 WebRTC 傳入（不需要按壓的場景）
    public void SetNetworkInput(float x, float y)
    {
        SetNetworkInput(x, y, false);
    }

    // 由手機 WebRTC 傳入（支援按壓狀態）
    public void SetNetworkInput(float x, float y, bool isPress)
    {
        if (isKnockback || isFrozen) return;

        Vector3 raw = new(x, 0f, y);

        if (raw.sqrMagnitude < 0.01f)
            networkMovement = Vector3.zero;
        else
            networkMovement = raw.normalized;

        // 按壓狀態變化時觸發事件
        if (isPressing != isPress)
        {
            isPressing = isPress;
            OnPressStateChanged?.Invoke(isPressing);
        }
    }

    void Update()
    {
        if (isKnockback || isFrozen) return;
        movement = networkMovement;
    }

    void FixedUpdate()
    {
        if (isKnockback || isFrozen) return;

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

        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        rb.angularVelocity = Vector3.zero;
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

    [Header("玩家互撞設定")]
    [Tooltip("啟用玩家互相碰撞彈開")]
    public bool enablePlayerCollision = false;  // 預設關閉，由各場景的 GameManager 啟用
    [Tooltip("玩家碰撞後的彈開力道")]
    public float playerBounceForce = 80f;
    [Tooltip("碰撞後的免疫時間（防止連續碰撞）")]
    public float collisionCooldown = 0.5f;
    private bool isCollisionCooldown = false;

    [Tooltip("冰凍特效")]
    [SerializeField] private GameObject iceEffect;

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



    [HideInInspector] public bool isFrozen = false; // 是否處於冰凍狀態

    // ++ 新增：供外部呼叫的冰凍方法 ++
    public void StartFrozen(float duration)
    {
        StartCoroutine(FrozenRoutine(duration));
    }

    private IEnumerator FrozenRoutine(float duration)
    {
        isFrozen = true;
        ForceStopMotion(); // 立即停止當前所有移動
        iceEffect.SetActive(true);

        yield return new WaitForSeconds(duration);


        iceEffect.SetActive(false);
        isFrozen = false;
    }

}
