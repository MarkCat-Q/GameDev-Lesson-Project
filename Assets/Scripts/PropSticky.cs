using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// 粘性道具-触碰时消失并赋予玩家爬墙能力
// 爬墙能力搁置
public class PropSticky : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // 上下小范围跃动
        transform.position = new Vector3(
            transform.position.x, 
            transform.position.y + Mathf.Sin(Time.time * 2f) * 0.0005f,
            transform.position.z);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // move.cs会通过检测组件类型来获得能力
            Destroy(gameObject);
        }
    }
}
