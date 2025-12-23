using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderWeb : MonoBehaviour
{
    [Header("减速设置")]
    [Tooltip("速度降低比例，0.5表示降低到原来的50%")]
    [Range(0f, 1f)]
    public float speedReductionRatio = 0.5f;

    // 存储当前在蜘蛛网中的玩家
    private PlatformerMovement playerInWeb = null;

    // 当玩家进入蜘蛛网碰撞体时
    void OnTriggerEnter2D(Collider2D other)
    {
        PlatformerMovement player = other.GetComponent<PlatformerMovement>();
        if (player != null)
        {
            playerInWeb = player;
            // 应用减速效果
            player.SetSpeedMultiplier(speedReductionRatio);
            Debug.Log($"玩家进入蜘蛛网，速度降低至原来的 {speedReductionRatio * 100}%");
        }
    }

    // 当玩家离开蜘蛛网碰撞体时
    void OnTriggerExit2D(Collider2D other)
    {
        PlatformerMovement player = other.GetComponent<PlatformerMovement>();
        if (player != null && player == playerInWeb)
        {
            // 恢复原始速度
            player.ResetSpeedMultiplier();
            playerInWeb = null;
            Debug.Log("玩家离开蜘蛛网，速度已恢复");
        }
    }

    // 如果使用碰撞体而不是触发器，也可以使用这个方法
    void OnCollisionEnter2D(Collision2D collision)
    {
        PlatformerMovement player = collision.gameObject.GetComponent<PlatformerMovement>();
        if (player != null)
        {
            playerInWeb = player;
            player.SetSpeedMultiplier(speedReductionRatio);
            Debug.Log($"玩家进入蜘蛛网，速度降低至原来的 {speedReductionRatio * 100}%");
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        PlatformerMovement player = collision.gameObject.GetComponent<PlatformerMovement>();
        if (player != null && player == playerInWeb)
        {
            player.ResetSpeedMultiplier();
            playerInWeb = null;
            Debug.Log("玩家离开蜘蛛网，速度已恢复");
        }
    }
}
