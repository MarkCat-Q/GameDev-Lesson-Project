using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TriggerText : MonoBehaviour
{
    [Header("玩家标签")]
    [Tooltip("与触发器交互的玩家标签名")]
    public string playerTag = "Player";

    [Header("文本对象(TMP)")]
    [Tooltip("需要显示/隐藏的TMP对象")]
    public GameObject textObject;

    void Awake()
    {
        // 若未在Inspector指定，则自动寻找子物体上的TMP组件
        if (textObject == null)
        {
            TMP_Text tmp = GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                textObject = tmp.gameObject;
            }
        }

        SetTextActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            SetTextActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            SetTextActive(false);
        }
    }

    void SetTextActive(bool isActive)
    {
        if (textObject != null)
        {
            textObject.SetActive(isActive);
        }
    }
}
