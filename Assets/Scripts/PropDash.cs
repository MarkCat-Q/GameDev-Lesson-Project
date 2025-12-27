using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PropDash : MonoBehaviour
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
