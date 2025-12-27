using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spikes : MonoBehaviour
{
    [Header("伤害设置")]
    public int damage = 1; // 伤害值
    
    // 记录每个玩家的无敌状态，用于检测状态变化
    private Dictionary<Collider2D, bool> playerInvincibleStates = new Dictionary<Collider2D, bool>();
    
    /// <summary>
    /// 当玩家进入地刺的触发器时造成伤害
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        // 检测是否是玩家
        if (other.CompareTag("Player"))
        {
            // 获取玩家的PlatformerMovement组件
            PlatformerMovement player = other.GetComponent<PlatformerMovement>();
            if (player != null)
            {
                // 记录玩家当前的无敌状态
                playerInvincibleStates[other] = player.IsInvincible();
                
                // 造成伤害（跳过击退效果，使用默认方向）
                // skipKnockback = true 表示不击退
                player.TakeDamage(damage, Vector2.zero, true);
                
                // 更新状态（因为受到伤害后会进入无敌状态）
                playerInvincibleStates[other] = true;
            }
        }
    }
    
    /// <summary>
    /// 当玩家停留在地刺的触发器内时持续检测
    /// </summary>
    void OnTriggerStay2D(Collider2D other)
    {
        // 检测是否是玩家
        if (other.CompareTag("Player"))
        {
            // 获取玩家的PlatformerMovement组件
            PlatformerMovement player = other.GetComponent<PlatformerMovement>();
            if (player != null)
            {
                // 如果玩家已死亡，不处理
                if (player.IsDead()) return;
                
                // 获取玩家当前的无敌状态
                bool currentInvincible = player.IsInvincible();
                
                // 如果之前记录过这个玩家
                if (playerInvincibleStates.ContainsKey(other))
                {
                    bool previousInvincible = playerInvincibleStates[other];
                    
                    // 如果玩家从无敌状态变为非无敌状态（无敌时间结束），造成伤害
                    if (previousInvincible && !currentInvincible)
                    {
                        // 造成伤害（跳过击退效果）
                        player.TakeDamage(damage, Vector2.zero, true);
                        // 更新状态（因为受到伤害后会进入无敌状态）
                        playerInvincibleStates[other] = true;
                    }
                    else
                    {
                        // 更新当前状态
                        playerInvincibleStates[other] = currentInvincible;
                    }
                }
                else
                {
                    // 如果没有记录，初始化状态
                    playerInvincibleStates[other] = currentInvincible;
                }
            }
        }
    }
    
    /// <summary>
    /// 当玩家离开地刺的触发器时移除记录
    /// </summary>
    void OnTriggerExit2D(Collider2D other)
    {
        // 移除玩家记录
        if (other.CompareTag("Player"))
        {
            playerInvincibleStates.Remove(other);
        }
    }
}
