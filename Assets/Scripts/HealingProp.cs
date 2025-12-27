using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// 回血道具-触碰时消失并回血
// 回血功能搁置
public class HealingProp : MonoBehaviour
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
            Destroy(gameObject);
        }
    }
}
