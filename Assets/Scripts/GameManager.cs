using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("玩家设置")]
    public PlatformerMovement player; // 玩家引用（可在Inspector中指定）
    public string playerTag = "Player"; // 玩家标签
    
    [Header("猫窝设置")]
    public string catBedTag = "RespawnPoint"; // 猫窝标签（如果使用Tag）
    
    [Header("死亡UI设置")]
    [SerializeField]public CanvasGroup deathUI; // 死亡UI的Canvas Group组件（可在Inspector中指定）
    [SerializeField]public GameObject deathUIGameObject; // 死亡UI的GameObject（如果未指定Canvas Group，可通过此方式指定）
    
    [Header("血量UI设置")]
    [SerializeField]public Image healthBarImage; // 血量条Image组件（Fill Amount类型）
    [SerializeField]public GameObject healthBarGameObject; // 血量条GameObject（如果未指定Image，可通过此方式指定）
    [Header("血量跃动效果设置")]
    [SerializeField]public float bounceScale = 1.2f; // 跃动时的缩放倍数
    [SerializeField]public float bounceDuration = 0.2f; // 跃动动画持续时间
    
    private Coroutine healthBarBounceCoroutine; // 当前运行的跃动效果协程
    
    [Header("BGM设置")]
    [SerializeField] private AudioSource bgmAudioSource; // BGM音频源（如果未指定会自动创建）
    [SerializeField] private GameObject bgmAudioSourceGameObject; // BGM音频源GameObject（如果未指定AudioSource，可通过此方式指定）
    
    [Header("区域1 BGM设置")]
    [SerializeField] private AudioClip zone1InitialBGM; // 区域1初始BGM
    [SerializeField] private AudioClip[] zone1RandomBGMs; // 区域1随机BGM列表
    [SerializeField] private Vector2 zone1RandomInterval = new Vector2(3f, 8f); // 区域1随机播放间隔（最小-最大秒数）
    
    [Header("区域2 BGM设置")]
    [SerializeField] private AudioClip[] zone2RandomBGMs; // 区域2随机BGM列表
    [SerializeField] private Vector2 zone2RandomInterval = new Vector2(3f, 8f); // 区域2随机播放间隔（最小-最大秒数）
    
    [Header("区域3 BGM设置")]
    [SerializeField] private AudioClip zone3LoopBGM; // 区域3循环BGM
    
    private int currentZone = 0; // 当前所在区域（0=无区域，1/2/3=对应区域）
    private Coroutine zone1BGMCoroutine; // 区域1的BGM协程
    private Coroutine zone2BGMCoroutine; // 区域2的BGM协程
    private Coroutine zone3BGMCoroutine; // 区域3的BGM协程
    
    void Start()
    {
        // 如果未在Inspector中指定玩家，自动查找
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
            {
                player = playerObj.GetComponent<PlatformerMovement>();
            }
            
            if (player == null)
            {
                Debug.LogWarning("[GameManager] 未找到玩家对象！请确保场景中有Tag为'Player'的GameObject，或在Inspector中手动指定玩家引用。");
            }
        }
        
        // 初始化死亡UI
        InitializeDeathUI();
        
        // 初始化血量UI
        InitializeHealthUI();
        
        // 订阅玩家事件
        SubscribeToPlayerEvents();
        
        // 初始化BGM系统
        InitializeBGM();
        
        // 游戏开始时自动播放区域1的初始BGM
        StartZone1BGM();
    }
    
    /// <summary>
    /// 初始化死亡UI
    /// </summary>
    void InitializeDeathUI()
    {
        // 如果未指定Canvas Group，尝试从GameObject获取
        if (deathUI == null && deathUIGameObject != null)
        {
            deathUI = deathUIGameObject.GetComponent<CanvasGroup>();
        }
        
        // 如果仍然没有找到，尝试通过名称查找
        if (deathUI == null)
        {
            GameObject deathUIObj = GameObject.Find("DeathUI");
            if (deathUIObj != null)
            {
                deathUI = deathUIObj.GetComponent<CanvasGroup>();
            }
        }
        
        // 初始化死亡UI为隐藏状态（Alpha = 0）
        if (deathUI != null)
        {
            deathUI.alpha = 0f;
            deathUI.interactable = false;
            deathUI.blocksRaycasts = false;
        }
        else
        {
            Debug.LogWarning("[GameManager] 未找到死亡UI！请确保场景中有Canvas Group组件，或在Inspector中手动指定死亡UI引用。");
        }
    }
    
    /// <summary>
    /// 初始化血量UI
    /// </summary>
    void InitializeHealthUI()
    {
        // 如果未指定Image，尝试从GameObject获取
        if (healthBarImage == null && healthBarGameObject != null)
        {
            healthBarImage = healthBarGameObject.GetComponent<Image>();
        }
        
        // 如果仍然没有找到，尝试通过名称查找
        if (healthBarImage == null)
        {
            GameObject healthBarObj = GameObject.Find("HealthBar");
            if (healthBarObj != null)
            {
                healthBarImage = healthBarObj.GetComponent<Image>();
            }
        }
        
        // 初始化血量UI
        if (healthBarImage != null)
        {
            // 确保Image类型为Filled
            if (healthBarImage.type != Image.Type.Filled)
            {
                healthBarImage.type = Image.Type.Filled;
                Debug.LogWarning("[GameManager] 血量条Image类型已自动设置为Filled");
            }
            
            // 初始化Fill Amount为1（满血）
            healthBarImage.fillAmount = 1f;
        }
        else
        {
            Debug.LogWarning("[GameManager] 未找到血量条Image！请确保场景中有Image组件，或在Inspector中手动指定血量条引用。");
        }
    }
    
    /// <summary>
    /// 订阅玩家事件
    /// </summary>
    void SubscribeToPlayerEvents()
    {
        if (player != null)
        {
            player.OnPlayerDeath += OnPlayerDeath;
            player.OnPlayerRespawn += OnPlayerRespawn;
            player.OnHealthChanged += OnHealthChanged;
        }
    }
    
    /// <summary>
    /// 取消订阅玩家事件（防止内存泄漏）
    /// </summary>
    void OnDestroy()
    {
        if (player != null)
        {
            player.OnPlayerDeath -= OnPlayerDeath;
            player.OnPlayerRespawn -= OnPlayerRespawn;
            player.OnHealthChanged -= OnHealthChanged;
        }
    }
    
    /// <summary>
    /// 玩家死亡时的回调
    /// </summary>
    void OnPlayerDeath()
    {
        ShowDeathUI();
    }
    
    /// <summary>
    /// 玩家重生时的回调
    /// </summary>
    void OnPlayerRespawn()
    {
        HideDeathUI();
    }
    
    /// <summary>
    /// 显示死亡UI
    /// </summary>
    void ShowDeathUI()
    {
        if (deathUI != null)
        {
            deathUI.alpha = 1f;
            deathUI.interactable = true;
            deathUI.blocksRaycasts = true;
        }
    }
    
    /// <summary>
    /// 隐藏死亡UI
    /// </summary>
    void HideDeathUI()
    {
        if (deathUI != null)
        {
            deathUI.alpha = 0f;
            deathUI.interactable = false;
            deathUI.blocksRaycasts = false;
        }
    }
    
    /// <summary>
    /// 玩家血量变化时的回调
    /// </summary>
    /// <param name="currentHealth">当前血量</param>
    /// <param name="maxHealth">最大血量</param>
    void OnHealthChanged(int currentHealth, int maxHealth)
    {
        UpdateHealthBar(currentHealth, maxHealth);
    }
    
    /// <summary>
    /// 更新血量条显示
    /// </summary>
    /// <param name="currentHealth">当前血量</param>
    /// <param name="maxHealth">最大血量</param>
    void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (healthBarImage == null) return;
        
        // 计算Fill Amount：每滴血对应0.2，所以 currentHealth * 0.2
        float targetFillAmount = currentHealth * 0.2f;
        targetFillAmount = Mathf.Clamp01(targetFillAmount); // 确保在0-1范围内
        
        // 更新Fill Amount
        healthBarImage.fillAmount = targetFillAmount;
        
        // 停止之前的跃动效果（如果存在）
        if (healthBarBounceCoroutine != null)
        {
            StopCoroutine(healthBarBounceCoroutine);
        }
        
        // 触发新的跃动效果
        healthBarBounceCoroutine = StartCoroutine(HealthBarBounceEffect());
    }
    
    /// <summary>
    /// 血量条跃动效果协程
    /// </summary>
    IEnumerator HealthBarBounceEffect()
    {
        if (healthBarImage == null) yield break;
        
        RectTransform rectTransform = healthBarImage.rectTransform;
        if (rectTransform == null) yield break;
        
        Vector3 originalScale = rectTransform.localScale;
        float elapsedTime = 0f;
        
        // 放大阶段
        while (elapsedTime < bounceDuration / 2f)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / (bounceDuration / 2f);
            float scale = Mathf.Lerp(1f, bounceScale, progress);
            rectTransform.localScale = originalScale * scale;
            yield return null;
        }
        
        // 恢复阶段
        elapsedTime = 0f;
        while (elapsedTime < bounceDuration / 2f)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / (bounceDuration / 2f);
            float scale = Mathf.Lerp(bounceScale, 1f, progress);
            rectTransform.localScale = originalScale * scale;
            yield return null;
        }
        
        // 确保最终缩放为原始值
        rectTransform.localScale = originalScale;
        
        // 清空协程引用
        healthBarBounceCoroutine = null;
    }

    void Update()
    {
        // 检测J键（开发者使用）
        if (Input.GetKeyDown(KeyCode.J))
        {
            RespawnPlayer();
        }
    }

    /// <summary>
    /// 重生按钮（供UI按钮调用）
    /// </summary>
    public void RespawnButton()
    {
        RespawnPlayer();
    }
    
    /// <summary>
    /// 重生玩家到最近的猫窝位置
    /// </summary>
    public void RespawnPlayer()
    {
        if (player == null)
        {
            Debug.LogWarning("[GameManager] 无法重生：未找到玩家对象！");
            return;
        }
        
        // 查找最近的猫窝
        Vector3? nearestCatBedPosition = FindNearestCatBedPosition();
        
        if (nearestCatBedPosition.HasValue)
        {
            // 在最近的猫窝位置重生
            player.Respawn(nearestCatBedPosition.Value);
            Debug.Log($"[GameManager] 玩家在猫窝位置重生：{nearestCatBedPosition.Value}");
        }
        else
        {
            // 如果没有找到猫窝，使用默认重生位置
            player.Respawn();
            Debug.LogWarning("[GameManager] 未找到猫窝，使用默认重生位置");
        }
    }
    
    /// <summary>
    /// 查找最近的猫窝位置
    /// </summary>
    /// <returns>最近的猫窝位置，如果没有找到则返回null</returns>
    Vector3? FindNearestCatBedPosition()
    {
        if (player == null) return null;
        
        // 方法1：通过Tag查找
        GameObject[] catBedsByTag = GameObject.FindGameObjectsWithTag(catBedTag);
        
        // 方法2：通过组件类型查找（更可靠）
        CatBed[] catBedsByComponent = FindObjectsOfType<CatBed>();
        
        // 合并两种方法的结果
        List<GameObject> allCatBeds = new List<GameObject>();
        
        // 添加通过Tag找到的
        foreach (GameObject bed in catBedsByTag)
        {
            if (bed != null && !allCatBeds.Contains(bed))
            {
                allCatBeds.Add(bed);
            }
        }
        
        // 添加通过组件找到的
        foreach (CatBed bed in catBedsByComponent)
        {
            if (bed != null && bed.gameObject != null && !allCatBeds.Contains(bed.gameObject))
            {
                allCatBeds.Add(bed.gameObject);
            }
        }
        
        // 如果没有找到任何猫窝
        if (allCatBeds.Count == 0)
        {
            return null;
        }
        
        // 找到最近的猫窝
        Vector3 playerPosition = player.transform.position;
        GameObject nearestCatBed = null;
        float nearestDistance = float.MaxValue;
        
        foreach (GameObject catBed in allCatBeds)
        {
            if (catBed == null) continue;
            
            float distance = Vector3.Distance(playerPosition, catBed.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestCatBed = catBed;
            }
        }
        
        if (nearestCatBed != null)
        {
            return nearestCatBed.transform.position;
        }
        
        return null;
    }

    public void GameExitButton()
    {
        Application.Quit();
    }
    
    #region BGM管理
    
    /// <summary>
    /// 初始化BGM系统
    /// </summary>
    void InitializeBGM()
    {
        // 如果未指定AudioSource，尝试从GameObject获取
        if (bgmAudioSource == null && bgmAudioSourceGameObject != null)
        {
            bgmAudioSource = bgmAudioSourceGameObject.GetComponent<AudioSource>();
        }
        
        // 如果仍然没有找到，尝试通过名称查找或创建
        if (bgmAudioSource == null)
        {
            GameObject bgmObj = GameObject.Find("BGMAudioSource");
            if (bgmObj != null)
            {
                bgmAudioSource = bgmObj.GetComponent<AudioSource>();
            }
            else
            {
                // 创建新的GameObject和AudioSource
                bgmObj = new GameObject("BGMAudioSource");
                bgmAudioSource = bgmObj.AddComponent<AudioSource>();
                bgmAudioSource.loop = false; // 默认不循环，由各区域逻辑控制
                bgmAudioSource.playOnAwake = false;
            }
        }
        
        if (bgmAudioSource == null)
        {
            Debug.LogWarning("[GameManager] 未找到BGM AudioSource！已自动创建。");
        }
    }
    
    /// <summary>
    /// 玩家进入BGM区域时的回调
    /// </summary>
    /// <param name="zoneNumber">区域编号</param>
    public void OnPlayerEnterBGMZone(int zoneNumber)
    {
        // 如果已经在同一区域，不重复处理
        if (currentZone == zoneNumber) return;
        
        // 停止当前区域的BGM
        StopCurrentZoneBGM();
        
        // 切换到新区域
        currentZone = zoneNumber;
        
        // 根据区域播放对应的BGM
        switch (zoneNumber)
        {
            case 1:
                StartZone1BGM();
                break;
            case 2:
                StartZone2BGM();
                break;
            case 3:
                StartZone3BGM();
                break;
        }
    }
    
    /// <summary>
    /// 玩家离开BGM区域时的回调
    /// </summary>
    /// <param name="zoneNumber">区域编号</param>
    public void OnPlayerExitBGMZone(int zoneNumber)
    {
        // 如果离开的是当前区域，停止BGM
        if (currentZone == zoneNumber)
        {
            StopCurrentZoneBGM();
            currentZone = 0;
        }
    }
    
    /// <summary>
    /// 停止当前区域的BGM
    /// </summary>
    void StopCurrentZoneBGM()
    {
        // 停止所有BGM协程
        if (zone1BGMCoroutine != null)
        {
            StopCoroutine(zone1BGMCoroutine);
            zone1BGMCoroutine = null;
        }
        if (zone2BGMCoroutine != null)
        {
            StopCoroutine(zone2BGMCoroutine);
            zone2BGMCoroutine = null;
        }
        if (zone3BGMCoroutine != null)
        {
            StopCoroutine(zone3BGMCoroutine);
            zone3BGMCoroutine = null;
        }
        
        // 停止音频播放
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Stop();
        }
    }
    
    /// <summary>
    /// 开始区域1的BGM播放
    /// </summary>
    void StartZone1BGM()
    {
        if (zone1BGMCoroutine != null)
        {
            StopCoroutine(zone1BGMCoroutine);
        }
        zone1BGMCoroutine = StartCoroutine(Zone1BGMPlayback());
    }
    
    /// <summary>
    /// 区域1的BGM播放协程：先播放初始BGM，然后随机间隔播放随机BGM
    /// </summary>
    IEnumerator Zone1BGMPlayback()
    {
        if (bgmAudioSource == null) yield break;
        
        // 1. 播放初始BGM
        if (zone1InitialBGM != null)
        {
            bgmAudioSource.clip = zone1InitialBGM;
            bgmAudioSource.loop = false;
            bgmAudioSource.Play();
            
            // 等待初始BGM播放完成
            while (bgmAudioSource.isPlaying && currentZone == 1)
            {
                yield return null;
            }
        }
        
        // 2. 如果区域1的随机BGM列表为空，结束
        if (zone1RandomBGMs == null || zone1RandomBGMs.Length == 0)
        {
            Debug.LogWarning("[GameManager] 区域1的随机BGM列表为空！");
            yield break;
        }
        
        // 3. 循环随机播放BGM
        while (currentZone == 1)
        {
            // 随机选择一个BGM
            AudioClip randomBGM = zone1RandomBGMs[Random.Range(0, zone1RandomBGMs.Length)];
            
            if (randomBGM != null)
            {
                bgmAudioSource.clip = randomBGM;
                bgmAudioSource.loop = false;
                bgmAudioSource.Play();
                
                // 等待BGM播放完成
                while (bgmAudioSource.isPlaying && currentZone == 1)
                {
                    yield return null;
                }
            }
            
            // 如果还在区域1，等待随机间隔后播放下一首
            if (currentZone == 1)
            {
                float waitTime = Random.Range(zone1RandomInterval.x, zone1RandomInterval.y);
                yield return new WaitForSeconds(waitTime);
            }
        }
        
        zone1BGMCoroutine = null;
    }
    
    /// <summary>
    /// 开始区域2的BGM播放
    /// </summary>
    void StartZone2BGM()
    {
        if (zone2BGMCoroutine != null)
        {
            StopCoroutine(zone2BGMCoroutine);
        }
        zone2BGMCoroutine = StartCoroutine(Zone2BGMPlayback());
    }
    
    /// <summary>
    /// 区域2的BGM播放协程：随机间隔播放随机BGM
    /// </summary>
    IEnumerator Zone2BGMPlayback()
    {
        if (bgmAudioSource == null) yield break;
        
        // 如果区域2的随机BGM列表为空，结束
        if (zone2RandomBGMs == null || zone2RandomBGMs.Length == 0)
        {
            Debug.LogWarning("[GameManager] 区域2的随机BGM列表为空！");
            yield break;
        }
        
        // 循环随机播放BGM
        while (currentZone == 2)
        {
            // 随机选择一个BGM
            AudioClip randomBGM = zone2RandomBGMs[Random.Range(0, zone2RandomBGMs.Length)];
            
            if (randomBGM != null)
            {
                bgmAudioSource.clip = randomBGM;
                bgmAudioSource.loop = false;
                bgmAudioSource.Play();
                
                // 等待BGM播放完成
                while (bgmAudioSource.isPlaying && currentZone == 2)
                {
                    yield return null;
                }
            }
            
            // 如果还在区域2，等待随机间隔后播放下一首
            if (currentZone == 2)
            {
                float waitTime = Random.Range(zone2RandomInterval.x, zone2RandomInterval.y);
                yield return new WaitForSeconds(waitTime);
            }
        }
        
        zone2BGMCoroutine = null;
    }
    
    /// <summary>
    /// 开始区域3的BGM播放
    /// </summary>
    void StartZone3BGM()
    {
        if (zone3BGMCoroutine != null)
        {
            StopCoroutine(zone3BGMCoroutine);
        }
        
        if (bgmAudioSource == null || zone3LoopBGM == null)
        {
            Debug.LogWarning("[GameManager] 区域3的BGM未设置！");
            return;
        }
        
        // 区域3循环播放
        bgmAudioSource.clip = zone3LoopBGM;
        bgmAudioSource.loop = true;
        bgmAudioSource.Play();
        
        // 启动一个协程来监控区域切换
        zone3BGMCoroutine = StartCoroutine(Zone3BGMPlayback());
    }
    
    /// <summary>
    /// 区域3的BGM播放协程：监控区域切换
    /// </summary>
    IEnumerator Zone3BGMPlayback()
    {
        // 只要还在区域3，就一直循环播放（由AudioSource的loop属性控制）
        while (currentZone == 3 && bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            yield return null;
        }
        
        zone3BGMCoroutine = null;
    }
    
    #endregion
}
