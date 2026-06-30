using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static UnityEditor.U2D.ScriptablePacker;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("플라스크 서버 설정")]
    [SerializeField] string apiServerIP = "127.0.0.1";
    [SerializeField] string apiTargetTime = "202408112300";


    #region ``[전역 마스터 객체 저장소] 다른 파일에서 바로 조회해서 쓰는 서랍장``
    // grid.geojson 격자 데이터 저장소
    public List<ZoneData> zoneList { get; private set; } = new List<ZoneData>();

    // 새로 추가된 PNG 데칼 이미지 텍스처 저장소 (Cesium 투사 연출용 등으로 사용)
    public Texture2D CachedDecalTexture { get; private set; }

    // 각 데이터가 완벽하게 들어왔는지 확인하는 개별 상태 플래그
    public bool IsGeoJsonLoaded { get; private set; } = false;
    public bool IsPngLoaded { get; private set; } = false;

    // 다른 파일에서 전체 로딩이 끝났는지 확인하는 마스터 상태 플래그
    public bool IsAllWeatherDataLoaded => IsGeoJsonLoaded && IsPngLoaded;
    #endregion


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(FetchPngDecalImage());
    }

    private void OnDisable()
    {
        // 스크립트가 비활성화되거나 씬이 멈출 때 통신을 모두 정리
        StopAllCoroutines();
    }


    #region ``격자 정보 받아와서 rawJsonText로 반환``
    public IEnumerator FetchZoneFromApi(Action<string> onResult)
    {
        string requestUrl = $"http://{apiServerIP}:5000/api/weather/mapo-decal.geojson?tm={apiTargetTime}";
        UnityWebRequest request = UnityWebRequest.Get(requestUrl);

        request.timeout = 30;

        try
        {
            // 통신 시도
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[서버 에러] 로드 실패: {request.error}");
                onResult?.Invoke(null);
            }
            else
            {
                onResult?.Invoke(request.downloadHandler.text);
            }
        }
        finally
        {
            // 에러가 나든 성공하든 통신 종료 후 리소스를 즉시 반납하여
            // 메모리 누수 방지 및 재사용 준비 (다음 통신 시 충돌 방지)
            request.Dispose();
            Debug.Log("[NetworkManager] 리소스 해제 완료");
        }
    }
    #endregion


    #region ``PNG 데칼 이미지 다운로드 코루틴``
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
                        Debug.Log($"[완료] PNG 다운로드 성공: ({texture.width}x{texture.height})");
                        isSuccess = true;
                    }
                }
                else
                {
                    retryCount++;
                    Debug.LogWarning($"[다운로드 시도 {retryCount}/{maxRetries} 실패]: {request.error}. {(retryCount < maxRetries ? "2초 후 재시도..." : "")}");
                    if (retryCount < maxRetries) yield return new WaitForSeconds(2.0f); // 2초 대기 후 재시도
                }
            }
        }

        if (!isSuccess)
        {
            Debug.LogError("[최종 에러] PNG 데칼 이미지 다운로드에 완전히 실패했습니다.");
        }
    }
    #endregion




}
