using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ZoneManager : MonoBehaviour
{
    public static ZoneManager Instance { get; private set; }

    // 전체 구역 리스트 (런타임 시 Start에서 미리 담아놓고 이후엔 접근해서 사용만 하도록 캐싱)
    public List<ZoneData> zoneList = new List<ZoneData>();

    public ZoneData selectedZone { get; private set; }  // 현재 선택된 zone

    // 구역 선택 시 발생 이벤트
    public static event Action OnRequestGridShow;




    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private async void Start()
    {
        // 런타임 시작 시 ZoneData 자동 캐싱
        await LoadAndCacheZoneData();
    }


    #region ``런타임 시작 시 연동된 API를 통해 ZoneData 받아와서 캐싱``
    public async Task LoadAndCacheZoneData()
    {
        // NetworkManager의 상태를 확인하여 중복 로드 방지
        if (NetworkManager.Instance.IsGeoJsonLoaded)
        {
            Debug.Log("[ZoneManager] 이미 데이터가 로드되어 있습니다.");
            return;
        }

        await FetchZoneData();
    }

    // 테스트용 코드
    // SimulationController에서 사용
    public async Task FetchZoneData()
    {
        // NetworkManager의 FetchZoneFromApi() 코루틴 결과를 Task로 변환하기 위해 필요
        // IEnumerator나 콜백 기반 API는 async/await와 직접 연결할 수 없으므로
        // 이렇게 Task 상자를 만들고 파싱 및 할당 작업이 끝나면 상자를 닫는다는 개념
        TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

        StartCoroutine(NetworkManager.Instance.FetchGeoJsonData((rawJson) =>
        {
            tcs.SetResult(rawJson);
        }));

        // 통신이 완료될 때까지 비동기로 대기
        string rawJsonText = await tcs.Task;

        if (string.IsNullOrEmpty(rawJsonText))
        {
            Debug.LogError("[ZoneManager] 서버로부터 받은 데이터가 없습니다.");
            return;
        }

        // 받아온 rawJsonText를 파싱하고 객체화
        DataParser parser = new DataParser();

        List<JObject> features = await Task.Run(() =>
        {
            List<JObject> list = new List<JObject>();
            JObject root = JObject.Parse(rawJsonText);
            foreach (JToken feat in (JArray)root["features"])
            {
                list.Add((JObject)feat);
            }
            return list;
        });

        // Data 추출 함수를 통해 객체화
        zoneList = parser.ExtractZoneData(features);

        // 로드 후 테스트 로그 출력 (재구성 완료 후 지우기)
        if (zoneList != null)
        {
            Debug.Log($"[ZoneManager] 캐싱 완료: {zoneList.Count}개 Zone");
            foreach (var zone in zoneList)
            {
                Debug.Log($"캐싱된 Zone: {zone.zoneId}, Temp: {zone.temperature}, 좌표수: {zone.polygon.GetLength(0)}");
            }
        }
    }
    #endregion


    #region ``BuildingSelector 관련 함수``
    // Zone 선택 이벤트 실행
    //public void SelectZone(ZoneData data)
    //{
    //    if (selectedZone != null)
    //    {
    //        OnZoneDeselected?.Invoke();  // 기존 선택 구역 있으면 해제
    //    }
    //    selectedZone = data;
    //    OnZoneSelected?.Invoke(data);
    //}

    //// Zone 선택 해제 이벤트 실행
    //public void DeselectZone()
    //{
    //    if (selectedZone == null)
    //        return;

    //    selectedZone = null;
    //    OnZoneDeselected?.Invoke();
    //}
    #endregion

    public void RequestGridShow()
    {
        // ZoneData가 캐싱되어 있는지 확인
        if (!NetworkManager.Instance.IsGeoJsonLoaded)
        {
            Debug.LogWarning("[ZoneManager] ZoneData 데이터가 로드되지 않았습니다. 데이터를 먼저 로드합니다.");
            return;
        }

        // ZoneData가 캐싱되어 있다면 구역 전체 그리드로 지형에 표시
        Debug.Log("[ZoneManager] RequestGridShow 시작");
        OnRequestGridShow?.Invoke();
    }
}
