using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ZoneManager : MonoBehaviour
{
    public static ZoneManager Instance { get; private set; }

    // 컴포넌트를 직접 참조 (Awake에서 미리 찾아 놓음)
    public ZoneGenerator zoneGenerator { get; private set; }
    public ZoneRenderer zoneRenderer { get; private set; }
    public ZoneSelector zoneSelector { get; private set; }

    // 전체 구역 리스트 (런타임 시 Start에서 미리 담아놓고 이후엔 접근해서 사용만 하도록 캐싱)
    public List<ZoneData> ZoneList = new List<ZoneData>();

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

        // 같은 오브젝트에 붙어있는 스크립트들을 참조 (없으면 생성)
        zoneGenerator = GetComponent<ZoneGenerator>();
        zoneRenderer = GetComponent<ZoneRenderer>();
        zoneSelector = GetComponent<ZoneSelector>();
        if (zoneGenerator == null) zoneGenerator = gameObject.AddComponent<ZoneGenerator>();
        if (zoneRenderer == null) zoneRenderer = gameObject.AddComponent<ZoneRenderer>();
        if (zoneSelector == null) zoneSelector = gameObject.AddComponent<ZoneSelector>();
    }

    void OnEnable()
    {
        ZoneSelector.OnZoneSelected += HandleZoneSelected;
    }

    void OnDisable()
    {
        ZoneSelector.OnZoneSelected -= HandleZoneSelected;
    }

    private async void Start()
    {
        // 런타임 시작 시 ZoneData 미리 캐싱
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
            Debug.LogError("[ZoneManager 에러] 서버로부터 받은 데이터가 없습니다.");
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
        ZoneList = parser.ExtractZoneData(features);
        Debug.Log($"[ZoneManager] ZoneData 캐싱 완료: {ZoneList.Count}개");
    }
    #endregion


    // '구역 선택' 버튼 눌렀을 때 호출됨
    public void EnableZoneSelection()
    {
        // ZoneData가 캐싱되어 있는지 확인
        if (!NetworkManager.Instance.IsGeoJsonLoaded)
        {
            Debug.LogWarning("[ZoneManager 경고] ZoneData 데이터가 로드되지 않았습니다. 데이터를 먼저 로드합니다.");
            return;
        }

        // ZoneData가 캐싱되어 있다면 구역 전체 그리드로 지형에 표시
        zoneGenerator.VisualizeGrids();
        zoneGenerator.ShowGrids();
        Debug.Log("[ZoneManager] 그리드 표시 완료");
    }

    // 그리드 하나 선택했을 때 실행됨
    private void HandleZoneSelected(ZoneItem selectedItem)
    {
        // 1. 주변 그리드 숨기기
        zoneGenerator.HideOtherGrids(selectedItem);

        // 2. 선택된 그리드 Blink 시작
        StartCoroutine(zoneGenerator.BlinkAndLoad(selectedItem));
    }

}
