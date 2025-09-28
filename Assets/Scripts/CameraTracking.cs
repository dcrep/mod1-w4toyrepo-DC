using UnityEngine;

public class CameraTracking : MonoBehaviour
{
    [SerializeField]
    GameObject objectTracking;

    public void SetObjectTracking(GameObject obj)
    {
        objectTracking = obj;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void LateUpdate()
    {
        if (objectTracking != null)
        {
            Vector3 newTransformPosition = objectTracking.transform.position;
            newTransformPosition.z = this.transform.position.z;
            this.transform.position = newTransformPosition;
        }
    }
}
