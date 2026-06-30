//using System;
//using UnityEngine;
//using UnityEngine.InputSystem;

//// 그리드 클릭 입력을 받아서 어떤 zone이 선택됐는지에 대한 상태 관리
//public class ZoneSelector : MonoBehaviour
//{
//    // 구역 선택 시 발생 이벤트 (ZoneManager, BuildingManager 등에서 구독하고 있으면 됨)
//    public static event Action<WeatherGridItem> OnZoneSelected;

//    // 그리드 깜빡거리는 코루틴
//    private Coroutine blinkRoutine;


//    void Update()
//    {
//        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
//        {
//            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
//            if (Physics.Raycast(ray, out RaycastHit hit))
//            {
//                WeatherGridItem gridItem = hit.collider.GetComponent<WeatherGridItem>();
//                if (gridItem != null)
//                {
//                    HandleSelection(gridItem, hit.collider.GetComponent<MeshRenderer>());
//                    OnZoneSelected?.Invoke(gridItem); // 상태 전달
//                }
//            }
//        }
//    }

//    private void HandleSelection(WeatherGridItem item, MeshRenderer mr)
//    {
//        // 기존 BlinkAndLoad 로직 호출 (이벤트 기반으로 건물 로드 연결)
//        if (blinkRoutine != null) StopCoroutine(blinkRoutine);
//        blinkRoutine = StartCoroutine(BlinkAndLoad(item, mr));
//    }

//    private IEnumerator BlinkAndLoad(WeatherGridItem grid, MeshRenderer mr)
//    {
//        // 1️⃣ Blink (2초간 0.2초 간격)
//        float duration = 2f;
//        float interval = 0.2f;
//        float elapsed = 0f;

//        while (elapsed < duration)
//        {
//            mr.enabled = !mr.enabled;
//            yield return new WaitForSeconds(interval);
//            elapsed += interval;
//        }
//        mr.enabled = true;  // 끝나면 켜진 상태로

//        // 2️⃣ 중심점 계산
//        (double lon, double lat) = grid.GetCenter();
//        Debug.Log($"[BlinkAndLoad] 중심점 계산 완료 — ({lon:F6}, {lat:F6})");

//        // 3️⃣ 750m 건물 로드
//        BuildingManager.Instance.FocusOnGrid(lon, lat);
//    }
//}
