using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatBed : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 进入玩家的AttackZone时被摧毁
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("OnTriggerEnter2D: " + other.tag);
        if (other.CompareTag("AttackZone"))
        {
            Destroy(gameObject);
        }
    }
}
