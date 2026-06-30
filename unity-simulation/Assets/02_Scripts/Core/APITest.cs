using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class api_test : MonoBehaviour
{
    // [싱글톤] 전역에서 이 클래스의 모든 데이터에 단 한 줄로 접근할 수 있는 공유 허브입니다.
    public static api_test Instance { get; private set; }

    [Header("플라스크 서버 설정")]
    [SerializeField] string apiServerIP = "127.0.0.1";
    [SerializeField] string apiTargetTime = "202408112300";

    #region 데이터 구조 선언 (기상 격자, Bounds)

    [Serializable]
    public class FeatureData
    {
        public int id;
        public double temperature;
        public List<double[]> polygonPoints; //: 경도, [1]: 위도
    }

    //  [새로운 구조체] Bounds 전용 API 데이터를 보관하기 위한 C# 클래스입니다.
    [Serializable]
    public class BoundsData
    {
        public double minLon;
        public double maxLon;
        public double minLat;
        public double maxLat;
    }

    // JSON 파싱 우회용 내부 껍데기 클래스
    [Serializable]
    private class BoundsWrapper
    {
        public BoundsData bounds;
    }

    #endregion

    #region [전역 마스터 객체 저장소] 다른 파일에서 바로 조회해서 쓰는 서랍장

    // 1. 기존 GeoJSON 기상 격자 데이터 저장소
    public List<FeatureData> CachedWeatherGrids { get; private set; } = new List<FeatureData>();

    // 2. 새로 추가된 PNG 데칼 이미지 텍스처 저장소 (Cesium 투사 연출용 등으로 사용)
    public Texture2D CachedDecalTexture { get; private set; }

    // 3. 새로 추가된 Bounds 영역 경계 데이터 저장소 (double 기반 정밀 좌표)
    public BoundsData CachedBounds { get; private set; }

    // 4. 모든 데이터가 완벽하게 들어왔는지 확인할 수 있는 개별 상태 플래그
    public bool IsGeoJsonLoaded { get; private set; } = false;
    public bool IsPngLoaded { get; private set; } = false;
    public bool IsBoundsLoaded { get; private set; } = false;

    // 다른 파일에서 "전체 로딩이 다 끝났니?"라고 물어볼 때 쓰는 마스터 상태 플래그
    public bool IsAllWeatherDataLoaded => IsGeoJsonLoaded && IsPngLoaded && IsBoundsLoaded;

    #endregion

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        //  [병렬 가동 이벤트] 시작하자마자 3개의 다운로드 작업을 대기 줄 없이 "동시에" 출발시킵니다.
        StartCoroutine(FetchGeoJsonData());
        StartCoroutine(FetchPngDecalImage());
        StartCoroutine(FetchBoundsData());
    }

    #region 1. 기존 GeoJSON 날씨 격자 다운로드 코루틴

    private IEnumerator FetchGeoJsonData()
    {
        string requestUrl = $"http://{apiServerIP}:5000/api/weather/mapo-decal.geojson?tm={apiTargetTime}";
        UnityWebRequest request = UnityWebRequest.Get(requestUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($" [서버 에러] GeoJSON 날씨 격자 로드 실패: {request.error}");
            yield break;
        }

        string rawJsonText = request.downloadHandler.text;
        List<FeatureData> parsedResults = new List<FeatureData>();

        try
        {
            string[] featureBlocks = rawJsonText.Split(new string[] { "{\"geometry\"" }, StringSplitOptions.RemoveEmptyEntries);
            if (featureBlocks.Length <= 1) yield break;

            int currentGridId = 1;
            for (int i = 1; i < featureBlocks.Length; i++)
            {
                string block = featureBlocks[i];

                int valueIdx = block.IndexOf("\"value\":");
                if (valueIdx < 0) continue;
                string valueSub = block.Substring(valueIdx + 8);
                int endBrace = valueSub.IndexOf("}");
                if (endBrace < 0) continue;
                string finalValueStr = valueSub.Substring(0, endBrace).Replace("}", "").Trim();
                double parsedTemp = double.Parse(finalValueStr, System.Globalization.CultureInfo.InvariantCulture);

                int coordStart = block.IndexOf("[[[");
                int coordEnd = block.IndexOf("]]]");
                if (coordStart < 0 || coordEnd < 0) continue;
                string coordText = block.Substring(coordStart + 3, coordEnd - coordStart - 3);
                string[] points = coordText.Split(new string[] { "],[" }, StringSplitOptions.None);

                List<double[]> geoPointsList = new List<double[]>();
                for (int j = 0; j < points.Length; j++)
                {
                    string[] raw = points[j].Replace("[", "").Replace("]", "").Split(',');
                    if (raw.Length < 2) continue;

                    double lon = double.Parse(raw[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                    double lat = double.Parse(raw[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                    geoPointsList.Add(new double[] { lon, lat });
                }

                FeatureData singleGrid = new FeatureData();
                singleGrid.id = currentGridId;
                singleGrid.temperature = parsedTemp;
                singleGrid.polygonPoints = geoPointsList;
                parsedResults.Add(singleGrid);

                currentGridId++;
            }

            CachedWeatherGrids = parsedResults;
            IsGeoJsonLoaded = true;
            Debug.Log($" [1/3 완료] GeoJSON 기상 격자 {CachedWeatherGrids.Count}개 보관 완료.");
            //  [연동 추가 코드] 다운로드가 끝나는 즉시 지면에 오브젝트를 그립니다!
            //FindFirstObjectByType<WeatherGridVisualizer>().VisualizeWeatherGrids();
        }
        catch (Exception e)
        {
            Debug.LogError($" [GeoJSON 파서 에러] 예외 발생: {e.Message}");
        }
    }

    #endregion

    #region 2.  PNG 데칼 이미지 다운로드 코루틴

    private IEnumerator FetchPngDecalImage()
    {
        string requestUrl = $"http://{apiServerIP}:5000/api/weather/mapo-decal.png?source=kma&tm={apiTargetTime}&obs=ta";

        int maxRetries = 3;
        int retryCount = 0;
        bool isSuccess = false;

        while (retryCount < maxRetries && !isSuccess)
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(requestUrl))
            {
                request.timeout = 15; // 서버에서 이미지 생성할 시간 충분히
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    if (texture != null)
                    {
                        CachedDecalTexture = texture;
                        IsPngLoaded = true;
                        Debug.Log($" [완료] PNG 다운로드 성공: ({texture.width}x{texture.height})");
                        isSuccess = true;
                    }
                }
                else
                {
                    retryCount++;
                    Debug.LogWarning($" [다운로드 시도 {retryCount}/{maxRetries} 실패]: {request.error}. {(retryCount < maxRetries ? "2초 후 재시도..." : "")}");
                    if (retryCount < maxRetries) yield return new WaitForSeconds(2.0f); // 2초 대기 후 재시도
                }
            }
        }

        if (!isSuccess)
        {
            Debug.LogError(" [최종 에러] PNG 데칼 이미지 다운로드에 완전히 실패했습니다.");
        }
    }


    #endregion

    #region 3. Bounds 영역 경계 데이터 다운로드 코루틴

    private IEnumerator FetchBoundsData()
    {
        string requestUrl = $"http://{apiServerIP}:5000/api/weather/mapo-decal.json";
        UnityWebRequest request = UnityWebRequest.Get(requestUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($" [서버 에러] Bounds 경계 데이터 로드 실패: {request.error}");
            yield break;
        }

        string rawJsonText = request.downloadHandler.text;

        try
        {
            //  이번 JSON은 단순한 단층 구조이므로 유니티의 순정 JsonUtility를 사용하여 정밀하게 파싱합니다.
            BoundsWrapper wrapper = JsonUtility.FromJson<BoundsWrapper>(rawJsonText);

            if (wrapper != null && wrapper.bounds != null)
            {
                //  [객체 보관] double 자릿수 손실 없이 온전하게 영역 경계 수치를 변수에 저장합니다.
                CachedBounds = wrapper.bounds;
                IsBoundsLoaded = true;

                Debug.Log($" [3/3 완료] Bounds 경계 영역 데이터 캐싱 완료.");
                Debug.Log($"  범위 스캔 ➡️ MinLon: {CachedBounds.minLon} / MaxLon: {CachedBounds.maxLon} / MinLat: {CachedBounds.minLat} / MaxLat: {CachedBounds.maxLat}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($" [Bounds 파서 에러] 예외 발생: {e.Message}");
        }
    }

    #endregion
}
