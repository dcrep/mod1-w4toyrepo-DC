using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppleTree : MonoBehaviour
{
    [Header("Inscribed")]
    public GameObject applePrefab;
    public float speed = 2f;
    public float leftEdge = -5f;
    public float rightEdge = 5f;
    public float appleDropDelay = 1f;

    private int applesDropped = 0;
    private float bombDropChance = 0.10f;
    public int maxApples = 20;

    bool treePause = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ReStart();        
    }

    void ReStart()
    {
        treePause = false;
         // Start dropping apples                                          
        Invoke(nameof(DropApple), appleDropDelay);
    }

    void DropApple()
    {
        GameObject apple = Instantiate<GameObject>(applePrefab);
        Vector3 applePos = transform.position;
        applePos.z = 0;
        apple.transform.position = applePos;

        applesDropped++;
        if (applesDropped == maxApples)
        {          
            treePause = true;
            Destroy(this.gameObject);
            return;
        }        
        Invoke(nameof(DropApple), appleDropDelay);
    }

    // Update is called once per frame
    void Update()
    {
        if (treePause)
        {
            if (Camera.main.transform.position.x + rightEdge < transform.position.x)
            {
                treePause = false;
            }
            else
            {
                return;
            }
        }

        Vector3 pos = transform.position;
        pos.x += speed * Time.deltaTime;
        transform.position = pos;

        if (pos.x > Camera.main.transform.position.x + rightEdge)
        {
            //speed = -Mathf.Abs(speed);  // move left
            treePause = true;
        }
    }

    /*private void FixedUpdate()
    {

    }
    */
}
