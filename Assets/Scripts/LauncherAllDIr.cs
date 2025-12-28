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

    [Tooltip("发射后冷却时间（秒），防止短时间内重复发射")]
    public float launchCooldown = 0.3f;

    [Tooltip("发射时是否重置玩家的冲刺和二段跳能力")]
    public bool resetPlayerAbilitiesOnLaunch = true;

    [Tooltip("发射时是否禁用玩家输入（秒），0表示不禁用")]
    public float disableInputDuration = 0f;

    [Tooltip("发射速度保持时间（秒），在这段时间内持续保持发射速度，防止被玩家移动系统覆盖")]
    public float launchVelocityMaintainDuration = 0.2f;

    [Header("输入设置")]
    [Tooltip("用于决定方向的水平轴名称（默认 Horizontal）")]
    public string horizontalAxis = "Horizontal";

    [Tooltip("用于决定方向的垂直轴名称（默认 Vertical）")]
    public string verticalAxis = "Vertical";

    [Tooltip("如果没有任何方向输入，是否使用发射器自身的本地 X 轴作为默认方向")]
    public bool useDefaultDirectionWhenNoInput = true;

    [Header("音效和特效（可选）")]
    [Tooltip("发射时播放的音效")]
    public AudioClip launchSound;
    
    [Tooltip("发射时播放的特效预制体")]
    public GameObject launchEffectPrefab;
    
    [Tooltip("发射特效的生成位置偏移")]
    public Vector2 effectOffset = Vector2.zero;

    [Header("可选：调试")]
    [Tooltip("是否在 Scene 视图中绘制默认发射方向")]
    public bool debugDrawDefaultDirection = true;
    
    [Tooltip("是否输出调试日志")]
    public bool debugLog = false;
    
    [Tooltip("【调试模式】启用手动发射：玩家进入发射器后静止，按下方向键后才发射")]
    public bool enableDebugManualLaunch = false;

    private float _holdTimer = 0f;
    private bool _isPlayerInside = false;
    private bool _hasLaunchedThisStay = false;
    private float _cooldownTimer = 0f;
    private Rigidbody2D _playerRb;
    private PlatformerMovement _playerMovement;
    private GameObject _playerObject;
    private AudioSource _audioSource;
    
    // 发射状态管理
    private bool _isLaunching = false;
    private Vector2 _launchVelocity = Vector2.zero;
    private float _launchMaintainTimer = 0f;
    
    // 调试模式：手动发射状态
    private bool _isWaitingForInput = false;

    private void Awake()
    {
        // 尝试获取或添加 AudioSource 组件
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null && launchSound != null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }
    }

    private void Update()
    {
        // 更新冷却计时器
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
        }
        
        // 更新发射状态计时器
        if (_isLaunching && _launchMaintainTimer > 0f)
        {
            _launchMaintainTimer -= Time.deltaTime;
            if (_launchMaintainTimer <= 0f)
            {
                _isLaunching = false;
                // 发射状态结束后，清空玩家引用（如果玩家已经离开触发器）
                ClearPlayerReferences();
                if (debugLog)
                {
                    Debug.Log("[全方向发射器] 发射速度保持时间结束");
                }
            }
        }
    }

    private void FixedUpdate()
    {
        // 调试模式：等待输入时保持玩家静止（优先级最高）
        if (enableDebugManualLaunch && _isWaitingForInput && _playerRb != null)
        {
            _playerRb.velocity = Vector2.zero;
            return; // 等待输入时，不执行后续的发射速度保持逻辑
        }
        
        // 在物理更新中持续保持发射速度，防止被玩家的移动系统覆盖
        if (_isLaunching && _launchMaintainTimer > 0f)
        {
            // 检查 Rigidbody2D 是否仍然有效（玩家可能被销毁或离开）
            if (_playerRb == null)
            {
                _isLaunching = false;
                _launchMaintainTimer = 0f;
                if (debugLog)
                {
                    Debug.LogWarning("[全方向发射器] Rigidbody2D 已失效，停止保持发射速度");
                }
                return;
            }
            
            // 保持发射速度，但允许重力影响垂直速度（如果发射方向有垂直分量）
            // 如果发射方向主要是水平的，保持水平速度，允许垂直速度受重力影响
            // 如果发射方向有垂直分量，保持完整的发射速度
            if (Mathf.Abs(_launchVelocity.y) > 0.1f)
            {
                // 有垂直分量，保持完整速度
                _playerRb.velocity = _launchVelocity;
            }
            else
            {
                // 主要是水平方向，保持水平速度，允许垂直速度受重力影响
                _playerRb.velocity = new Vector2(_launchVelocity.x, _playerRb.velocity.y);
            }
            
            if (debugLog && Time.frameCount % 30 == 0)
            {
                Debug.Log($"[全方向发射器] 保持发射速度: {_playerRb.velocity}, 剩余时间: {_launchMaintainTimer:F2}秒");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        // 如果还在冷却中，不处理
        if (_cooldownTimer > 0f)
            return;

        _isPlayerInside = true;
        _hasLaunchedThisStay = false;
        _holdTimer = 0f;
        _playerRb = other.attachedRigidbody;
        _playerObject = other.gameObject;
        
        // 尝试获取玩家移动组件
        if (_playerMovement == null && _playerObject != null)
        {
            _playerMovement = _playerObject.GetComponent<PlatformerMovement>();
        }

        // 调试模式：进入后静止，等待方向键输入
        if (enableDebugManualLaunch)
        {
            _isWaitingForInput = true;
            if (_playerRb != null)
            {
                _playerRb.velocity = Vector2.zero;
            }
            if (debugLog)
            {
                Debug.Log($"[全方向发射器-调试模式] 玩家进入触发器，等待方向键输入...");
            }
        }
        else
        {
            if (debugLog)
            {
                Debug.Log($"[全方向发射器] 玩家进入触发器，开始蓄力...");
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!_isPlayerInside || !other.CompareTag(playerTag))
            return;

        // 如果还在冷却中，不处理
        if (_cooldownTimer > 0f)
            return;

        if (_hasLaunchedThisStay)
            return;

        // 调试模式：等待方向键输入，按下后立即发射
        if (enableDebugManualLaunch && _isWaitingForInput)
        {
            Vector2 dir = GetInputDirection();
            if (dir.sqrMagnitude > 0.0001f)
            {
                // 检测到方向键输入，立即发射
                _isWaitingForInput = false;
                LaunchPlayer(dir.normalized);
                _hasLaunchedThisStay = true;
                _cooldownTimer = launchCooldown;
                
                if (debugLog)
                {
                    Debug.Log($"[全方向发射器-调试模式] 检测到方向键输入: {dir}，立即发射");
                }
            }
            // 如果没有输入，继续等待（速度在 FixedUpdate 中被设置为0）
            return;
        }

        // 正常模式：蓄力后发射
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
            _cooldownTimer = launchCooldown;
        }
        else if (debugLog && Time.frameCount % 30 == 0) // 每30帧输出一次，避免日志过多
        {
            float remainingTime = holdTimeThreshold - _holdTimer;
            Debug.Log($"[全方向发射器] 蓄力中... 剩余时间: {remainingTime:F2}秒");
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

        // 计算发射速度
        _launchVelocity = direction * launchForce;
        
        // 设置发射状态
        _isLaunching = true;
        _launchMaintainTimer = launchVelocityMaintainDuration;
        
        // 重置玩家状态（如果需要）
        if (resetPlayerAbilitiesOnLaunch && _playerMovement != null)
        {
            // 重置速度倍率（确保玩家移动速度正常）
            _playerMovement.ResetSpeedMultiplier();
        }

        // 设置发射速度（直接覆盖，确保发射效果明显）
        _playerRb.velocity = _launchVelocity;

        if (debugLog)
        {
            Debug.Log($"[全方向发射器] 设置发射速度: {_launchVelocity}, 方向: {direction}, Rigidbody2D: {(_playerRb != null ? "存在" : "不存在")}");
        }

        // 播放音效
        if (launchSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(launchSound);
        }

        // 生成特效
        if (launchEffectPrefab != null)
        {
            Vector3 effectPosition = transform.position + (Vector3)effectOffset;
            GameObject effect = Instantiate(launchEffectPrefab, effectPosition, Quaternion.identity);
            // 让特效面向发射方向
            if (direction.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                effect.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }

        // 如果设置了禁用输入时间，通过协程处理
        if (disableInputDuration > 0f && _playerMovement != null)
        {
            StartCoroutine(DisablePlayerInputTemporarily());
        }

        // 触发发射事件（如果有监听器）
        OnPlayerLaunched?.Invoke(_playerObject, direction, launchForce);

        if (debugLog)
        {
            Debug.Log($"[全方向发射器] 发射玩家！方向: {direction}, 力度: {launchForce}");
        }
    }

    private System.Collections.IEnumerator DisablePlayerInputTemporarily()
    {
        if (_playerMovement == null)
            yield break;

        // 通过设置速度倍数为0来禁用输入
        // 注意：这只会禁用水平移动，玩家仍然可以跳跃等
        // 如果需要完全禁用输入，需要在 PlatformerMovement 中添加相应的方法
        _playerMovement.SetSpeedMultiplier(0f);
        
        yield return new WaitForSeconds(disableInputDuration);
        
        // 恢复速度倍率
        _playerMovement.ResetSpeedMultiplier();
    }

    private void ResetState()
    {
        _isPlayerInside = false;
        _hasLaunchedThisStay = false;
        _holdTimer = 0f;
        _isWaitingForInput = false; // 重置等待输入状态
        
        // 注意：不清空 _playerRb 和 _playerObject，因为可能还在发射状态中
        // 只有在发射状态完全结束后才清空（在 Update 中处理）
        // 也不重置 _playerMovement，因为玩家对象可能还在场景中
    }
    
    private void ClearPlayerReferences()
    {
        // 只有在发射状态完全结束后才清空玩家引用
        if (!_isLaunching && _launchMaintainTimer <= 0f)
        {
            _playerRb = null;
            _playerObject = null;
        }
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

    // 发射事件（可选，供外部监听）
    public System.Action<GameObject, Vector2, float> OnPlayerLaunched;
}