using UnityEngine;
using UnityEngine.Rendering.Universal;
using CesiumForUnity;
using Unity.Mathematics;
using System.Reflection;

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