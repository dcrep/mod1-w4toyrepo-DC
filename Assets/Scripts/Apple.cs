using UnityEngine;

public class Apple : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
        if (transform.position.y < -10f)
        {
            Destroy(gameObject);
        }
    }
}
