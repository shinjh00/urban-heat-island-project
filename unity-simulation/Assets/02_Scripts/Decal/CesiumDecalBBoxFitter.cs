using CesiumForUnity;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CesiumDecalBBoxFitter : MonoBehaviour
{
    [Header("Target")]
    public DecalProjector decalProjector;
    public CesiumGlobeAnchor globeAnchor;

    // 인스펙터에 마포, 공원 좌표값 입력
    [Header("BBox (Lon/Lat)")]
    public double minLon;
    public double minLat;
    public double maxLon;
    public double maxLat;

    [Header("Decal Size (Meter)")]
    public float widthMeter;
    public float heightMeter;
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

        Debug.Log(
            $"BBox 적용 완료 / Lat:{centerLat}, Lon:{centerLon}, Width:{widthMeter}, Height:{heightMeter}");
    }

    void SetDecalSize(float width, float height, float depth)
    {
        // Unity 공식 API 사용
        decalProjector.size = new Vector3(width, height, depth);
    }
}
