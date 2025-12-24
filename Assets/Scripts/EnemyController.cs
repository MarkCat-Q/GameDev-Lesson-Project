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
    
    private Rigidbody2D rb;
    private Animator animator;
    private Vector3 originalScale;
    private bool movingRight = true; // 当前移动方向
    
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
