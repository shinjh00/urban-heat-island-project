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

    // 진행 중인 GeoJSON 요청
    private UnityWebRequest currentGeoJsonRequest;

    // Mapo_Material 원본 파일을 보호하기 위한 런타임용 변수
    private Material runtimeMaterial;

    // 현재 진행되고 있는 데칼 요청 코루틴
    private Coroutine currentDecalCoroutine;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        runtimeMaterial = new Material(mapoDirectMaterial);
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        if (currentGeoJsonRequest != null)
        {
            currentGeoJsonRequest.Abort();   // 진행 중인 다운로드 강제 중단
            currentGeoJsonRequest.Dispose(); // C++ 네이티브 메모리 해제
            currentGeoJsonRequest = null;    // 참조 정리
            Debug.Log("[NetworkManager] OnDisable 안전 장치에 의해 진행 중인 GeoJSON 통신이 강제 정리되었습니다.");
        }
    }


    #region ``시각화 기능``

    // TODO: [피드백/크리티컬]
    // 이 함수를 사용자가 날짜를 빠르게 두 번 이상 바꿔서 연달아 호출하면 코루틴이 여러개 동시에 돌게될듯
    // 재시도 로직(최대 3회 + 실패 시 2초 대기) 때문에 요청마다 응답 시간이 달라서
    // 먼저보낸 요청이 나중에 도착해 ApplyTextureToSceneDecal()을
    // 더 늦게 호출하면 최신 날짜 대신 이전 날짜의 히트맵이 남을 수 도있음  
    // 고치는 법 예시: 요청 시작 시 자기만의 요청 ID(또는 요청한 apiTargetTime)를 기억해두고,

    // 응답이 왔을 때 "지금 UI가 가리키는 날짜와 이 응답의 날짜가 같은 경우에만" 텍스처를 적용하면 어떨지? 
    // 아니면 새 요청 시작 전에 이전 코루틴을 StopCoroutine으로 취소하기

    // ControlPanel.cs에서 시각화 시작 버튼 클릭 시 호출됨
    public void RefreshDecalData()
    {
        if (currentDecalCoroutine != null)
        {
            StopCoroutine(currentDecalCoroutine);
        }

        currentDecalCoroutine = StartCoroutine(FetchPngDecalImage());
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
                        if (CachedDecalTexture != null)
                            Destroy(CachedDecalTexture);

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

        if (runtimeMaterial != null)
        {
            // 셰이더 그래프 내부 아이디 "Base_Map"과 바인딩
            runtimeMaterial.SetTexture("Base_Map", newTexture);

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
        // 없으면 6월껄로 , 있으면 그걸로
        string requestUrl = $"http://{apiServerIP}:5000/api/weather/mapo-decal.geojson?tm={apiTargetTime}";
        UnityWebRequest request = UnityWebRequest.Get(requestUrl);

        currentGeoJsonRequest = request;
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

        currentDecalCoroutine = null;
        yield break;
    }
    #endregion

}