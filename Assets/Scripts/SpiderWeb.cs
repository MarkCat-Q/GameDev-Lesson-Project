using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderWeb : MonoBehaviour
{
    // 存储当前在蜘蛛网中的玩家信息
    private PlatformerMovement playerInWeb = null;
    private Collider2D webCollider;
    private Collider2D playerCollider;

    void Start()
    {
        webCollider = GetComponent<Collider2D>();
        if (webCollider == null)
        {
            Debug.LogError("SpiderWeb 需要 Collider2D 组件！");
        }
    }

    void FixedUpdate()
    {
        // 在物理更新前检测与蜘蛛网重叠的玩家并提前设置碰撞忽略
        // 这样可以避免在 OnCollisionEnter2D 时已经发生碰撞
        if (webCollider != null)
        {
            // 检测与蜘蛛网碰撞体重叠的所有碰撞体
            ContactFilter2D filter = new ContactFilter2D();
            filter.NoFilter(); // 不过滤任何层
            List<Collider2D> overlappingColliders = new List<Collider2D>();
            webCollider.OverlapCollider(filter, overlappingColliders);
            
            // 查找玩家
            PlatformerMovement foundPlayer = null;
            Collider2D foundPlayerCollider = null;
            
            foreach (Collider2D col in overlappingColliders)
            {
                PlatformerMovement player = col.GetComponent<PlatformerMovement>();
                if (player != null)
                {
                    foundPlayer = player;
                    foundPlayerCollider = col;
                    break;
                }
            }
            
            // 如果找到了玩家
            if (foundPlayer != null)
            {
                // 如果还没有记录玩家，或者玩家改变了，更新记录
                if (playerInWeb == null || foundPlayer != playerInWeb)
                {
                    if (playerInWeb != null && playerCollider != null)
                    {
                        // 恢复之前的碰撞
                        Physics2D.IgnoreCollision(playerCollider, webCollider, false);
                    }
                    
                    playerInWeb = foundPlayer;
                    playerCollider = foundPlayerCollider;
                }
                
                // 根据冲刺状态设置碰撞忽略
                bool isDashing = foundPlayer.IsDashing();
                Physics2D.IgnoreCollision(playerCollider, webCollider, isDashing);
            }
            else
            {
                // 如果没有找到玩家，但之前有记录，清除记录
                if (playerInWeb != null)
                {
                    // 恢复碰撞
                    if (playerCollider != null)
                    {
                        Physics2D.IgnoreCollision(playerCollider, webCollider, false);
                    }
                    playerInWeb = null;
                    playerCollider = null;
                }
            }
        }
    }

    // 当玩家进入蜘蛛网碰撞体时（需要碰撞体是非触发器）
    void OnCollisionEnter2D(Collision2D collision)
    {
        PlatformerMovement player = collision.gameObject.GetComponent<PlatformerMovement>();
        if (player != null)
        {
            // 如果玩家不在冲刺状态，说明碰撞没有被忽略，这是正常的阻挡
            if (!player.IsDashing())
            {
                Debug.Log("玩家被蜘蛛网阻挡（需要冲刺才能通过）");
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        PlatformerMovement player = collision.gameObject.GetComponent<PlatformerMovement>();
        if (player != null && player == playerInWeb)
        {
            Debug.Log("玩家离开蜘蛛网");
        }
    }
}
