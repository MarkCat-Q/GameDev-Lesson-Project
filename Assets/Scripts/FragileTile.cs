using UnityEngine;

public class FragileTile : MonoBehaviour
{
    [Header("可击碎方向")]
    [SerializeField] private bool breakFromTop = true;
    [SerializeField] private bool breakFromBottom = true;
    [SerializeField] private bool breakFromLeft = true;
    [SerializeField] private bool breakFromRight = true;

    [Header("摧毁设置")]
    [SerializeField] private float destroyDelay = 0f;

    private Collider2D cachedCollider;
    private bool isBroken;

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
        isBroken = true;

        if (cachedCollider != null)
            cachedCollider.enabled = false;

        Destroy(gameObject, destroyDelay);
    }

    /// <summary>
    /// 可选：在攻击触发器上实现以显式提供方向。
    /// </summary>
    public interface IAttackDirectionProvider
    {
        AttackDirection Direction { get; }
    }
}
