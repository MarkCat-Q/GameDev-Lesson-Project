using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("巡逻设置")]
    [Tooltip("巡逻的左边界点")]
    public Transform leftPoint;
    [Tooltip("巡逻的右边界点")]
    public Transform rightPoint;
    [Tooltip("移动速度")]
    public float moveSpeed = 2f;
    
    [Header("动画参数")]
    [Tooltip("Animator中速度参数的名称")]
    public string speedParameterName = "Speed";
    [Tooltip("Animator中死亡触发器的名称（可选）")]
    public string deathTriggerName = "Death";
    
    [Header("死亡设置")]
    [Tooltip("死亡后销毁延迟时间（秒）")]
    public float deathDestroyDelay = 0.5f;
    [Tooltip("是否播放死亡动画")]
    public bool playDeathAnimation = true;
    
    [Header("伤害设置")]
    [Tooltip("对玩家造成的伤害值")]
    public int damageToPlayer = 1;
    [Tooltip("玩家标签名称")]
    public string playerTag = "Player";
    
    private Rigidbody2D rb;
    private Animator animator;
    private Vector3 originalScale;
    private bool movingRight = true; // 当前移动方向
    private bool isDead = false; // 是否已死亡
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        originalScale = transform.localScale;
        
        // 如果没有指定边界点，使用当前位置左右各2单位作为默认范围
        if (leftPoint == null || rightPoint == null)
        {
            Debug.LogWarning("EnemyController: 未指定巡逻边界点，将使用默认范围");
            GameObject leftObj = new GameObject("LeftPoint");
            GameObject rightObj = new GameObject("RightPoint");
            leftObj.transform.position = transform.position + Vector3.left * 2f;
            rightObj.transform.position = transform.position + Vector3.right * 2f;
            leftPoint = leftObj.transform;
            rightPoint = rightObj.transform;
        }
    }

    void Update()
    {
        // 如果已死亡，停止所有逻辑
        if (isDead) return;
        
        // 检查是否到达边界并转向
        if (movingRight && transform.position.x >= rightPoint.position.x)
        {
            movingRight = false;
        }
        else if (!movingRight && transform.position.x <= leftPoint.position.x)
        {
            movingRight = true;
        }
        
        // 设置移动方向
        float moveDirection = movingRight ? 1f : -1f;
        
        // 控制动画
        if (animator != null)
        {
            animator.SetFloat(speedParameterName, Mathf.Abs(moveDirection));
        }
        
        // 翻转角色朝向
        if (moveDirection > 0)
        {
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
        else if (moveDirection < 0)
        {
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
    }
    
    void FixedUpdate()
    {
        // 如果已死亡，停止移动
        if (isDead)
        {
            if (rb != null)
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
            return;
        }
        
        // 在FixedUpdate中处理物理移动
        float moveDirection = movingRight ? 1f : -1f;
        if (rb != null)
        {
            rb.velocity = new Vector2(moveDirection * moveSpeed, rb.velocity.y);
        }
        else
        {
            // 如果没有Rigidbody2D，使用Transform移动
            transform.position += new Vector3(moveDirection * moveSpeed * Time.fixedDeltaTime, 0, 0);
        }
    }
    
    // 检测玩家攻击
    void OnTriggerEnter2D(Collider2D other)
    {
        // 如果已死亡，忽略后续攻击
        if (isDead) return;
        
        // 检测是否被玩家的攻击区域击中
        if (other.CompareTag("AttackZone"))
        {
            Debug.Log($"[敌人] {gameObject.name} 被攻击！");
            Die();
        }
    }
    
    // 检测与玩家的碰撞（伤害玩家）
    void OnCollisionEnter2D(Collision2D collision)
    {
        // 如果已死亡，不伤害玩家
        if (isDead) return;
        
        // 检测是否是玩家
        if (collision.gameObject.CompareTag(playerTag))
        {
            // 获取玩家的PlatformerMovement组件
            PlatformerMovement player = collision.gameObject.GetComponent<PlatformerMovement>();
            if (player != null)
            {
                // 计算伤害方向（从敌人指向玩家）
                Vector2 damageDirection = ((Vector2)collision.transform.position - (Vector2)transform.position).normalized;
                
                // 如果方向向量太小（几乎重叠），使用敌人朝向的反方向
                if (damageDirection.magnitude < 0.1f)
                {
                    // 根据敌人朝向确定伤害方向
                    damageDirection = movingRight ? Vector2.right : Vector2.left;
                }
                
                // 对玩家造成伤害（带击退效果）
                player.TakeDamage(damageToPlayer, damageDirection, false);
                
                Debug.Log($"[敌人] {gameObject.name} 对玩家造成 {damageToPlayer} 点伤害");
            }
        }
    }
    
    // 死亡处理
    void Die()
    {
        if (isDead) return; // 防止重复调用
        
        isDead = true;
        
        // 停止移动
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        
        // 播放死亡动画
        if (playDeathAnimation && animator != null && !string.IsNullOrEmpty(deathTriggerName))
        {
            animator.SetTrigger(deathTriggerName);
        }
        
        // 禁用碰撞体（防止继续与玩家或其他物体交互）
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }
        
        // 可选：禁用子对象的碰撞体
        Collider2D[] childColliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in childColliders)
        {
            col.enabled = false;
        }
        
        // 延迟销毁对象
        StartCoroutine(DestroyAfterDelay());
    }
    
    // 延迟销毁协程
    IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(deathDestroyDelay);
        Destroy(gameObject);
    }
    
    // 在Scene视图中绘制巡逻范围（仅在编辑器中可见）
    void OnDrawGizmosSelected()
    {
        if (leftPoint != null && rightPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(leftPoint.position, rightPoint.position);
            Gizmos.DrawWireSphere(leftPoint.position, 0.2f);
            Gizmos.DrawWireSphere(rightPoint.position, 0.2f);
        }
    }
}
