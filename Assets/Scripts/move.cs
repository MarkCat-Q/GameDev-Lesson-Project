using UnityEngine;
using System.Collections;
using System; 

public class PlatformerMovement : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 4.2f;
    public string attackTrigger = "Attack";
    public string attackDownTrigger = "AttackDown";
    public string attackUpTrigger = "AttackUp";
    
    [Header("攻击设置")]
    public GameObject attackZoneFront; // 前方攻击区域
    public GameObject attackZoneUp; // 上方攻击区域
    public GameObject attackZoneDown; // 下方攻击区域
    public float attackDuration = 0.3f; // 攻击持续时间（秒）

    [Header("跳跃设置")]
    public float jumpSpeed = 15.7f; // 跳跃初始速度
    public float minJumpTime = 0.08f; // 最小跳跃时间
    public float maxJumpTime = 0.2f; // 最大跳跃时间
    public float coyoteTime = 0.1f; // 离开地面后仍可跳跃的时间

    [Header("重力设置")]
    public float gravityScale = 3f; // 重力缩放
    public float maxFallSpeed = 21f; // 最大下落速度

    [Header("二段跳设置")]
    public bool hasDoubleJump = false; // 是否拥有二段跳能力
    private bool canDoubleJump = false; // 是否可以二段跳
    private bool hasUsedDoubleJump = false; // 是否已经使用过二段跳

    [Header("冲刺设置")]
    public bool hasDash = false; // 是否拥有冲刺能力
    public float dashSpeed = 20f; // 冲刺速度
    public float dashDuration = 0.25f; // 冲刺持续时间
    public float dashCooldown = 0.5f; // 冲刺冷却时间
    public string dashTrigger = "Dash"; // 冲刺动画触发器
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private bool canDash = true;
    private int dashUsedInAir = 0; // 空中使用次数

    [Header("贴墙设置")]
    public bool hasWallCling = false; // 是否拥有贴墙能力
    public float wallCheckDistance = 0.5f; // 检测墙壁的距离
    public float maxWallClingDistance = 0.2f; // 最大贴墙距离，超过此距离不认为在墙上
    public float wallJumpHorizontalSpeed = 8.3f; // 贴墙跳跃水平速度
    public bool debugWallDetection = false; // 是否显示墙壁检测调试信息
    // Climb动画已改为Bool变量Climbing，不再需要触发器字符串
    private bool isWallClinging = false;
    private bool isOnWall = false;
    private int wallDirection = 0; // -1左，1右

    [Header("小球悬挂设置")]
    private bool isOnBall = false; // 是否在小球上
    private bool isHangingOnBall = false; // 是否正在悬挂在小球上

    [Header("血量设置")]
    public int maxHealth = 5; // 最大血量
    private int currentHealth; // 当前血量
    public event Action<int, int> OnHealthChanged; // 血量变化事件 (当前血量, 最大血量)
    public event Action OnPlayerDeath; // 玩家死亡事件
    public event Action OnPlayerRespawn; // 玩家重生事件

    [Header("受伤/无敌设置")]
    public float invincibleTime = 1.3f; // 无敌持续时间（根据设计文档为1.3秒）
    private bool isInvincible = false;   // 是否处于无敌状态
    public string hurtTrigger = "Hurt";
    
    [Header("受伤硬直设置")]
    public float hitPauseDuration = 0.3f; // 画面暂停时间（根据设计文档为0.3秒）
    public float knockbackHorizontalSpeed = 15f; // 击退水平速度倍数（根据设计文档为15x）
    public float knockbackUpwardSpeed = 7.5f; // 击退向上速度倍数（根据设计文档为7.5x）
    public float knockbackDuration = 0.2f; // 击退持续时间（根据设计文档为0.2秒）
    
    [Header("死亡/重生设置")]
    private bool isDead = false; // 是否死亡
    private Vector3 respawnPosition; // 重生位置（暂定为原地）

    [Header("地面检测设置")]
    public float groundCheckDistance = 0.6f; // 地面检测距离
    public float groundCheckOffset = 0.4f; // 地面检测偏移

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;
    private float speedMultiplier = 1f;
    private Collider2D selfCollider; // 自己的碰撞体，用于获取边界和排除检测

    // 跳跃相关
    private bool isGrounded = false;
    private float coyoteTimeTimer = 0f;
    private bool isJumping = false;
    private float jumpHoldTime = 0f;
    private bool wasGrounded = false;

    // 输入缓冲
    private float jumpInputBufferTime = 0.15f; // 预输入时间窗口
    private float jumpInputBufferTimer = 0f;
    
    // 攻击相关
    private bool isAttacking = false; // 是否正在攻击
    
    [Header("下抓设置")]
    public float downslashBounceSpeed = 12f; // 下抓反弹速度（12x）
    public float downslashBounceDuration = 0.25f; // 下抓反弹持续时间（0.25秒）
    private bool isDownslashing = false; // 是否正在下抓
    private bool isBouncing = false; // 是否正在反弹
    private float bounceTimer = 0f; // 反弹计时器
    
    [Header("上抓设置")]
    private bool isUpslashing = false; // 是否正在上抓
    
    // 受伤硬直相关
    private bool isInHitStun = false; // 是否处于硬直状态
    private bool isKnockbackActive = false; // 是否正在击退
    private float knockbackTimer = 0f; // 击退计时器
    private Vector2 knockbackDirection; // 击退方向

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
        selfCollider = GetComponent<Collider2D>();
        
        // 设置重力缩放
        rb.gravityScale = gravityScale;
        
        // 初始化攻击触发器（如果未在Inspector中指定，则自动查找子对象）
        InitializeAttackZones();
        
        // 初始禁用所有攻击触发器
        SetAttackZonesActive(false);
        
        // 初始化血量系统
        currentHealth = maxHealth;
        respawnPosition = transform.position; // 记录初始位置作为重生点
        OnHealthChanged?.Invoke(currentHealth, maxHealth); // 通知UI更新
    }
    
    void InitializeAttackZones()
    {
        // 如果未在Inspector中指定，则自动查找子对象
        if (attackZoneFront == null)
        {
            Transform front = transform.Find("AttackZoneFront");
            if (front != null) attackZoneFront = front.gameObject;
        }
        
        if (attackZoneUp == null)
        {
            Transform up = transform.Find("AttackZoneUp");
            if (up != null) attackZoneUp = up.gameObject;
        }
        
        if (attackZoneDown == null)
        {
            Transform down = transform.Find("AttackZoneDown");
            if (down != null) attackZoneDown = down.gameObject;
        }
        
        // 检查是否找到所有攻击区域
        if (attackZoneFront == null || attackZoneUp == null || attackZoneDown == null)
        {
            Debug.LogWarning("[攻击系统] 未找到所有攻击触发器子对象。请确保存在 AttackZoneFront、AttackZoneUp、AttackZoneDown 子对象，或在Inspector中手动指定。");
        }
    }
    
    void SetAttackZonesActive(bool active)
    {
        if (attackZoneFront != null) attackZoneFront.SetActive(active);
        if (attackZoneUp != null) attackZoneUp.SetActive(active);
        if (attackZoneDown != null) attackZoneDown.SetActive(active);
    }

    void Update()
    {
        // 如果死亡，只处理重生输入
        if (isDead)
        {
            HandleRespawnInput();
            return;
        }
        
        // 更新计时器
        UpdateTimers();
        
        // 更新击退效果
        UpdateKnockback();
        
        // 如果处于硬直状态，不处理其他输入
        if (isInHitStun)
        {
            return;
        }
        
        // 检测状态
        CheckGrounded();
        CheckWallContact();
        
        // 处理输入缓冲
        HandleInputBuffer();
        
        // 1. 移动逻辑（非冲刺状态且不在击退状态）
        // 注意：悬挂在小球上时，水平移动仍然允许（玩家可以左右移动）
        if (!isDashing && !isKnockbackActive)
        {
            float horizontal = Input.GetAxis("Horizontal");
            
            // 如果正在下抓反弹，保持向上速度
            if (isBouncing)
            {
                // 反弹期间保持向上速度，允许水平移动
                rb.velocity = new Vector2(horizontal * moveSpeed * speedMultiplier, downslashBounceSpeed);
            }
            // 如果悬挂在小球上，只允许水平移动，垂直速度保持为0
            else if (isHangingOnBall)
            {
                rb.velocity = new Vector2(horizontal * moveSpeed * speedMultiplier, 0f);
            }
            // 如果没有贴墙能力，强制设置速度，防止被物理碰撞卡住
            // 如果有贴墙能力且正在贴墙，保持当前逻辑（速度在 HandleWallCling 中设置）
            else if (!hasWallCling)
            {
                // 强制设置速度，确保即使靠近墙壁也能移动
                rb.velocity = new Vector2(horizontal * moveSpeed * speedMultiplier, rb.velocity.y);
            }
            else if (!isWallClinging)
            {
                // 有贴墙能力但未贴墙时，正常移动
                rb.velocity = new Vector2(horizontal * moveSpeed * speedMultiplier, rb.velocity.y);
            }
            // 如果正在贴墙，速度在 HandleWallCling 中设置，这里不修改
            
            // 动画
            animator.SetFloat("Speed", Mathf.Abs(horizontal));
            
            // 翻转
            if (horizontal > 0) transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
            else if (horizontal < 0) transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }

        // 2. 小球悬挂逻辑
        HandleBallHanging();

        // 3. 贴墙逻辑
        HandleWallCling();

        // 4. 跳跃逻辑
        HandleJump();

        // 5. 冲刺逻辑
        HandleDash();

        // 6. 攻击/下抓/上抓
        if (Input.GetKeyDown(KeyCode.J) || Input.GetButtonDown("Fire1"))
        {
            float vertical = Input.GetAxisRaw("Vertical");
            
            // 检测是否同时按下下方向键
            if (vertical < -0.1f)
            {
                // 下方向键被按下，执行下抓
                Downslash();
            }
            // 检测是否同时按下上方向键
            else if (vertical > 0.1f)
            {
                // 上方向键被按下，执行上抓
                Upslash();
            }
            else
            {
                // 普通攻击
                Attack();
            }
        }

        // 7. 控制重力
        HandleGravity();

        MaintainOriginalScale();
    }

    void UpdateTimers()
    {
        // Coyote Time计时器
        if (!isGrounded && wasGrounded)
        {
            coyoteTimeTimer += Time.deltaTime;
        }
        else
        {
            coyoteTimeTimer = 0f;
        }

        // 跳跃输入缓冲计时器
        if (jumpInputBufferTimer > 0)
        {
            jumpInputBufferTimer -= Time.deltaTime;
        }

        // 冲刺计时器
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                EndDash();
            }
        }

        // 冲刺冷却计时器
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
        
        // 击退计时器
        if (isKnockbackActive)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0)
            {
                isKnockbackActive = false;
            }
        }
        
        // 下抓反弹计时器
        if (isBouncing)
        {
            bounceTimer -= Time.deltaTime;
            if (bounceTimer <= 0)
            {
                isBouncing = false;
            }
        }
    }
    
    void UpdateKnockback()
    {
        // 如果正在击退，持续施加击退速度
        if (isKnockbackActive && knockbackTimer > 0)
        {
            rb.velocity = new Vector2(knockbackDirection.x * knockbackHorizontalSpeed, knockbackDirection.y * knockbackUpwardSpeed);
        }
    }
    
    void HandleRespawnInput()
    {
        // 检测重生输入（J键或点击死亡UI按钮）
        if (Input.GetKeyDown(KeyCode.J))
        {
            Respawn();
        }
    }

    void CheckGrounded()
    {
        wasGrounded = isGrounded;
        bool previousGrounded = isGrounded;
        isGrounded = IsGrounded();
        
        // 调试：如果持续不在地面但速度接近0，输出调试信息
        if (!isGrounded && Mathf.Abs(rb.velocity.y) < 0.1f && Time.frameCount % 60 == 0)
        {
            Vector2 checkPos = (Vector2)transform.position + Vector2.down * groundCheckOffset;
            Collider2D[] allColliders = Physics2D.OverlapCircleAll(checkPos, 0.5f);
            //Debug.LogWarning($"[地面检测] 未检测到地面 - position:{transform.position}, checkPos:{checkPos}, velocity.y:{rb.velocity.y:F2}, 附近碰撞体数量:{allColliders.Length}");
            // foreach (Collider2D col in allColliders)
            // {
            //     Debug.LogWarning($"[地面检测] 附近碰撞体 - Tag:{col.tag}, Name:{col.name}, IsTrigger:{col.isTrigger}");
            // }
        }
        
        // 落地时重置
        if (isGrounded && rb.velocity.y <= 0.1f)
        {
            dashUsedInAir = 0;
            canDash = true;
            if (hasDoubleJump)
            {
                canDoubleJump = true;
                hasUsedDoubleJump = false;
            }
            coyoteTimeTimer = 0f;
        }
    }

    bool IsGrounded()
    {
        // 主要方法：使用OverlapBox检测（可以检测到起点所在的碰撞体，适合Composite Collider）
        Vector2 boxSize = new Vector2(0.4f, 0.1f);
        Vector2 boxCenter = (Vector2)transform.position + Vector2.down * groundCheckOffset;
        Collider2D[] boxColliders = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f);
        
        foreach (Collider2D col in boxColliders)
        {
            if (col.CompareTag("Ground") && !col.isTrigger)
            {
                Debug.DrawRay(boxCenter, Vector2.down * 0.1f, Color.green, 0.1f);
                return true;
            }
        }

        // 备用方法1：使用OverlapCircle检测
        Collider2D[] circleColliders = Physics2D.OverlapCircleAll(boxCenter, 0.2f);
        foreach (Collider2D col in circleColliders)
        {
            if (col.CompareTag("Ground") && !col.isTrigger)
            {
                Debug.DrawRay(boxCenter, Vector2.down * 0.1f, Color.green, 0.1f);
                return true;
            }
        }

        // 备用方法2：使用Raycast检测（从稍微上方开始，避免起点在碰撞体内）
        Vector2 rayStart = (Vector2)transform.position + Vector2.down * (groundCheckOffset - 0.1f);
        Vector2[] checkPoints = new Vector2[]
        {
            rayStart, // 中心点
            rayStart + Vector2.left * 0.3f, // 左侧
            rayStart + Vector2.right * 0.3f // 右侧
        };

        foreach (Vector2 point in checkPoints)
        {
            RaycastHit2D hit = Physics2D.Raycast(point, Vector2.down, groundCheckDistance + 0.1f);
            if (hit.collider != null && hit.collider.CompareTag("Ground") && !hit.collider.isTrigger)
            {
                Debug.DrawRay(point, Vector2.down * hit.distance, Color.green, 0.1f);
                return true;
            }
            else
            {
                Debug.DrawRay(point, Vector2.down * (groundCheckDistance + 0.1f), Color.red, 0.1f);
            }
        }

        return false;
    }

    void CheckWallContact()
    {
        // 检测左右墙壁（从碰撞体边缘开始检测，避免从内部开始）
        if (selfCollider == null)
        {
            selfCollider = GetComponent<Collider2D>();
            if (selfCollider == null)
            {
                isOnWall = false;
                if (debugWallDetection)
                {
                    Debug.LogError("[墙壁检测] 错误：角色没有 Collider2D 组件！");
                }
                return;
            }
        }
        
        // 获取碰撞体的边界
        Bounds bounds = selfCollider.bounds;
        // 从边缘稍微向外偏移，确保起点在碰撞体外
        // 使用更小的偏移量，避免距离太远时检测失败
        float edgeOffset = 0.005f; // 很小的偏移量，确保起点在碰撞体外
        float leftEdge = bounds.min.x - edgeOffset;
        float rightEdge = bounds.max.x + edgeOffset;
        float centerY = bounds.center.y;
        float topY = bounds.max.y - 0.1f; // 稍微向下一点
        float bottomY = bounds.min.y + 0.1f; // 稍微向上一点
        
        if (debugWallDetection && Time.frameCount % 60 == 0) // 每60帧输出一次，避免日志过多
        {
            Debug.Log($"[墙壁检测] 角色位置: {transform.position}, 碰撞体边界: min={bounds.min}, max={bounds.max}, " +
                     $"左边缘(偏移后): {leftEdge}, 右边缘(偏移后): {rightEdge}, 检测距离: {wallCheckDistance}, 边缘偏移: {edgeOffset}");
        }
        
        // 使用多个检测点（上、中、下），从碰撞体边缘稍微外侧开始
        Vector2[] leftCheckPoints = new Vector2[]
        {
            new Vector2(leftEdge, topY),      // 上方
            new Vector2(leftEdge, centerY),   // 中心
            new Vector2(leftEdge, bottomY)    // 下方
        };
        
        Vector2[] rightCheckPoints = new Vector2[]
        {
            new Vector2(rightEdge, topY),     // 上方
            new Vector2(rightEdge, centerY),  // 中心
            new Vector2(rightEdge, bottomY)   // 下方
        };
        
        bool hitLeftWall = false;
        bool hitRightWall = false;
        
        // 检测左侧墙壁（从左边缘向左发射射线）
        // 使用 RaycastAll 来获取所有命中的碰撞体，然后过滤掉自己的
        int leftCheckIndex = 0;
        foreach (Vector2 point in leftCheckPoints)
        {
            // 使用 RaycastAll 获取所有命中的碰撞体
            RaycastHit2D[] hits = Physics2D.RaycastAll(point, Vector2.left, wallCheckDistance);
            
            if (debugWallDetection)
            {
                Debug.Log($"[墙壁检测-左侧-点{leftCheckIndex}] 起点: {point}, 距离: {wallCheckDistance}, 命中数量: {hits.Length}");
            }
            
            bool foundWall = false;
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null) continue;
                
                bool isSelf = hit.collider == selfCollider;
                bool isGround = hit.collider.CompareTag("Ground");
                bool isTrigger = hit.collider.isTrigger;
                
                if (debugWallDetection)
                {
                    Debug.Log($"[墙壁检测-左侧-点{leftCheckIndex}] 命中碰撞体: {hit.collider.name}, " +
                             $"Tag: {hit.collider.tag}, 距离: {hit.distance:F3}, " +
                             $"是否自己: {isSelf}, 是否Ground: {isGround}, 是否触发器: {isTrigger}");
                }
                
                if (!isSelf && isGround && !isTrigger)
                {
                    // 只有当距离在合理范围内时才认为在墙上
                    if (hit.distance <= maxWallClingDistance)
                    {
                        hitLeftWall = true;
                        foundWall = true;
                        Debug.DrawRay(point, Vector2.left * hit.distance, Color.yellow, 0.1f);
                        if (debugWallDetection)
                        {
                            Debug.Log($"[墙壁检测-左侧] ✓ 检测到墙壁！碰撞体: {hit.collider.name}, 距离: {hit.distance:F3} (<= {maxWallClingDistance})");
                        }
                        break;
                    }
                    else if (debugWallDetection)
                    {
                        Debug.LogWarning($"[墙壁检测-左侧] 检测到墙壁但距离太远: {hit.distance:F3} > {maxWallClingDistance}");
                    }
                }
            }
            
            if (foundWall) break;
            
            // 绘制调试射线
            float rayDistance = hits.Length > 0 ? hits[0].distance : wallCheckDistance;
            bool isWall = false;
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider != selfCollider && hit.collider.CompareTag("Ground") && !hit.collider.isTrigger && hit.distance <= maxWallClingDistance)
                {
                    isWall = true;
                    break;
                }
            }
            Debug.DrawRay(point, Vector2.left * rayDistance, isWall ? Color.yellow : Color.gray, 0.1f);
            
            leftCheckIndex++;
        }
        
        // 检测右侧墙壁（从右边缘向右发射射线）
        // 使用 RaycastAll 来获取所有命中的碰撞体，然后过滤掉自己的
        int rightCheckIndex = 0;
        foreach (Vector2 point in rightCheckPoints)
        {
            // 使用 RaycastAll 获取所有命中的碰撞体
            RaycastHit2D[] hits = Physics2D.RaycastAll(point, Vector2.right, wallCheckDistance);
            
            if (debugWallDetection)
            {
                Debug.Log($"[墙壁检测-右侧-点{rightCheckIndex}] 起点: {point}, 距离: {wallCheckDistance}, 命中数量: {hits.Length}");
            }
            
            bool foundWall = false;
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null) continue;
                
                bool isSelf = hit.collider == selfCollider;
                bool isGround = hit.collider.CompareTag("Ground");
                bool isTrigger = hit.collider.isTrigger;
                
                if (debugWallDetection)
                {
                    Debug.Log($"[墙壁检测-右侧-点{rightCheckIndex}] 命中碰撞体: {hit.collider.name}, " +
                             $"Tag: {hit.collider.tag}, 距离: {hit.distance:F3}, " +
                             $"是否自己: {isSelf}, 是否Ground: {isGround}, 是否触发器: {isTrigger}");
                }
                
                if (!isSelf && isGround && !isTrigger)
                {
                    // 只有当距离在合理范围内时才认为在墙上
                    if (hit.distance <= maxWallClingDistance)
                    {
                        hitRightWall = true;
                        foundWall = true;
                        Debug.DrawRay(point, Vector2.right * hit.distance, Color.yellow, 0.1f);
                        if (debugWallDetection)
                        {
                            Debug.Log($"[墙壁检测-右侧] ✓ 检测到墙壁！碰撞体: {hit.collider.name}, 距离: {hit.distance:F3} (<= {maxWallClingDistance})");
                        }
                        break;
                    }
                    else if (debugWallDetection)
                    {
                        Debug.LogWarning($"[墙壁检测-右侧] 检测到墙壁但距离太远: {hit.distance:F3} > {maxWallClingDistance}");
                    }
                }
            }
            
            if (foundWall) break;
            
            // 绘制调试射线
            float rayDistance = hits.Length > 0 ? hits[0].distance : wallCheckDistance;
            bool isWall = false;
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider != selfCollider && hit.collider.CompareTag("Ground") && !hit.collider.isTrigger && hit.distance <= maxWallClingDistance)
                {
                    isWall = true;
                    break;
                }
            }
            Debug.DrawRay(point, Vector2.right * rayDistance, isWall ? Color.yellow : Color.gray, 0.1f);
            
            rightCheckIndex++;
        }
        
        isOnWall = hitLeftWall || hitRightWall;
        
        if (debugWallDetection && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[墙壁检测-结果] isOnWall: {isOnWall}, hitLeftWall: {hitLeftWall}, hitRightWall: {hitRightWall}, wallDirection: {wallDirection}");
        }
        
        if (hitLeftWall)
        {
            wallDirection = -1;
        }
        else if (hitRightWall)
        {
            wallDirection = 1;
        }
        else
        {
            // 如果没有检测到墙壁，保持之前的 wallDirection（用于贴墙跳跃后的方向记忆）
            // 但如果完全离开墙壁，可以重置为0
            if (!isWallClinging)
            {
                wallDirection = 0;
            }
        }
        
        // 额外的检测方法：使用 OverlapCircle 作为备用检测
        if (!isOnWall && debugWallDetection)
        {
            // 在左右边缘使用 OverlapCircle 检测附近的碰撞体
            Collider2D[] leftOverlaps = Physics2D.OverlapCircleAll(new Vector2(leftEdge, centerY), wallCheckDistance);
            Collider2D[] rightOverlaps = Physics2D.OverlapCircleAll(new Vector2(rightEdge, centerY), wallCheckDistance);
            
            if (leftOverlaps.Length > 0 || rightOverlaps.Length > 0)
            {
                Debug.LogWarning($"[墙壁检测-备用方法] 左侧附近碰撞体数量: {leftOverlaps.Length}, 右侧附近碰撞体数量: {rightOverlaps.Length}");
                foreach (Collider2D col in leftOverlaps)
                {
                    if (col != selfCollider)
                    {
                        Debug.LogWarning($"[墙壁检测-备用方法] 左侧附近碰撞体: {col.name}, Tag: {col.tag}, 是否触发器: {col.isTrigger}");
                    }
                }
                foreach (Collider2D col in rightOverlaps)
                {
                    if (col != selfCollider)
                    {
                        Debug.LogWarning($"[墙壁检测-备用方法] 右侧附近碰撞体: {col.name}, Tag: {col.tag}, 是否触发器: {col.isTrigger}");
                    }
                }
            }
        }

        // 检测头顶是否碰到墙
        Vector2 centerPos = (Vector2)transform.position;
        RaycastHit2D hitUp = Physics2D.Raycast(centerPos, Vector2.up, 0.5f);
        if (hitUp.collider != null && hitUp.collider != selfCollider && hitUp.collider.CompareTag("Ground") && !hitUp.collider.isTrigger && rb.velocity.y > 0)
        {
            // 头顶碰到墙，速度降为0
            rb.velocity = new Vector2(rb.velocity.x, 0f);
            isJumping = false;
            jumpHoldTime = 0f;
        }
    }

    void HandleInputBuffer()
    {
        // 检测跳跃输入（预输入）
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpInputBufferTimer = jumpInputBufferTime;
        }
        // 如果正在按住空格键且输入缓冲已过期，重新设置（用于长按情况）
        else if (Input.GetKey(KeyCode.Space) && jumpInputBufferTimer <= 0 && !isJumping)
        {
            jumpInputBufferTimer = jumpInputBufferTime;
        }
    }

    void HandleJump()
    {
        // 检测按住跳跃键
        if (Input.GetKey(KeyCode.Space) && isJumping && !isOnWall)
        {
            jumpHoldTime += Time.deltaTime;
            // 如果还在最大跳跃时间内，继续给予向上的力
            if (jumpHoldTime < maxJumpTime && rb.velocity.y > 0)
            {
                // 保持跳跃速度
                rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
            }
        }

        // 检测松开跳跃键
        if (Input.GetKeyUp(KeyCode.Space))
        {
            // 只有在上升阶段才停止跳跃状态
            if (rb.velocity.y > 0)
            {
                isJumping = false;
            }
            jumpHoldTime = 0f;
        }
        
        // 如果速度变为向下，停止跳跃状态
        if (rb.velocity.y <= 0 && isJumping)
        {
            isJumping = false;
        }

        // 检测跳跃输入（包括预输入）
        bool jumpInput = jumpInputBufferTimer > 0 || Input.GetKeyDown(KeyCode.Space);
        
        if (jumpInput)
        {
            // 小球悬挂跳跃（优先检测，因为悬挂时允许普通跳跃）
            if (isHangingOnBall && isOnBall)
            {
                StartJump();
                isHangingOnBall = false; // 跳跃后离开悬挂状态
                jumpInputBufferTimer = 0f; // 清除输入缓冲
            }
            // 普通跳跃（在地面或Coyote Time内）
            else
            {
                bool inCoyoteTime = !isGrounded && wasGrounded && coyoteTimeTimer <= coyoteTime;
                bool canNormalJump = (isGrounded || inCoyoteTime) && !isJumping && !isOnWall && !isHangingOnBall;
                
                if (canNormalJump)
                {
                    StartJump();
                    jumpInputBufferTimer = 0f; // 清除输入缓冲
                }
                // 二段跳（需要拥有能力，不在地面，不在Coyote Time内，正在下落，可以二段跳，且未使用过二段跳）
                else if (!isGrounded && !inCoyoteTime && hasDoubleJump && rb.velocity.y <= 0 && canDoubleJump && !hasUsedDoubleJump && !isWallClinging && !isHangingOnBall)
                {
                    if (!hasUsedDoubleJump && canDoubleJump)
                    {
                        StartDoubleJump();
                        jumpInputBufferTimer = 0f; // 清除输入缓冲
                    }
                }
                // 贴墙跳跃（需要按下与墙壁相反方向的方向键）
                else if (isWallClinging && hasWallCling)
                {
                    float horizontal = Input.GetAxis("Horizontal");
                    // 检查是否按下与墙壁相反方向的方向键
                    // 贴在左墙（wallDirection == -1）时，需要按下右方向键（horizontal > 0）
                    // 贴在右墙（wallDirection == 1）时，需要按下左方向键（horizontal < 0）
                    bool pressingOppositeDirection = (wallDirection == -1 && horizontal > 0.1f) || 
                                                    (wallDirection == 1 && horizontal < -0.1f);
                    
                    if (pressingOppositeDirection)
                    {
                        WallJump();
                        jumpInputBufferTimer = 0f; // 清除输入缓冲
                    }
                    // 如果只按了跳跃键但没有按相反方向键，不执行跳跃（防止沿墙上升）
                }
            }
        }
    }

    void StartJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
        animator.SetTrigger("Jump");
        isJumping = true;
        jumpHoldTime = 0f;
        coyoteTimeTimer = coyoteTime + 0.1f; // 防止重复触发
        jumpInputBufferTimer = 0f;
        // 如果拥有二段跳能力，普通跳跃后可以二段跳
        if (hasDoubleJump)
        {
            canDoubleJump = true;
            hasUsedDoubleJump = false; // 重置二段跳使用状态（新的一次跳跃）
        }
    }

    void StartDoubleJump()
    {
        // 立即禁用二段跳，防止重复触发
        canDoubleJump = false;
        hasUsedDoubleJump = true;
        
        rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
        animator.SetTrigger("Jump");
        isJumping = true;
        jumpHoldTime = 0f;
        jumpInputBufferTimer = 0f;
    }

    void HandleDash()
    {
        if (!hasDash) return;

        // 检测冲刺输入
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && dashCooldownTimer <= 0 && !isDashing)
        {
            StartDash();
        }
    }

    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        canDash = false;
        
        // 触发冲刺动画
        animator.SetTrigger(dashTrigger);
        
        // 确定冲刺方向
        float dashDir = Mathf.Sign(transform.localScale.x);
        if (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f)
        {
            dashDir = Mathf.Sign(Input.GetAxis("Horizontal"));
        }
        
        // 设置冲刺速度
        rb.velocity = new Vector2(dashDir * dashSpeed, 0f);
        
        // 如果在空中使用，记录次数
        if (!isGrounded)
        {
            dashUsedInAir++;
        }
    }

    void EndDash()
    {
        isDashing = false;
        // 落地、下抓到实体、贴墙后会重置，否则空中只能使用一次
        if (isGrounded || (isWallClinging && hasWallCling))
        {
            canDash = true;
            dashUsedInAir = 0;
        }
        else if (dashUsedInAir >= 1)
        {
            canDash = false;
        }
    }

    void HandleBallHanging()
    {
        // 如果在小球上且正在悬挂，设置爬墙动画
        if (isOnBall && isHangingOnBall)
        {
            animator.SetBool("Climbing", true);
            // 如果玩家正在上升（跳跃），取消悬挂状态
            if (isJumping && rb.velocity.y > 0.1f)
            {
                isHangingOnBall = false;
                // 如果不在墙上，设置Climbing为false（在墙上时让HandleWallCling()处理）
                if (!isOnWall)
                {
                    animator.SetBool("Climbing", false);
                }
            }
        }
        // 注意：不在悬挂状态时不设置Climbing，让HandleWallCling()统一管理，避免冲突
    }

    void HandleWallCling()
    {
        if (!hasWallCling)
        {
            // 如果禁用了贴墙能力，确保重置贴墙状态并恢复重力
            if (isWallClinging)
            {
                OnWallClingEnd();
            }
            isWallClinging = false;
            // 确保重力恢复（防止在禁用能力时仍然没有重力）
            if (rb.gravityScale == 0f && !isDashing && !isHangingOnBall)
            {
                rb.gravityScale = gravityScale;
            }
            // 确保速度不被设置为0（防止在禁用能力时仍然不掉落）
            if (rb.velocity.y == 0f && !isGrounded && !isDashing && !isHangingOnBall)
            {
                // 允许正常下落
            }
            return;
        }

        // 如果正在悬挂在小球上，不处理贴墙逻辑
        if (isHangingOnBall)
        {
            return;
        }

        // 检查是否可以贴墙（不在强制上升阶段，且在下落）
        bool canCling = !isJumping || jumpHoldTime >= minJumpTime;
        canCling = canCling && rb.velocity.y <= 0 && isOnWall;

        if (debugWallDetection && isOnWall && Time.frameCount % 30 == 0) // 每30帧输出一次
        {
            Debug.Log($"[贴墙逻辑] isOnWall: {isOnWall}, isJumping: {isJumping}, jumpHoldTime: {jumpHoldTime:F3}, " +
                     $"minJumpTime: {minJumpTime}, velocity.y: {rb.velocity.y:F3}, canCling: {canCling}, " +
                     $"wallDirection: {wallDirection}, isWallClinging: {isWallClinging}");
        }

        if (canCling)
        {
            // 检测是否按住墙方向的方向键
            float horizontal = Input.GetAxis("Horizontal");
            bool holdingWallDirection = (wallDirection == -1 && horizontal < -0.1f) || 
                                       (wallDirection == 1 && horizontal > 0.1f);

            if (debugWallDetection && isOnWall && Time.frameCount % 30 == 0)
            {
                Debug.Log($"[贴墙逻辑] 水平输入: {horizontal:F3}, wallDirection: {wallDirection}, " +
                         $"holdingWallDirection: {holdingWallDirection}");
            }

            if (holdingWallDirection)
            {
                // 开始贴墙
                if (!isWallClinging)
                {
                    if (debugWallDetection)
                    {
                        Debug.Log($"[贴墙逻辑] ✓ 开始贴墙！");
                    }
                    OnWallClingStart();
                }
                else
                {
                    // 已经在贴墙状态，确保Climbing保持为true（防止被其他逻辑重置）
                    animator.SetBool("Climbing", true);
                }
                isWallClinging = true;
                rb.velocity = new Vector2(rb.velocity.x, 0f); // 停止下落
            }
            else
            {
                // 松开方向键，停止贴墙
                if (isWallClinging)
                {
                    if (debugWallDetection)
                    {
                        Debug.Log($"[贴墙逻辑] ✗ 松开方向键，停止贴墙");
                    }
                    OnWallClingEnd();
                }
                isWallClinging = false;
                // 如果不在小球悬挂状态，确保Climbing为false（在墙上但没有按住方向键时不应该显示爬墙动画）
                if (!isHangingOnBall)
                {
                    animator.SetBool("Climbing", false);
                }
            }
        }
        else
        {
            // 如果已经在贴墙状态，但canCling为false，需要判断是否真的应该停止贴墙
            // 只有当明确不在墙上时，才停止贴墙（避免因速度等条件短暂变化导致的闪动）
            if (isWallClinging)
            {
                if (!isOnWall)
                {
                    // 明确不在墙上，停止贴墙
                    if (debugWallDetection)
                    {
                        Debug.LogWarning($"[贴墙逻辑] ✗ 不在墙上，停止贴墙");
                    }
                    OnWallClingEnd();
                    isWallClinging = false;
                }
                else
                {
                    // 仍在墙上，但canCling为false（可能是速度或其他条件），保持贴墙状态
                    // 确保Climbing保持为true
                    animator.SetBool("Climbing", true);
                    rb.velocity = new Vector2(rb.velocity.x, 0f); // 保持停止下落
                }
            }
            else
            {
                // 如果不在墙上且不在小球悬挂状态，确保Climbing为false
                if (!isOnWall && !isHangingOnBall)
                {
                    animator.SetBool("Climbing", false);
                }
            }
        }
    }

    void OnWallClingStart()
    {
        // 设置爬墙动画Bool变量
        animator.SetBool("Climbing", true);
        
        // 贴墙时刷新冲刺和二段跳
        canDash = true;
        dashUsedInAir = 0;
        if (hasDoubleJump)
        {
            canDoubleJump = true;
            hasUsedDoubleJump = false; // 重置二段跳使用状态
        }
    }

    void OnWallClingEnd()
    {
        // 贴墙结束时关闭爬墙动画
        animator.SetBool("Climbing", false);
    }
    
    void OnBallHangingStart()
    {
        // 开始悬挂在小球上
        isHangingOnBall = true;
        
        // 重置二段跳和冲刺
        canDash = true;
        dashUsedInAir = 0;
        if (hasDoubleJump)
        {
            canDoubleJump = true;
            hasUsedDoubleJump = false; // 重置二段跳使用状态
        }
        
        // 停止下落
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        
        Debug.Log("[小球悬挂] 玩家开始悬挂在小球上，二段跳和冲刺已重置");
    }
    
    void OnBallHangingEnd()
    {
        // 离开小球悬挂状态
        isHangingOnBall = false;
        animator.SetBool("Climbing", false);
        Debug.Log("[小球悬挂] 玩家离开小球");
    }

    void WallJump()
    {
        // 45度角斜上跳跃
        rb.velocity = new Vector2(-wallDirection * wallJumpHorizontalSpeed, jumpSpeed);
        animator.SetTrigger("Jump");
        isJumping = true;
        jumpHoldTime = 0f;
        isWallClinging = false;
        animator.SetBool("Climbing", false); // 离开贴墙状态，关闭爬墙动画
        jumpInputBufferTimer = 0f;
    }

    void HandleGravity()
    {
        // 如果正在冲刺、贴墙（且拥有贴墙能力）、悬挂在小球上、正在击退或正在反弹，禁用重力
        if (isDashing || (isWallClinging && hasWallCling) || isHangingOnBall || isKnockbackActive || isBouncing)
        {
            rb.gravityScale = 0f;
        }
        else
        {
            // 恢复重力
            rb.gravityScale = gravityScale;
        }

        // 限制最大下落速度（仅在非击退状态和非反弹状态下）
        if (!isKnockbackActive && !isBouncing && rb.velocity.y < -maxFallSpeed)
        {
            rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);
        }
    }

    public void Attack()
    {
        // 如果正在攻击，不允许重复攻击
        if (isAttacking) return;
        
        // 触发攻击动画
        animator.SetTrigger(attackTrigger);
        
        // 启用攻击触发器
        SetAttackZonesActive(true);
        isAttacking = true;
        
        // 启动协程，在攻击持续时间后禁用触发器
        StartCoroutine(EndAttackAfterDuration());
    }
    
    IEnumerator EndAttackAfterDuration()
    {
        yield return new WaitForSeconds(attackDuration);
        EndAttack();
    }
    
    void EndAttack()
    {
        // 禁用攻击触发器
        SetAttackZonesActive(false);
        isAttacking = false;
    }

    /// <summary>
    /// 下抓动作：检测下方可下抓物体，触发反弹并刷新能力
    /// </summary>
    public void Downslash()
    {
        // 如果正在攻击或下抓，不允许重复下抓
        if (isAttacking || isDownslashing) return;
        
        // 触发下抓攻击动画
        animator.SetTrigger(attackDownTrigger);
        
        // 启用下方攻击区域
        if (attackZoneDown != null)
        {
            attackZoneDown.SetActive(true);
        }
        isDownslashing = true;
        
        // 检测AttackZoneDown区域内是否有"Downslashable" tag的物体
        bool foundDownslashable = CheckDownslashableObjects();
        
        if (foundDownslashable)
        {
            // 触发反弹效果
            StartDownslashBounce();
            
            // 刷新冲刺和二段跳能力
            canDash = true;
            dashUsedInAir = 0;
            if (hasDoubleJump)
            {
                canDoubleJump = true;
                hasUsedDoubleJump = false;
            }
            
            Debug.Log("[下抓] 检测到可下抓物体，触发反弹并刷新冲刺和二段跳能力");
        }
        
        // 启动协程，在下抓持续时间后禁用攻击区域
        StartCoroutine(EndDownslashAfterDuration());
    }
    
    /// <summary>
    /// 检测AttackZoneDown区域内是否有"Downslashable" tag的物体
    /// </summary>
    bool CheckDownslashableObjects()
    {
        if (attackZoneDown == null) return false;
        
        // 获取AttackZoneDown的Collider2D
        Collider2D zoneCollider = attackZoneDown.GetComponent<Collider2D>();
        if (zoneCollider == null)
        {
            Debug.LogWarning("[下抓] AttackZoneDown没有Collider2D组件");
            return false;
        }
        
        // 获取攻击区域的边界
        Bounds bounds = zoneCollider.bounds;
        
        // 使用OverlapBoxAll检测该区域内的所有碰撞体
        Collider2D[] colliders = Physics2D.OverlapBoxAll(bounds.center, bounds.size, 0f);
        
        foreach (Collider2D col in colliders)
        {
            // 检查是否有"Downslashable" tag
            if (col.CompareTag("Downslashable"))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 开始下抓反弹效果
    /// </summary>
    void StartDownslashBounce()
    {
        isBouncing = true;
        bounceTimer = downslashBounceDuration;
        
        // 设置向上速度（12x）
        rb.velocity = new Vector2(rb.velocity.x, downslashBounceSpeed);
    }
    
    /// <summary>
    /// 结束下抓动作的协程
    /// </summary>
    IEnumerator EndDownslashAfterDuration()
    {
        yield return new WaitForSeconds(attackDuration);
        EndDownslash();
    }
    
    /// <summary>
    /// 结束下抓动作
    /// </summary>
    void EndDownslash()
    {
        // 禁用下方攻击区域（其他攻击区域应该由EndAttack处理）
        if (attackZoneDown != null)
        {
            attackZoneDown.SetActive(false);
        }
        isDownslashing = false;
    }

    /// <summary>
    /// 上抓动作：检测上方Ground物体，如果正在上升则停止上升
    /// </summary>
    public void Upslash()
    {
        // 如果正在攻击、上抓或下抓，不允许重复上抓
        if (isAttacking || isUpslashing || isDownslashing) return;
        
        // 触发上抓攻击动画
        animator.SetTrigger(attackUpTrigger);
        
        // 启用上方攻击区域
        if (attackZoneUp != null)
        {
            attackZoneUp.SetActive(true);
        }
        isUpslashing = true;
        
        // 检测AttackZoneUp区域内是否有"Ground" tag的物体
        bool foundGround = CheckUpslashableObjects();
        
        // 如果玩家正在上升且检测到Ground，将纵向速度降为0
        if (foundGround && rb.velocity.y > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0f);
            Debug.Log("[上抓] 检测到Ground物体，正在上升，纵向速度已降为0");
        }
        
        // 启动协程，在上抓持续时间后禁用攻击区域
        StartCoroutine(EndUpslashAfterDuration());
    }
    
    /// <summary>
    /// 检测AttackZoneUp区域内是否有"Ground" tag的物体
    /// </summary>
    bool CheckUpslashableObjects()
    {
        if (attackZoneUp == null) return false;
        
        // 获取AttackZoneUp的Collider2D
        Collider2D zoneCollider = attackZoneUp.GetComponent<Collider2D>();
        if (zoneCollider == null)
        {
            Debug.LogWarning("[上抓] AttackZoneUp没有Collider2D组件");
            return false;
        }
        
        // 获取攻击区域的边界
        Bounds bounds = zoneCollider.bounds;
        
        // 使用OverlapBoxAll检测该区域内的所有碰撞体
        Collider2D[] colliders = Physics2D.OverlapBoxAll(bounds.center, bounds.size, 0f);
        
        foreach (Collider2D col in colliders)
        {
            // 检查是否有"Ground" tag且不是触发器
            if (col.CompareTag("Ground") && !col.isTrigger)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 结束上抓动作的协程
    /// </summary>
    IEnumerator EndUpslashAfterDuration()
    {
        yield return new WaitForSeconds(attackDuration);
        EndUpslash();
    }
    
    /// <summary>
    /// 结束上抓动作
    /// </summary>
    void EndUpslash()
    {
        // 禁用上方攻击区域（其他攻击区域应该由EndAttack处理）
        if (attackZoneUp != null)
        {
            attackZoneUp.SetActive(false);
        }
        isUpslashing = false;
    }

    // --- 受伤逻辑处理 ---

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 接地判定
        if (collision.gameObject.CompareTag("Ground"))
        {
            // 检查碰撞点是否在玩家下方（避免顶到天花板时误判为落地）
            bool isCollisionFromBelow = false;
            foreach (ContactPoint2D contact in collision.contacts)
            {
                // 如果碰撞点的Y坐标小于玩家中心点的Y坐标，说明碰撞来自下方（地面）
                if (contact.point.y < transform.position.y)
                {
                    isCollisionFromBelow = true;
                    break;
                }
            }
            
            // 只有当碰撞来自下方且速度向下时，才认为是落地
            if (isCollisionFromBelow && rb.velocity.y <= 0.1f)
            {
                canDash = true;
                dashUsedInAir = 0;
                if (hasDoubleJump)
                {
                    canDoubleJump = true;
                    hasUsedDoubleJump = false; // 重置二段跳使用状态
                }
            }
        }

        // 碰到敌人逻辑
        if (collision.gameObject.CompareTag("Enemy") && !isInvincible)
        {
            // 计算伤害方向（从敌人指向玩家）
            Vector2 damageDirection = ((Vector2)transform.position - (Vector2)collision.transform.position).normalized;
            if (damageDirection.magnitude < 0.1f) // 如果方向向量太小，使用默认方向
            {
                damageDirection = transform.localScale.x > 0 ? Vector2.left : Vector2.right;
            }
            TakeDamage(1, damageDirection);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 检测小球（tag为Ball）
        if (other.CompareTag("Ball"))
        {
            isOnBall = true;
            // 如果玩家正在下落或速度向下，开始悬挂
            if (rb.velocity.y <= 0)
            {
                OnBallHangingStart();
            }
        }
        
        // 检测道具（通过组件类型）
        if (other.GetComponent<PropDoubleJump>() != null)
        {
            hasDoubleJump = true;
            canDoubleJump = true;
            hasUsedDoubleJump = false; // 重置二段跳使用状态
        }
        else if (other.GetComponent<PropDash>() != null)
        {
            hasDash = true;
            canDash = true;
        }
        else if (other.GetComponent<PropSticky>() != null)
        {
            hasWallCling = true;
        }
    }
    
    void OnTriggerStay2D(Collider2D other)
    {
        // 持续检测小球，如果玩家在小球内且速度向下，保持悬挂状态
        if (other.CompareTag("Ball") && isOnBall)
        {
            // 如果玩家正在下落或速度向下，且未在跳跃上升阶段，开始或保持悬挂
            if (rb.velocity.y <= 0 && (!isJumping || jumpHoldTime >= minJumpTime))
            {
                if (!isHangingOnBall)
                {
                    OnBallHangingStart();
                }
            }
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        // 离开小球
        if (other.CompareTag("Ball"))
        {
            isOnBall = false;
            if (isHangingOnBall)
            {
                OnBallHangingEnd();
            }
        }
    }

    /// <summary>
    /// 受到伤害（无参数版本，用于兼容旧代码）
    /// </summary>
    public void TakeDamage()
    {
        // 默认伤害为1，方向为角色当前朝向的反方向
        float damageDir = transform.localScale.x > 0 ? -1 : 1;
        TakeDamage(1, new Vector2(damageDir, 0), false);
    }
    
    /// <summary>
    /// 受到伤害（完整版本，供外部调用）
    /// </summary>
    /// <param name="damage">伤害值</param>
    /// <param name="damageDirection">伤害来源方向（归一化向量）</param>
    /// <param name="skipKnockback">是否跳过击退效果（用于地刺等场景伤害）</param>
    public void TakeDamage(int damage, Vector2 damageDirection, bool skipKnockback = false)
    {
        // 如果处于无敌状态或已死亡，不处理伤害
        if (isInvincible || isDead) return;
        
        // 1. 扣除血量
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth); // 确保血量不为负
        OnHealthChanged?.Invoke(currentHealth, maxHealth); // 通知UI更新
        
        Debug.Log($"玩家受到 {damage} 点伤害，当前血量: {currentHealth}/{maxHealth}");
        
        // 2. 检查是否死亡
        if (currentHealth <= 0)
        {
            Die();
            return;
        }
        
        // 3. 启动受伤硬直协程（包含画面暂停、击退、无敌等效果）
        StartCoroutine(HitStunRoutine(damageDirection, skipKnockback));
    }
    
    /// <summary>
    /// 受伤硬直协程（根据设计文档实现）
    /// </summary>
    /// <param name="damageDirection">伤害来源方向</param>
    /// <param name="skipKnockback">是否跳过击退效果</param>
    IEnumerator HitStunRoutine(Vector2 damageDirection, bool skipKnockback = false)
    {
        isInHitStun = true;
        isInvincible = true; // 提前设置无敌，防止重复受伤
        
        // 清除反弹状态（硬直期间不应该反弹）
        if (isBouncing)
        {
            isBouncing = false;
            bounceTimer = 0f;
        }
        
        // 1. 播放受伤动画
        animator.SetTrigger(hurtTrigger);
        
        // 2. 画面暂停0.3秒（仅非场景伤害时暂停）
        if (!skipKnockback)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(hitPauseDuration);
            Time.timeScale = 1f;
            
            // 3. 强制角色面向受到伤害的方向
            if (damageDirection.x > 0)
            {
                // 伤害来自右侧，角色面向右侧
                transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
            }
            else if (damageDirection.x < 0)
            {
                // 伤害来自左侧，角色面向左侧
                transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
            }
            
            // 4. 计算击退方向（伤害来源的反方向，向上）
            // 水平方向：伤害来源的反方向
            float horizontalDir = damageDirection.x != 0 ? -Mathf.Sign(damageDirection.x) : (transform.localScale.x > 0 ? -1 : 1);
            // 垂直方向：向上
            knockbackDirection = new Vector2(horizontalDir, 1f).normalized;
            
            // 5. 启动击退效果
            isKnockbackActive = true;
            knockbackTimer = knockbackDuration;
            
            // 6. 等待击退持续时间
            yield return new WaitForSeconds(knockbackDuration);
            
            // 7. 结束击退状态
            isKnockbackActive = false;
        }
        else
        {
            // 场景伤害（如地刺）不暂停画面，不击退，直接进入无敌状态
            yield return null; // 等待一帧，确保状态更新
        }
        
        // 8. 结束硬直状态
        isInHitStun = false;
        
        // 9. 启动无敌协程（倒计时和闪烁）
        StartCoroutine(InvincibleRoutine());
    }
    
    /// <summary>
    /// 死亡处理
    /// </summary>
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        currentHealth = 0;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnPlayerDeath?.Invoke();
        
        // 停止所有移动
        rb.velocity = Vector2.zero;
        
        // 禁用所有能力
        isDashing = false;
        isJumping = false;
        isWallClinging = false;
        isAttacking = false;
        isDownslashing = false;
        isUpslashing = false;
        isInHitStun = false;
        isKnockbackActive = false;
        isHangingOnBall = false;
        isOnBall = false;
        animator.SetBool("Climbing", false); // 确保关闭爬墙动画

        //播放死亡动画
        animator.SetTrigger("Death");
        
        Debug.Log("玩家死亡！按J键或点击死亡UI按钮重生");
    }
    
    /// <summary>
    /// 重生（可在外部调用，例如死亡UI按钮）
    /// </summary>
    /// <param name="respawnPos">重生位置，如果为null则使用默认的重生位置</param>
    public void Respawn(Vector3? respawnPos = null)
    {
        if (!isDead) return;
        
        // 重置血量
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        // 重置位置（使用指定位置或默认重生位置）
        Vector3 targetPosition = respawnPos ?? respawnPosition;
        transform.position = targetPosition;
        
        // 重置状态
        isDead = false;
        isInvincible = false;
        isInHitStun = false;
        isKnockbackActive = false;
        isDashing = false;
        isJumping = false;
        isWallClinging = false;
        isAttacking = false;
        isDownslashing = false;
        isUpslashing = false;
        isHangingOnBall = false;
        isOnBall = false;
        
        // 重置速度
        rb.velocity = Vector2.zero;
        
        // 重置颜色
        spriteRenderer.color = new Color(1, 1, 1, 1f);
        
        // 重置能力状态
        canDash = true;
        dashUsedInAir = 0;
        if (hasDoubleJump)
        {
            canDoubleJump = true;
            hasUsedDoubleJump = false;
        }
        
        // 重置动画机
        ResetAnimator();
        
        OnPlayerRespawn?.Invoke();
        Debug.Log("玩家重生！");
    }
    
    /// <summary>
    /// 重置动画机到初始状态
    /// </summary>
    void ResetAnimator()
    {
        if (animator == null) return;
        
        // 方法1：使用Rebind重新绑定所有参数到默认值（推荐）
        animator.Rebind();
        
        // 方法2：手动重置常用参数（作为备用，确保参数被正确重置）
        // 重置Trigger参数（防止之前的trigger状态残留）
        animator.ResetTrigger(attackTrigger);
        animator.ResetTrigger(hurtTrigger);
        animator.ResetTrigger("Jump");
        animator.ResetTrigger("Death");
        if (!string.IsNullOrEmpty(dashTrigger))
        {
            animator.ResetTrigger(dashTrigger);
        }
        // 重置Climbing Bool变量
        animator.SetBool("Climbing", false);
        
        // 重置Float参数
        animator.SetFloat("Speed", 0f);
        
        // 确保动画机播放默认状态（通常是Idle状态）
        // 注意：这需要Animator Controller中有名为"Idle"的状态，如果没有可以注释掉
        // animator.Play("Idle", 0, 0f);
    }

    // 协程：处理无敌时间和闪烁
    IEnumerator InvincibleRoutine()
    {
        isInvincible = true;

        // 闪烁效果（每0.1秒切换一次透明度）
        float timer = 0;
        while (timer < invincibleTime)
        {
            // 变半透明
            spriteRenderer.color = new Color(1, 1, 1, 0.5f);
            yield return new WaitForSeconds(0.1f);
            // 恢复不透明
            spriteRenderer.color = new Color(1, 1, 1, 1f);
            yield return new WaitForSeconds(0.1f);
            timer += 0.2f;
        }

        spriteRenderer.color = new Color(1, 1, 1, 1f); // 确保最后是完全显示的
        isInvincible = false;
        Debug.Log("无敌状态结束");
    }

    // --- 其他辅助函数 ---

    void MaintainOriginalScale()
    {
        if (transform.localScale.x != originalScale.x && transform.localScale.x != -originalScale.x)
        {
            float direction = Mathf.Sign(transform.localScale.x);
            transform.localScale = new Vector3(direction * Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
    }

    public void SetSpeedMultiplier(float multiplier) { speedMultiplier = multiplier; }
    public void ResetSpeedMultiplier() { speedMultiplier = 1f; }
    
    // --- 血量系统公共接口 ---
    
    /// <summary>
    /// 获取当前血量
    /// </summary>
    public int GetCurrentHealth() { return currentHealth; }
    
    /// <summary>
    /// 获取最大血量
    /// </summary>
    public int GetMaxHealth() { return maxHealth; }
    
    /// <summary>
    /// 获取血量百分比（0-1）
    /// </summary>
    public float GetHealthPercentage() { return maxHealth > 0 ? (float)currentHealth / maxHealth : 0f; }
    
    /// <summary>
    /// 是否死亡
    /// </summary>
    public bool IsDead() { return isDead; }
    
    /// <summary>
    /// 是否处于无敌状态
    /// </summary>
    public bool IsInvincible() { return isInvincible; }
    
    /// <summary>
    /// 是否正在冲刺
    /// </summary>
    public bool IsDashing() { return isDashing; }
    
    /// <summary>
    /// 设置重生位置（供外部调用，例如检查点）
    /// </summary>
    public void SetRespawnPosition(Vector3 position)
    {
        respawnPosition = position;
    }
    
    /// <summary>
    /// 治疗（恢复血量）
    /// </summary>
    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    /// <summary>
    /// 设置最大血量（并自动调整当前血量）
    /// </summary>
    public void SetMaxHealth(int newMaxHealth)
    {
        if (newMaxHealth <= 0) return;
        float healthPercentage = GetHealthPercentage();
        maxHealth = newMaxHealth;
        currentHealth = Mathf.RoundToInt(maxHealth * healthPercentage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
