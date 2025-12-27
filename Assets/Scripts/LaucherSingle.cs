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

    [Tooltip("发射角度偏移（度），正值为向上偏移，负值为向下偏移")]
    [Range(-90f, 90f)]
    public float launchAngleOffset = 0f;

    [Tooltip("发射后冷却时间（秒），防止短时间内重复发射")]
    public float launchCooldown = 0.3f;

    [Tooltip("发射时是否重置玩家的冲刺和二段跳能力")]
    public bool resetPlayerAbilitiesOnLaunch = true;

    [Tooltip("发射时是否禁用玩家输入（秒），0表示不禁用")]
    public float disableInputDuration = 0f;

    [Tooltip("发射速度保持时间（秒），在这段时间内持续保持发射速度，防止被玩家移动系统覆盖")]
    public float launchVelocityMaintainDuration = 0.2f;

    [Header("音效和特效（可选）")]
    [Tooltip("发射时播放的音效")]
    public AudioClip launchSound;
    
    [Tooltip("发射时播放的特效预制体")]
    public GameObject launchEffectPrefab;
    
    [Tooltip("发射特效的生成位置偏移")]
    public Vector2 effectOffset = Vector2.zero;

    [Header("可选：调试")]
    [Tooltip("是否在 Scene 视图中绘制发射方向")]
    public bool debugDrawDirection = true;
    
    [Tooltip("是否输出调试日志")]
    public bool debugLog = false;

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
                    Debug.Log("[发射器] 发射速度保持时间结束");
                }
            }
        }
    }

    private void FixedUpdate()
    {
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
                    Debug.LogWarning("[发射器] Rigidbody2D 已失效，停止保持发射速度");
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
                Debug.Log($"[发射器] 保持发射速度: {_playerRb.velocity}, 剩余时间: {_launchMaintainTimer:F2}秒");
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

        if (debugLog)
        {
            Debug.Log($"[发射器] 玩家进入触发器，开始蓄力...");
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

        _holdTimer += Time.deltaTime;

        if (_holdTimer >= holdTimeThreshold)
        {
            LaunchPlayer();
            _hasLaunchedThisStay = true;
            _cooldownTimer = launchCooldown;
        }
        else if (debugLog && Time.frameCount % 30 == 0) // 每30帧输出一次，避免日志过多
        {
            float remainingTime = holdTimeThreshold - _holdTimer;
            Debug.Log($"[发射器] 蓄力中... 剩余时间: {remainingTime:F2}秒");
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

        // 计算发射方向（基于发射器的本地 X 轴，加上角度偏移）
        Vector2 baseDirection = transform.right.normalized;
        
        // 应用角度偏移
        if (Mathf.Abs(launchAngleOffset) > 0.01f)
        {
            float angleInRadians = launchAngleOffset * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleInRadians);
            float sin = Mathf.Sin(angleInRadians);
            // 旋转向量
            Vector2 rotatedDir = new Vector2(
                baseDirection.x * cos - baseDirection.y * sin,
                baseDirection.x * sin + baseDirection.y * cos
            );
            baseDirection = rotatedDir.normalized;
        }

        // 重置玩家状态（如果需要）
        if (resetPlayerAbilitiesOnLaunch && _playerMovement != null)
        {
            // 重置速度倍率（确保玩家移动速度正常）
            _playerMovement.ResetSpeedMultiplier();
        }

        // 计算发射速度
        _launchVelocity = baseDirection * launchForce;
        
        // 设置发射状态
        _isLaunching = true;
        _launchMaintainTimer = launchVelocityMaintainDuration;
        
        // 设置发射速度（直接覆盖，确保发射效果明显）
        _playerRb.velocity = _launchVelocity;

        if (debugLog)
        {
            Debug.Log($"[发射器] 设置发射速度: {_launchVelocity}, Rigidbody2D: {(_playerRb != null ? "存在" : "不存在")}");
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
            if (baseDirection.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
                effect.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }

        // 如果设置了禁用输入时间，通过协程处理
        if (disableInputDuration > 0f && _playerMovement != null)
        {
            StartCoroutine(DisablePlayerInputTemporarily());
        }

        // 触发发射事件（如果有监听器）
        OnPlayerLaunched?.Invoke(_playerObject, baseDirection, launchForce);

        if (debugLog)
        {
            Debug.Log($"[发射器] 发射玩家！方向: {baseDirection}, 力度: {launchForce}, 角度偏移: {launchAngleOffset}°");
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
        if (!debugDrawDirection)
            return;

        Gizmos.color = Color.yellow;
        Vector3 start = transform.position;
        
        // 计算实际发射方向（包含角度偏移）
        Vector2 baseDirection = transform.right.normalized;
        if (Mathf.Abs(launchAngleOffset) > 0.01f)
        {
            float angleInRadians = launchAngleOffset * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleInRadians);
            float sin = Mathf.Sin(angleInRadians);
            Vector2 rotatedDir = new Vector2(
                baseDirection.x * cos - baseDirection.y * sin,
                baseDirection.x * sin + baseDirection.y * cos
            );
            baseDirection = rotatedDir.normalized;
        }
        
        Vector3 end = start + (Vector3)baseDirection * 1.5f;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.06f);
        
        // 绘制角度范围（可选）
        if (Mathf.Abs(launchAngleOffset) > 0.01f)
        {
            Gizmos.color = Color.yellow * 0.5f;
            Vector2 baseDir = transform.right.normalized;
            float angleRad = launchAngleOffset * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad);
            float sin = Mathf.Sin(angleRad);
            Vector2 rotated = new Vector2(
                baseDir.x * cos - baseDir.y * sin,
                baseDir.x * sin + baseDir.y * cos
            );
            Gizmos.DrawLine(start, start + (Vector3)rotated * 1.2f);
        }
    }

    // 发射事件（可选，供外部监听）
    public System.Action<GameObject, Vector2, float> OnPlayerLaunched;
}