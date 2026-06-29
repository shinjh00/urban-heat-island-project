using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ZoneData
{
    public string zoneId;       // 해당하는 Grid의 ID (index)
    public Vector2[] polygon;   // Grid를 그려주는 polygon 좌표 (5개 고정)
    public float temperature;   // 해당 Grid의 온도값
}
