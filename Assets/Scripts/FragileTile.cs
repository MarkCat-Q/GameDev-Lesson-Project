using UnityEngine;
using System.Collections;

public class FragileTile : MonoBehaviour
{
    [Header("可击碎方向")]
    [SerializeField] private bool breakFromTop = true;
    [SerializeField] private bool breakFromBottom = true;
    [SerializeField] private bool breakFromLeft = true;
    [SerializeField] private bool breakFromRight = true;

    [Header("摧毁设置")]
    [SerializeField] private float destroyDelay = 0f;
    
    [Header("打击震动效果设置")]
    [SerializeField] private float shakeIntensity = 0.1f; // 震动强度
    [SerializeField] private float shakeDuration = 0.3f; // 震动持续时间
    [SerializeField] private float shakeFrequency = 20f; // 震动频率（每秒震动次数）

    private Collider2D cachedCollider;
    private bool isBroken;
    private Vector3 originalPosition; // 原始位置
    private Coroutine shakeCoroutine; // 震动协程引用

    public enum AttackDirection
    {
        Up,
        Down,
        Left,
        Right,
        Unknown
    }

    private void Awake()
    {
        cachedCollider = GetComponent<Collider2D>();
        if (cachedCollider == null)
        {
            Debug.LogWarning($"{name} 缺少 Collider2D，无法检测攻击触发。");
        }
        
        // 记录原始位置
        originalPosition = transform.position;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isBroken || cachedCollider == null)
            return;

        if (!other.CompareTag("AttackZone"))
            return;

        AttackDirection dir = GetAttackDirection(other.transform);
        if (!IsDirectionAllowed(dir))
            return;

        Break();
    }

    private AttackDirection GetAttackDirection(Transform attackTransform)
    {
        // 优先读取自定义标记组件（若存在）
        var marker = attackTransform.GetComponent<IAttackDirectionProvider>();
        if (marker != null)
            return marker.Direction;

        // 其次根据名称简单识别
        string n = attackTransform.name.ToLower();
        if (n.Contains("up")) return AttackDirection.Up;
        if (n.Contains("down")) return AttackDirection.Down;
        if (n.Contains("left")) return AttackDirection.Left;
        if (n.Contains("right")) return AttackDirection.Right;

        // 最后根据相对位置推断
        Vector2 diff = (Vector2)(transform.position - attackTransform.position);
        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
            return diff.x > 0 ? AttackDirection.Left : AttackDirection.Right;
        else
            return diff.y > 0 ? AttackDirection.Down : AttackDirection.Up;
    }

    private bool IsDirectionAllowed(AttackDirection dir)
    {
        switch (dir)
        {
            case AttackDirection.Up: return breakFromBottom;   // 攻击来自下方向上打
            case AttackDirection.Down: return breakFromTop;    // 攻击来自上方向下打
            case AttackDirection.Left: return breakFromLeft;   // 攻击来自左往右
            case AttackDirection.Right: return breakFromRight; // 攻击来自右往左
            default: return false;
        }
    }

    private void Break()
    {
        if (isBroken) return;
        
        isBroken = true;

        if (cachedCollider != null)
            cachedCollider.enabled = false;

        // 先执行震动效果，震动完成后再销毁
        if (shakeCoroutine == null)
        {
            shakeCoroutine = StartCoroutine(ShakeAndDestroy());
        }
    }
    
    /// <summary>
    /// 震动并销毁协程
    /// </summary>
    private IEnumerator ShakeAndDestroy()
    {
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;
        float shakeTimer = 0f;
        
        // 震动阶段
        while (elapsedTime < shakeDuration)
        {
            elapsedTime += Time.deltaTime;
            shakeTimer += Time.deltaTime * shakeFrequency;
            
            // 计算震动偏移（使用正弦波和随机值组合，产生更自然的震动效果）
            float x = (Mathf.Sin(shakeTimer * 2f) + Random.Range(-0.5f, 0.5f)) * 0.5f;
            float y = (Mathf.Cos(shakeTimer * 2f) + Random.Range(-0.5f, 0.5f)) * 0.5f;
            
            // 随着时间衰减震动强度
            float intensityMultiplier = 1f - (elapsedTime / shakeDuration);
            Vector3 shakeOffset = new Vector3(x, y, 0) * shakeIntensity * intensityMultiplier;
            
            // 应用震动偏移
            transform.position = startPosition + shakeOffset;
            
            yield return null;
        }
        
        // 震动结束，恢复原始位置
        transform.position = startPosition;
        
        // 等待一小段时间确保位置恢复
        yield return new WaitForSeconds(0.05f);
        
        // 销毁对象
        Destroy(gameObject, destroyDelay);
        
        shakeCoroutine = null;
    }

    /// <summary>
    /// 可选：在攻击触发器上实现以显式提供方向。
    /// </summary>
    public interface IAttackDirectionProvider
    {
        AttackDirection Direction { get; }
    }
}
