using UnityEngine;

public class PlatformerMovement : MonoBehaviour
{
    [Header("�ƶ�����")]
    public float moveSpeed = 8f;
    public float jumpForce = 12f;

    [Header("�������")]
    private Rigidbody2D rb;
    private Animator animator;

    private bool isGrounded = false;
    private Vector3 originalScale;
    private float speedMultiplier = 1f; // 速度倍数，用于外部修改（如蜘蛛网减速） // �ؼ����洢ԭʼ��С

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        originalScale = transform.localScale; // �����ʼ��С
    }

    void Update()
    {
        // ˮƽ�ƶ�
        float horizontal = Input.GetAxis("Horizontal");
        rb.velocity = new Vector2(horizontal * moveSpeed * speedMultiplier, rb.velocity.y);

        // ��������
        animator.SetFloat("Speed", Mathf.Abs(horizontal));

        // ��ɫ���� - �ؼ���ʹ��ԭʼ��С��ֻ�޸�X�᷽��
        if (horizontal > 0) // �����ƶ�
        {
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
        else if (horizontal < 0) // �����ƶ�
        {
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }

        // �ո����Ծ���  
        // ��һ��bug����Ծû�м��isGround��Ҳ����˵�����ڿ���ʱ��Ȼ�ܹ���Ծ
        if (Input.GetKeyDown(KeyCode.Space) && IsGroundedSimple())
        {
            Jump();
        }

        // J���������
        if (Input.GetKeyDown(KeyCode.J))
        {
            Attack();
        }

        // ÿ֡ǿ�Ʊ���ԭʼ��С��˫�ر��գ�
        MaintainOriginalScale();
    }

    void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        animator.SetTrigger("Jump");
        Debug.Log("ִ����Ծ��");
    }

    void Attack()
    {
        animator.SetTrigger("Attack");
        Debug.Log("ִ�й�����");
    }

    // ǿ�Ʊ���ԭʼ��С
    void MaintainOriginalScale()
    {
        if (transform.localScale.x != originalScale.x && transform.localScale.x != -originalScale.x)
        {
            // ���X���С���޸ģ�ǿ�ƻָ�
            float direction = Mathf.Sign(transform.localScale.x);
            transform.localScale = new Vector3(direction * Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
        if (transform.localScale.y != originalScale.y)
        {
            // ���Y���С���޸ģ�ǿ�ƻָ�
            transform.localScale = new Vector3(transform.localScale.x, originalScale.y, originalScale.z);
        }
    }

    // �򵥵��ŵؼ��
    bool IsGroundedSimple()
    {
        // ����1�����߼��
        float rayLength = 0.6f; // ������
        Vector2 rayStart = (Vector2)transform.position + Vector2.down * 0.5f;
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, rayLength);

        // ���ӻ����ߣ������ã�
        Debug.DrawRay(rayStart, Vector2.down * rayLength, hit.collider != null ? Color.green : Color.red);

        return hit.collider != null;
    }

    // ��ײ�����Ϊ����
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

    // 设置速度倍数（供外部调用，如蜘蛛网）
    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }

    // 重置速度倍数（恢复原始速度）
    public void ResetSpeedMultiplier()
    {
        speedMultiplier = 1f;
    }
}