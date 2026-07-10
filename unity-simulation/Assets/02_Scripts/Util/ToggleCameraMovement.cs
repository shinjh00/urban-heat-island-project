using CesiumForUnity;
using UnityEngine;

public class ToggleCameraMovement : MonoBehaviour
{
    private CesiumCameraController cameraController;
    private bool isEnabled;

    void Awake()
    {
        // 동일 오브젝트에 붙어있는 CesiumCameraController를 가져옵니다.
        cameraController = GetComponent<CesiumCameraController>();

        if (cameraController == null)
        {
            Debug.LogError("[ToggleCameraMovement] CesiumCameraController를 찾을 수 없습니다.");
        }
    }

    private void Start()
    {
        isEnabled = true;
        string status = isEnabled ? "ON" : "OFF";
        UIManager.Instance.CameraToggleText("ON");
    }

    void Update()
    {
        // F키를 눌렀을 때 토글
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleMovement();
        }
    }

    public void ToggleMovement()
    {
        if (cameraController == null) return;

        isEnabled = !isEnabled;

        cameraController.enableMovement = isEnabled;
        cameraController.enableRotation = isEnabled;

        string status = isEnabled ? "ON" : "OFF";
        UIManager.Instance.CameraToggleText(status);
    }
}
