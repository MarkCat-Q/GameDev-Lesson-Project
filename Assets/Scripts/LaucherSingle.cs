using UnityEngine;

public class LaucherSingle : MonoBehaviour
{
    [Header("发射设置")]
    [Tooltip("需要带有此标签的物体进入触发器才会被发射")]
    public string playerTag = "Player";

    [Tooltip("蓄力阈值时间（秒），在触发器中停留超过该时间就会被发射")]
    public float holdTimeThreshold = 0.5f;

    [Tooltip("发射力度大小")]
    public float launchForce = 15f;

    [Header("可选：调试")]
    [Tooltip("是否在 Scene 视图中绘制发射方向")]
    public bool debugDrawDirection = true;

    private float _holdTimer = 0f;
    private bool _isPlayerInside = false;
    private bool _hasLaunchedThisStay = false;
    private Rigidbody2D _playerRb;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("OnTriggerEnter2D: " + other.tag);
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
            LaunchPlayer();
            _hasLaunchedThisStay = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        ResetState();
    }

    private void LaunchPlayer()
    {
        if (_playerRb == null)
            return;

        // 使用发射器自身的本地 X 轴作为发射方向
        Vector2 launchDir = transform.right.normalized;

        // 可以根据需要选择速度方式或力方式：
        // 这里直接设置速度，保证效果立即且稳定
        _playerRb.velocity = launchDir * launchForce;
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
        if (!debugDrawDirection)
            return;

        Gizmos.color = Color.yellow;
        Vector3 start = transform.position;
        Vector3 end = start + transform.right * 1.5f;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.06f);
    }
}