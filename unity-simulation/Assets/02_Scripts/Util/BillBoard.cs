using UnityEngine;

// 오브젝트가 항상 카메라 시점을 따라 회전하는 유틸 컴포넌트
public class Billboard : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main == null) return;

        // 카메라와 같은 방향을 바라보게 → 텍스트가 항상 정면으로 읽힘
        transform.rotation = Camera.main.transform.rotation;
    }
}