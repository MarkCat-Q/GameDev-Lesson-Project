using UnityEngine;
using System.Collections; // 必须引用这个，才能使用协程（用于倒计时）

public class PlatformerMovement : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 8f;
    public float jumpForce = 12f;
    public string attackTrigger = "Attack";

    [Header("二段跳设置")]
    public int maxJumps = 2;
    private int remainJumps;

    [Header("受伤/无敌设置")]
    public float invincibleTime = 1.5f; // 无敌持续时间
    private bool isInvincible = false;   // 是否处于无敌状态
    public string hurtTrigger = "Hurt";

    [Header("层级设置")]
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer; // 用于控制闪烁效果
    private Vector3 originalScale;
    private float speedMultiplier = 1f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>(); // 获取渲染组件
        originalScale = transform.localScale;
        remainJumps = maxJumps;
    }

    void Update()
    {
        // 1. 移动逻辑
        float horizontal = Input.GetAxis("Horizontal");
        rb.velocity = new Vector2(horizontal * moveSpeed * speedMultiplier, rb.velocity.y);

        // 2. 动画
        animator.SetFloat("Speed", Mathf.Abs(horizontal));

        // 3. 翻转
        if (horizontal > 0) transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        else if (horizontal < 0) transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);

        // 4. 接地与二段跳重置
        if (IsGroundedSimple() && rb.velocity.y <= 0.1f)
        {
            remainJumps = maxJumps;
        }

        // 5. 跳跃
        if (Input.GetKeyDown(KeyCode.Space) && remainJumps > 0)
        {
            Jump();
        }

        // 6. 攻击
        if (Input.GetKeyDown(KeyCode.J) || Input.GetButtonDown("Fire1"))
        {
            Attack();
        }

        MaintainOriginalScale();
    }

    public void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        animator.SetTrigger("Jump");
        remainJumps--;
    }

    public void Attack()
    {
        animator.SetTrigger(attackTrigger);
    }

    // --- 受伤逻辑处理 ---

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 接地判定
        if (collision.gameObject.CompareTag("Ground"))
        {
            // 这里可以保留，也可以通过射线处理
        }

        // 碰到敌人逻辑
        if (collision.gameObject.CompareTag("Enemy") && !isInvincible)
        {
            TakeDamage();
        }
    }

    public void TakeDamage()
    {
        if (isInvincible) return;

        // 1. 播放动画
        animator.SetTrigger(hurtTrigger);
        Debug.Log("玩家受伤！进入无敌状态");

        // 2. 击退效果（向后上方弹开）
        float knockbackDir = transform.localScale.x > 0 ? -1 : 1;
        rb.velocity = new Vector2(knockbackDir * 5f, 6f);

        // 3. 启动无敌协程（倒计时和闪烁）
        StartCoroutine(InvincibleRoutine());
    }

    // 协程：处理无敌时间和闪烁
    IEnumerator InvincibleRoutine()
    {
        isInvincible = true;

        // 闪烁效果（每0.1秒切换一次透明度）
        float timer = 0;
        while (timer < invincibleTime)
        {
            // 变半透明
            spriteRenderer.color = new Color(1, 1, 1, 0.5f);
            yield return new WaitForSeconds(0.1f);
            // 恢复不透明
            spriteRenderer.color = new Color(1, 1, 1, 1f);
            yield return new WaitForSeconds(0.1f);
            timer += 0.2f;
        }

        spriteRenderer.color = new Color(1, 1, 1, 1f); // 确保最后是完全显示的
        isInvincible = false;
        Debug.Log("无敌状态结束");
    }

    // --- 其他辅助函数 ---

    void MaintainOriginalScale()
    {
        if (transform.localScale.x != originalScale.x && transform.localScale.x != -originalScale.x)
        {
            float direction = Mathf.Sign(transform.localScale.x);
            transform.localScale = new Vector3(direction * Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
    }

    public bool IsGroundedSimple()
    {
        float rayLength = 0.6f;
        Vector2 rayStart = (Vector2)transform.position + Vector2.down * 0.4f;
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, rayLength, groundLayer);
        return hit.collider != null;
    }

    public void SetSpeedMultiplier(float multiplier) { speedMultiplier = multiplier; }
    public void ResetSpeedMultiplier() { speedMultiplier = 1f; }
}