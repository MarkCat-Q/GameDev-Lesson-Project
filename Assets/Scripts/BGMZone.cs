using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BGM区域脚本，用于检测玩家进入/离开区域并通知GameManager
/// </summary>
public class BGMZone : MonoBehaviour
{
    [Header("区域设置")]
    [Tooltip("区域编号（1、2或3）")]
    public int zoneNumber = 1;
    
    [Header("玩家标签")]
    [Tooltip("与触发器交互的玩家标签名")]
    public string playerTag = "Player";
    
    private GameManager gameManager;
    
    void Start()
    {
        // 查找GameManager
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
        
        if (gameManager == null)
        {
            Debug.LogWarning($"[BGMZone {zoneNumber}] 未找到GameManager！");
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            if (gameManager != null)
            {
                gameManager.OnPlayerEnterBGMZone(zoneNumber);
                Debug.Log($"[BGMZone] 玩家进入区域 {zoneNumber}");
            }
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            if (gameManager != null)
            {
                gameManager.OnPlayerExitBGMZone(zoneNumber);
                Debug.Log($"[BGMZone] 玩家离开区域 {zoneNumber}");
            }
        }
    }
}

