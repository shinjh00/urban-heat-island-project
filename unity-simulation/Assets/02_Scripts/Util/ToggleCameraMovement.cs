using CesiumForUnity;
using UnityEngine;
using UnityEngine.EventSystems;

public class ToggleCameraMovement : MonoBehaviour
{
    private CesiumCameraController cameraController;
    private bool isEnabled = true;

    void Awake()
    {
        // 동일 오브젝트에 붙어있는 CesiumCameraController를 가져옵니다.
        cameraController = GetComponent<CesiumCameraController>();

        if (cameraController == null)
        {
            Debug.LogError("[ToggleCameraMovement] CesiumCameraController를 찾을 수 없습니다.");
        }
    }

    void Start()
    {
        // 게임 시작 시 마우스 커서를 중앙에 고정하고 숨김
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;

        //EventSystem.current.SetSelectedGameObject(null);
    }

    void Update()
    {
        // 스페이스바를 눌렀을 때 토글
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

        // 커서 상태 제어 추가
        //if (isEnabled)
        //{
        //    Cursor.lockState = CursorLockMode.Locked; // 카메라 회전 활성화 시 커서 잠금
        //    Cursor.visible = false;
        //}
        //else
        //{
        //    Cursor.lockState = CursorLockMode.None;   // UI 클릭을 위해 잠금 해제
        //    Cursor.visible = true;
        //}

        string status = isEnabled ? "활성화됨" : "비활성화됨";
        Debug.Log($"[ToggleCameraMovement] 카메라 움직임 {status}");
    }
}
