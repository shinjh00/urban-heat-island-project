using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// 그리드 클릭 입력을 받아서 어떤 zone이 선택됐는지에 대한 상태 관리
public class ZoneSelector : MonoBehaviour
{
    // 구역 선택 시 발생 이벤트 (ZoneManager에서 구독)
    public static event Action<ZoneItem> OnZoneSelected;

    // 그리드 깜빡거리는 코루틴
    private Coroutine blinkRoutine;

    void Update()
    {
        // 마우스 입력 확인
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            // 레이캐스트
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // 아이템 컴포넌트 확인
                ZoneItem item = hit.collider.GetComponent<ZoneItem>();
                if (item != null)
                {
                    // 이벤트 호출 및 로직 수행
                    //HandleSelection(item, hit.collider.GetComponent<MeshRenderer>());
                    OnZoneSelected?.Invoke(item); // ZoneManager에 상태 전달
                }
            }
        }
    }
}
