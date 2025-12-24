using UnityEngine;

public class LauncherAllDIr : MonoBehaviour
{
    [Header("发射设置")]
    [Tooltip("需要带有此标签的物体进入触发器才会被发射")]
    public string playerTag = "Player";

    [Tooltip("蓄力阈值时间（秒），在触发器中停留超过该时间就会被发射")]
    public float holdTimeThreshold = 0.5f;

    [Tooltip("发射力度大小")]
    public float launchForce = 15f;

    [Header("输入设置")]
    [Tooltip("用于决定方向的水平轴名称（默认 Horizontal）")]
    public string horizontalAxis = "Horizontal";

    [Tooltip("用于决定方向的垂直轴名称（默认 Vertical）")]
    public string verticalAxis = "Vertical";

    [Tooltip("如果没有任何方向输入，是否使用发射器自身的本地 X 轴作为默认方向")]
    public bool useDefaultDirectionWhenNoInput = true;

    [Header("可选：调试")]
    [Tooltip("是否在 Scene 视图中绘制默认发射方向")]
    public bool debugDrawDefaultDirection = true;

    private float _holdTimer = 0f;
    private bool _isPlayerInside = false;
    private bool _hasLaunchedThisStay = false;
    private Rigidbody2D _playerRb;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        _isPlayerInside = true;
        _hasLaunchedThisStay = false;
        _holdTimer = 0f;
        _playerRb = other.attachedRigidbody;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!_isPlayerInside || !other.CompareTag(playerTag))
            return;

        if (_hasLaunchedThisStay)
            return;

        _holdTimer += Time.deltaTime;

        if (_holdTimer >= holdTimeThreshold)
        {
            Vector2 dir = GetInputDirection();
            if (dir.sqrMagnitude <= 0.0001f)
            {
                // 没有输入方向且不允许使用默认方向，则继续等待
                if (!useDefaultDirectionWhenNoInput)
                    return;

                // 使用发射器默认方向（本地 X 轴）
                dir = (Vector2)transform.right;
            }

            LaunchPlayer(dir.normalized);
            _hasLaunchedThisStay = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        ResetState();
    }

    private Vector2 GetInputDirection()
    {
        float h = Input.GetAxisRaw(horizontalAxis);
        float v = Input.GetAxisRaw(verticalAxis);

        // 只允许上、下、左、右四个方向
        // 规则：取绝对值更大的轴作为主方向；若都为 0，则返回 (0,0)
        if (Mathf.Abs(h) > Mathf.Abs(v))
        {
            // 左或右
            return new Vector2(Mathf.Sign(h), 0f);
        }
        else if (Mathf.Abs(v) > 0f)
        {
            // 上或下
            return new Vector2(0f, Mathf.Sign(v));
        }

        // 没有输入
        return Vector2.zero;
    }

    private void LaunchPlayer(Vector2 direction)
    {
        if (_playerRb == null)
            return;

        _playerRb.velocity = direction * launchForce;
    }

    private void ResetState()
    {
        _isPlayerInside = false;
        _hasLaunchedThisStay = false;
        _holdTimer = 0f;
        _playerRb = null;
    }

    private void OnDrawGizmos()
    {
        if (!debugDrawDefaultDirection)
            return;

        Gizmos.color = Color.cyan;
        Vector3 start = transform.position;
        Vector3 end = start + transform.right * 1.5f;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.06f);
    }
}