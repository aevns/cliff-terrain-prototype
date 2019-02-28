using UnityEngine;

// (Just a modified sample script for camera behaviour)

public class PlayerFollow : MonoBehaviour
{

    // The target we are following
    [SerializeField]
    private Transform target;
    // The distance in the x-z plane to the target
    [SerializeField]
    private float distance = 10.0f;
    [SerializeField]
    private float maxDistance = 40.0f;
    // the height we want the camera to be above the target
    [SerializeField]
    private float height = 5.0f;
    [SerializeField]
    private float maxHeight = 20.0f;
    [SerializeField]
    private float rotationDamping;
    [SerializeField]
    private float heightDamping;
    [SerializeField]
    private float switchTimeout = 1f;
    [SerializeField]
    private float zoomSpeed = 20f;

    private float switchTimer;

    private void Update()
    {
        float zoom = Input.GetAxis("Mouse ScrollWheel");

        if (zoom != 0)
        {
            height *= 1 - zoomSpeed * Time.deltaTime * zoom;
            distance *= 1 - zoomSpeed * Time.deltaTime * zoom;

            height = Mathf.Min(height, maxHeight);
            distance = Mathf.Min(distance, maxDistance);
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        // Follow a MoveTo AI if we have no target
        if (!target)
        {
            MoveTo[] agents = FindObjectsOfType<MoveTo>();
            if (agents.Length > 0)
                target = agents[Mathf.FloorToInt(Random.value * agents.Length) % agents.Length].transform;
            return;
        }

        // Calculate the current rotation angles
        var wantedRotationAngle = target.eulerAngles.y;
        var wantedHeight = target.position.y + height;

        var currentRotationAngle = transform.eulerAngles.y;
        var currentHeight = transform.position.y;

        // Damp the rotation around the y-axis
        currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, rotationDamping * Time.deltaTime);

        // Damp the height
        currentHeight = Mathf.Lerp(currentHeight, wantedHeight, heightDamping * Time.deltaTime);

        // Convert the angle into a rotation
        var currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);

        // Set the position of the camera on the x-z plane to:
        // distance meters behind the target
        transform.position = target.position;
        transform.position -= currentRotation * Vector3.forward * distance;

        // Set the height of the camera
        transform.position = new Vector3(transform.position.x, currentHeight, transform.position.z);

        // Account for walls by choosing a new location to view from at random
        if (switchTimer < Time.time)
        {
            RaycastHit hitInfo;
            Vector3 newpos = transform.position;
            int k = 0;
            while (k < 20 && Physics.Raycast(target.position, (newpos - target.position).normalized, out hitInfo, (newpos - target.position).magnitude))
            {
                k++;
                newpos = target.position + Quaternion.Euler(0f, Random.value * 360f, 0f) * (transform.position - target.position);
                transform.LookAt(target);
                Debug.DrawRay(hitInfo.point, hitInfo.normal, Color.red, 20f);
                switchTimer = Time.time + switchTimeout;
            }
            if (k < 20)
                transform.position = newpos;
        }
        // Always look at the target
        transform.LookAt(target);
    }
}