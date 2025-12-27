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
    private Rigidbody2D rb; // 刚体组件（如果有）
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
            Debug.LogWarning($"[易碎平台] {name} 缺少 Collider2D，无法检测攻击触发。");
        }
        else
        {
            // 检查碰撞器是否为 Trigger
            if (!cachedCollider.isTrigger)
            {
                Debug.LogWarning($"[易碎平台] {name} 的 Collider2D 不是 Trigger，OnTriggerEnter2D 将无法触发！请将 Is Trigger 设置为 true。");
            }
        }
        
        // 检查是否有 Rigidbody2D
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Debug.Log($"[易碎平台] {name} 检测到 Rigidbody2D，震动时将使用物理方式");
        }
        
        // 记录原始位置
        originalPosition = transform.position;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isBroken || cachedCollider == null)
        {
            if (isBroken)
                Debug.Log($"[易碎平台] {name} 已被击碎，忽略攻击");
            return;
        }

        if (!other.CompareTag("AttackZone"))
        {
            Debug.Log($"[易碎平台] {name} 检测到碰撞，但标签不是 AttackZone，标签: {other.tag}");
            return;
        }

        Debug.Log($"[易碎平台] {name} 检测到攻击区域: {other.name}");

        AttackDirection dir = GetAttackDirection(other.transform);
        Debug.Log($"[易碎平台] {name} 检测到攻击方向: {dir}");

        if (!IsDirectionAllowed(dir))
        {
            Debug.Log($"[易碎平台] {name} 攻击方向 {dir} 不被允许");
            return;
        }

        Debug.Log($"[易碎平台] {name} 开始击碎流程");
        Break();
    }

    private AttackDirection GetAttackDirection(Transform attackTransform)
    {
        // 优先读取自定义标记组件（若存在）
        var marker = attackTransform.GetComponent<IAttackDirectionProvider>();
        if (marker != null)
        {
            Debug.Log($"[易碎平台] {name} 从标记组件获取方向: {marker.Direction}");
            return marker.Direction;
        }

        // 计算相对位置（在方法开始处计算，避免重复计算）
        Vector2 diff = (Vector2)(transform.position - attackTransform.position);

        // 其次根据名称简单识别（支持 AttackZoneUp, AttackZoneDown, AttackZoneFront 等）
        string n = attackTransform.name.ToLower();
        if (n.Contains("up") || n.Contains("top"))
        {
            Debug.Log($"[易碎平台] {name} 从名称识别方向: Up (名称: {attackTransform.name})");
            return AttackDirection.Up;
        }
        if (n.Contains("down") || n.Contains("bottom"))
        {
            Debug.Log($"[易碎平台] {name} 从名称识别方向: Down (名称: {attackTransform.name})");
            return AttackDirection.Down;
        }
        if (n.Contains("left"))
        {
            Debug.Log($"[易碎平台] {name} 从名称识别方向: Left (名称: {attackTransform.name})");
            return AttackDirection.Left;
        }
        if (n.Contains("right"))
        {
            Debug.Log($"[易碎平台] {name} 从名称识别方向: Right (名称: {attackTransform.name})");
            return AttackDirection.Right;
        }
        // AttackZoneFront 根据玩家朝向判断
        if (n.Contains("front"))
        {
            // 需要获取玩家朝向，这里先尝试从相对位置推断
            // 如果攻击区域在平台左侧，说明玩家在右侧，攻击方向是 Right
            // 如果攻击区域在平台右侧，说明玩家在左侧，攻击方向是 Left
            AttackDirection frontDir = diff.x > 0 ? AttackDirection.Right : AttackDirection.Left;
            Debug.Log($"[易碎平台] {name} 从名称识别方向: {frontDir} (Front, 名称: {attackTransform.name})");
            return frontDir;
        }

        // 最后根据相对位置推断
        AttackDirection inferredDir;
        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
            inferredDir = diff.x > 0 ? AttackDirection.Left : AttackDirection.Right;
        else
            inferredDir = diff.y > 0 ? AttackDirection.Down : AttackDirection.Up;
        
        Debug.Log($"[易碎平台] {name} 从相对位置推断方向: {inferredDir} (位置差: {diff})");
        return inferredDir;
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
        if (isBroken)
        {
            Debug.LogWarning($"[易碎平台] {name} 尝试重复击碎，已忽略");
            return;
        }
        
        Debug.Log($"[易碎平台] {name} 开始击碎，启动震动效果");
        isBroken = true;

        if (cachedCollider != null)
        {
            cachedCollider.enabled = false;
            Debug.Log($"[易碎平台] {name} 已禁用碰撞器");
        }

        // 停止之前的协程（如果存在）
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        // 先执行震动效果，震动完成后再销毁
        shakeCoroutine = StartCoroutine(ShakeAndDestroy());
        if (shakeCoroutine == null)
        {
            Debug.LogError($"[易碎平台] {name} 启动震动协程失败！");
        }
        else
        {
            Debug.Log($"[易碎平台] {name} 震动协程已启动");
        }
    }
    
    /// <summary>
    /// 震动并销毁协程
    /// </summary>
    private IEnumerator ShakeAndDestroy()
    {
        Debug.Log($"[易碎平台] {name} 震动协程开始执行");
        
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;
        float shakeTimer = 0f;
        
        // 如果有 Rigidbody2D，暂时禁用物理影响（如果可能）
        bool wasKinematic = false;
        if (rb != null)
        {
            wasKinematic = rb.isKinematic;
            // 如果可能，设置为 Kinematic 以避免物理系统干扰震动
            if (!wasKinematic)
            {
                rb.isKinematic = true;
                rb.velocity = Vector2.zero;
            }
        }
        
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
        
        Debug.Log($"[易碎平台] {name} 震动完成，准备销毁");
        
        // 震动结束，恢复原始位置
        transform.position = startPosition;
        
        // 恢复 Rigidbody2D 状态（如果需要）
        if (rb != null && !wasKinematic)
        {
            rb.isKinematic = wasKinematic;
        }
        
        // 等待一小段时间确保位置恢复
        yield return new WaitForSeconds(0.05f);
        
        // 销毁对象
        Debug.Log($"[易碎平台] {name} 正在销毁（延迟: {destroyDelay}秒）");
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
