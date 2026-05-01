using UnityEngine;

public class PanelPlacement : MonoBehaviour
{

    //private GameObject currentPanel;
    public GameObject newPanelPrefab;
    public float distance = 1.5f;
    private Transform centerEye;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Start()
    {
        if (centerEye == null) return;

        // Find the camera rig in the scene
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();

        if (rig != null)
        {
            centerEye = rig.centerEyeAnchor;
            Debug.Log("Found Center Eye at: " + centerEye.position);


            ReplaceAndCenterPanel();
        }
    }

    public void ReplaceAndCenterPanel()
    {
        // 1. Destroy or Disable old panel
        //if (currentPanel != null) Destroy(currentPanel);

        //// 2. Instantiate new panel
        Instantiate(newPanelPrefab);

        // 3. Get User Position and Direction
        //Transform cameraTransform = Camera.main.transform;
        Vector3 targetPosition = centerEye.position + (centerEye.forward * distance);

        // 4. Apply Position and Rotate to Face User
        transform.position = targetPosition;
        transform.LookAt(new Vector3(centerEye.position.x, targetPosition.y, centerEye.position.z));
        transform.Rotate(0, 180, 0); // Correct for UI facing away
    }
}
