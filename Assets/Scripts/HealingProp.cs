using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 回血道具-触碰时消失并回血
/// </summary>
public class HealingProp : MonoBehaviour
{
    [Header("回血设置")]
    [Tooltip("恢复的血量值")]
    public int healAmount = 1;
    
    private Vector3 originalPosition; // 原始位置（用于跃动效果）
    
    void Start()
    {
        // 记录原始位置
        originalPosition = transform.position;
    }

    void Update()
    {
        // 上下小范围跃动（基于原始位置）
        float offsetY = Mathf.Sin(Time.time * 2f) * 0.0005f;
        transform.position = new Vector3(
            originalPosition.x, 
            originalPosition.y + offsetY,
            originalPosition.z);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 获取玩家的PlatformerMovement组件
            PlatformerMovement player = other.GetComponent<PlatformerMovement>();
            if (player != null)
            {
                // 恢复玩家血量
                player.Heal(healAmount);
                Debug.Log($"[回血道具] 玩家恢复 {healAmount} 点血量");
            }
            
            // 销毁道具
            Destroy(gameObject);
        }
    }
}
