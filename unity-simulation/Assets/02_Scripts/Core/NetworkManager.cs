using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering.Universal;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("플라스크 서버 설정")]
    [SerializeField]
    private string apiServerIP = "localhost";
    [SerializeField]
    private string apiTargetTime;

    [Header("Mapo_Material")]
    [Tooltip("Mapo_Material 원본 파일 연결")]
    public Material mapoDirectMaterial;
    [Tooltip("씬에 배치된 Decal Projector 컴포넌트를 연결")]
    public DecalProjector mapoDecalProjector;


    #region ``[전역 마스터 객체 저장소] 다른 파일에서 바로 조회해서 쓰는 변수들``
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

    private void OnDisable()
    {
        // 스크립트가 비활성화되거나 씬이 멈출 때 통신을 모두 정리
        StopAllCoroutines();
    }


    #region ``시각화 기능``
    // ControlPanel.cs에서 시각화 시작 버튼 클릭 시 호출됨
    public void RefreshDecalData()
    {
        StartCoroutine(FetchPngDecalImage());
    }

    // PNG 데칼 이미지 다운로드 코루틴
    private IEnumerator FetchPngDecalImage()
    {
        apiTargetTime = UIManager.Instance.CurrentSelectedDate;
        string requestUrl = $"http://{apiServerIP}:5000/api/weather/mapo-decal.png?source=kma&tm={apiTargetTime}&obs=ta";

        int maxRetries = 3;
        int retryCount = 0;
        bool isSuccess = false;

        while (retryCount < maxRetries && !isSuccess)
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(requestUrl))
            {
                request.timeout = 30; // 서버에서 이미지 생성할 시간 충분히
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    if (texture != null)
                    {
                        CachedDecalTexture = texture;
                        IsPngLoaded = true;
                        Debug.Log($"[NetworkManager] 히트맵 PNG 다운로드 성공: ({texture.width}x{texture.height})");

                        // 다운로드 성공 시 Mapo_Material에 새로 받은 텍스처 반영
                        ApplyTextureToSceneDecal(texture);
                        isSuccess = true;
                    }
                }
                else
                {
                    retryCount++;
                    Debug.LogWarning($"[NetworkManager 경고] 다운로드 시도 {retryCount}/{maxRetries} 실패]: {request.error}. {(retryCount < maxRetries ? "2초 후 재시도..." : "")}");
                    if (retryCount < maxRetries) yield return new WaitForSeconds(2.0f); // 2초 대기 후 재시도
                }
            }
        }

        if (!isSuccess)
        {
            Debug.LogError("[NetworkManager 에러] PNG 데칼 이미지 다운로드 실패");
        }
    }

    // 다운로드 된 텍스처(PNG)를 Mapo_Material에 적용
    private void ApplyTextureToSceneDecal(Texture2D newTexture)
    {
        if (newTexture == null)
        {
            Debug.LogError("[NetworkManager 에러] 전달된 히트맵 텍스처(PNG) 데이터가 Null입니다.");
            return;
        }

        // GPU 메모리 동기화 및 텍스처 최적화 세팅
        newTexture.wrapMode = TextureWrapMode.Repeat;
        newTexture.filterMode = FilterMode.Bilinear;
        newTexture.Apply(); // GPU에 텍스처 데이터 업로드 고정

        // 복사본(Instance)을 만들지 않고 원본 머테리얼에 다이렉트 주입 (최적화 핵심)
        if (mapoDirectMaterial != null)
        {
            // 셰이더 그래프 내부 아이디 "Base_Map"과 바인딩
            mapoDirectMaterial.SetTexture("Base_Map", newTexture);

            // Decal Projector를 껐다 켜서 화면 갱신 유도 (CPU 연산 최소화)
            if (mapoDecalProjector != null)
            {
                mapoDecalProjector.enabled = false;
                mapoDecalProjector.enabled = true;
            }

            Debug.Log("[NetworkManager] 히트맵 텍스처 교체 및 새로고침 성공");
        }
        else
        {
            Debug.LogError("[NetworkManager 에러] NetworkManager 인스펙터에 MapoDirectMaterial이 등록되지 않았습니다.");
        }
    }
    #endregion


    #region ``grid.geojson 받아와서 rawJsonText로 반환``
    public IEnumerator FetchGeoJsonData(Action<string> onResult)
    {
        //string requestUrl = $"http://{apiServerIP}:5000/api/weather/mapo-decal.geojson?tm={apiTargetTime}";
        string requestUrl = $"http://{apiServerIP}:5000/api/weather/mapo-decal.geojson?tm=202511151400";
        UnityWebRequest request = UnityWebRequest.Get(requestUrl);

        request.timeout = 30;

        try
        {
            // 통신 시도
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[서버 에러] 로드 실패: {request.error}");
                IsGeoJsonLoaded = false; // 실패 시 플래그 false 유지
                onResult?.Invoke(null);
            }
            else
            {
                IsGeoJsonLoaded = true; // 성공 시 true 설정
                onResult?.Invoke(request.downloadHandler.text);
            }
        }
        finally
        {
            // 에러가 나든 성공하든 통신 종료 후 리소스를 즉시 반납하여
            // 메모리 누수 방지 및 재사용 준비 (다음 통신 시 충돌 방지)
            request.Dispose();
            Debug.Log("[NetworkManager] 통신 종료 후 리소스 해제 완료");
        }
    }
    #endregion

}
