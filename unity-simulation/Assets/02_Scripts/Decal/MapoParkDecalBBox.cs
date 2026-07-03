using UnityEngine;
using UnityEngine.Rendering.Universal;
using CesiumForUnity;
using Unity.Mathematics;
using System.Reflection;


// TODO: [피드백] 
// 현재 두개의 데칼 스크립트 MapoPark와 Mapo는 데이터만 다를 뿐 수행하는 기능이 같음
// 하나의 스크립트 예를 들어CesiumDecalBBoxFitter같은 것을 만들고 값 만 다르게 사용하는 것이 좋겠음.

public class MapoParkDecalBBox : MonoBehaviour
{
    [Header("Target")]
    public DecalProjector decalProjector;
    public CesiumGlobeAnchor globeAnchor;

    [Header("Park BBox Lon/Lat")]
    public double minLon = 126.8661444492528;
    public double minLat = 37.53529993990061;
    public double maxLon = 126.9665402049656;
    public double maxLat = 37.58869326086632;

    [Header("Decal Size Meter")]
    public float widthMeter = 8870.19f;
    public float heightMeter = 5926.03f;
    public float projectionDepth = 500f;

    [Header("Height")]
    public double decalHeight = 151.0473;

    void Start()
    {
        FitDecal();
    }

    void FitDecal()
    {
        if (decalProjector == null)
            decalProjector = GetComponent<DecalProjector>();

        if (globeAnchor == null)
            globeAnchor = GetComponent<CesiumGlobeAnchor>();

        double centerLon = (minLon + maxLon) * 0.5;
        double centerLat = (minLat + maxLat) * 0.5;

        if (globeAnchor != null)
        {
            globeAnchor.longitudeLatitudeHeight =
                new double3(centerLon, centerLat, decalHeight);
        }

        if (decalProjector != null)
        {
            SetDecalSize(widthMeter, heightMeter, projectionDepth);
        }

        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Debug.Log($"공원 BBox 자동 적용 완료 / Lat:{centerLat}, Lon:{centerLon}, Width:{widthMeter}, Height:{heightMeter}");
    }

    void SetDecalSize(float width, float height, float depth)
    {
        // TODO: [피드백] 
        //현재 작성된 SetDecalSize는 URP 버전에 대응하려는 의도는 좋았으나, 
        //유니티 공식 프로퍼티인 decalProjector.size를 직접 수정하는 것이 성능과 안정성 면에서 훨씬 우수하니 수정해보기
        // 아래 예시 참조
        //void SetDecalSize(float width, float height, float depth)
        //{
        //    // 리플렉션 없이 공식 API를 사용하여 직접 값을 대입 
        //    decalProjector.size = new Vector3(width, height, depth);
        //}

        var type = decalProjector.GetType();

        PropertyInfo sizeProp = type.GetProperty("size");
        if (sizeProp != null)
        {
            sizeProp.SetValue(decalProjector, new Vector3(width, height, depth));
            return;
        }

        PropertyInfo widthProp = type.GetProperty("width");
        PropertyInfo heightProp = type.GetProperty("height");
        PropertyInfo depthProp = type.GetProperty("projectionDepth");

        if (widthProp != null) widthProp.SetValue(decalProjector, width);
        if (heightProp != null) heightProp.SetValue(decalProjector, height);
        if (depthProp != null) depthProp.SetValue(decalProjector, depth);
    }
}