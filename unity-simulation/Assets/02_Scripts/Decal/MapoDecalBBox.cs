using UnityEngine;
using UnityEngine.Rendering.Universal;
using CesiumForUnity;
using Unity.Mathematics;
using System.Reflection;

public class MapoDecalBBox : MonoBehaviour
{
    [Header("Target")]
    public DecalProjector decalProjector;
    public CesiumGlobeAnchor globeAnchor;

    [Header("BBox Lon/Lat")]
    public double minLon = 126.85362854763055;
    public double minLat = 37.53378572594233;
    public double maxLon = 126.96396551660256;
    public double maxLat = 37.590792433269456;

    [Header("Decal Size Meter")]
    public float widthMeter = 10860.7729f;
    public float heightMeter = 8325.8927f;
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

        Debug.Log($"BBox 자동 적용 완료 / Lat:{centerLat}, Lon:{centerLon}, Width:{widthMeter}, Height:{heightMeter}");
    }

    void SetDecalSize(float width, float height, float depth)
    {
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
