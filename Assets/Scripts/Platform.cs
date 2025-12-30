using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Platform : MonoBehaviour
{
    private Collider2D platformCollider;
    private GameObject player;
    
    void Start()
    {
        // 优先获取Composite Collider 2D（tilemap常用）
        CompositeCollider2D compositeCollider = GetComponent<CompositeCollider2D>();
        if (compositeCollider != null)
        {
            platformCollider = compositeCollider;
        }
        else
        {
            // 如果没有Composite Collider，尝试获取其他Collider2D
            platformCollider = GetComponent<Collider2D>();
        }
        
        // 如果平台有Rigidbody2D，确保它不会移动（设置为Kinematic或Static）
        Rigidbody2D platformRb = GetComponent<Rigidbody2D>();
        if (platformRb != null)
        {
            // 如果平台应该是静态的，设置为Kinematic
            if (platformRb.bodyType == RigidbodyType2D.Dynamic)
            {
                platformRb.bodyType = RigidbodyType2D.Kinematic;
            }
        }
        
        // 查找玩家对象（假设玩家有"Player"标签）
        player = GameObject.FindGameObjectWithTag("Player");
        
        // 如果没有找到玩家，尝试通过名称查找
        if (player == null)
        {
            player = GameObject.Find("Player");
        }
        
        if (platformCollider == null)
        {
            Debug.LogWarning("Platform: 未找到碰撞器组件！请确保平台有 Composite Collider 2D 或其他 Collider2D 组件。");
        }
    }

    void Update()
    {
        if (player == null || platformCollider == null)
            return;
        
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null)
            return;
        
        // 获取玩家的位置信息
        float playerTop = playerCollider.bounds.max.y;
        float playerBottom = playerCollider.bounds.min.y;
        float platformTop = platformCollider.bounds.max.y;
        float platformBottom = platformCollider.bounds.min.y;
        
        // 获取玩家的Rigidbody2D来检测移动方向
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        bool playerMovingUp = playerRb != null && playerRb.velocity.y > 0.1f;
        bool playerMovingDown = playerRb != null && playerRb.velocity.y < -0.1f;
        
        // 判断玩家是否在平台的垂直范围内（水平方向也需要检查）
        bool playerInPlatformVerticalRange = playerBottom < platformTop && playerTop > platformBottom;
        
        // 如果玩家在平台下方且向上移动，则禁用碰撞（允许从下往上穿透）
        if (playerTop < platformTop && playerMovingUp)
        {
            Physics2D.IgnoreCollision(playerCollider, platformCollider, true);
        }
        // 如果玩家已经完全穿透到平台上方，重新启用碰撞
        else if (playerBottom > platformTop)
        {
            Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
        }
        // 如果玩家在平台上方且向下移动，确保碰撞启用（防止从上方穿透）
        else if (playerBottom >= platformTop && playerMovingDown)
        {
            Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
        }
        // 如果玩家在平台上或静止，确保碰撞启用
        else
        {
            Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
        }
    }
    
    // 当玩家离开平台时，确保碰撞恢复正常
    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject == player && platformCollider != null)
        {
            // 确保碰撞已启用
            Physics2D.IgnoreCollision(collision.collider, platformCollider, false);
        }
    }
}
