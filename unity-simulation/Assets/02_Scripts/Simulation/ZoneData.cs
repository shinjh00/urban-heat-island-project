using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ZoneData
{
    public string zoneId;       // 해당하는 Grid의 ID (index)
    public double[,] polygon = new double[5, 2];   // Grid를 그려주는 polygon 좌표 (5행 2열 고정)
    public float temperature;   // 해당 Grid의 온도값
}
