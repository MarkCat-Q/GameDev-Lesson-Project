using UnityEngine;

public class PlatformerMovement : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 8f;
    public float jumpForce = 12f;

    [Header("组件引用")]
    private Rigidbody2D rb;
    private Animator animator;

    private bool isGrounded = false;
    private Vector3 originalScale; // 关键：存储原始大小

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        originalScale = transform.localScale; // 保存初始大小
    }

    void Update()
    {
        // 水平移动
        float horizontal = Input.GetAxis("Horizontal");
        rb.velocity = new Vector2(horizontal * moveSpeed, rb.velocity.y);

        // 动画控制
        animator.SetFloat("Speed", Mathf.Abs(horizontal));

        // 角色朝向 - 关键：使用原始大小，只修改X轴方向
        if (horizontal > 0) // 向右移动
        {
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
        else if (horizontal < 0) // 向左移动
        {
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }

        // 空格键跳跃检测
        if (Input.GetKeyDown(KeyCode.Space) && IsGroundedSimple())
        {
            Jump();
        }

        // J键攻击检测
        if (Input.GetKeyDown(KeyCode.J))
        {
            Attack();
        }

        // 每帧强制保持原始大小（双重保险）
        MaintainOriginalScale();
    }

    void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        animator.SetTrigger("Jump");
        Debug.Log("执行跳跃！");
    }

    void Attack()
    {
        animator.SetTrigger("Attack");
        Debug.Log("执行攻击！");
    }

    // 强制保持原始大小
    void MaintainOriginalScale()
    {
        if (transform.localScale.x != originalScale.x && transform.localScale.x != -originalScale.x)
        {
            // 如果X轴大小被修改，强制恢复
            float direction = Mathf.Sign(transform.localScale.x);
            transform.localScale = new Vector3(direction * Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
        if (transform.localScale.y != originalScale.y)
        {
            // 如果Y轴大小被修改，强制恢复
            transform.localScale = new Vector3(transform.localScale.x, originalScale.y, originalScale.z);
        }
    }

    // 简单的着地检测
    bool IsGroundedSimple()
    {
        // 方法1：射线检测
        float rayLength = 0.6f; // 检测距离
        Vector2 rayStart = (Vector2)transform.position + Vector2.down * 0.5f;
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, rayLength);

        // 可视化射线（调试用）
        Debug.DrawRay(rayStart, Vector2.down * rayLength, hit.collider != null ? Color.green : Color.red);

        return hit.collider != null;
    }

    // 碰撞检测作为备用
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}