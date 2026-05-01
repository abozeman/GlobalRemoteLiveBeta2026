using UnityEngine;

public class ERLManualPlacement : MonoBehaviour
{

    public GameObject manualPlacementPrefab;
    public GameObject preview;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        preview = Instantiate(manualPlacementPrefab, Vector3.zero, Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = new Ray(OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch), OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch) * Vector3.forward);

        if(Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            preview.transform.position = hitInfo.point;
            //preview.transform.rotation = Quaternion.LookRotation(hitInfo.normal);
            preview.transform.rotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
        }

        if(OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
        {
            Instantiate(manualPlacementPrefab, preview.transform.position, preview.transform.rotation);
        }

    }
}
