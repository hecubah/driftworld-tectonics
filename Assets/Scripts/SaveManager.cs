using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[System.Serializable]
public class Vector3Serial
{
    public float x;
    public float y;
    public float z;
    public Vector3Serial(Vector3 tri)
    {
        x = tri.x; y = tri.y; z = tri.z; 
    }
}

public class QuaternionSerial
{
    public float x;
    public float y;
    public float z;
    public float w;

    public QuaternionSerial(Quaternion quat)
    {
        x = quat.x; y = quat.y; z = quat.z; w = quat.w;
    }
}

[System.Serializable]
public class DRTriangleSerial
{
    public int a;
    public int b;
    public int c;
    public DRTriangleSerial (DRTriangle tri)
    {
        a = tri.m_A; b = tri.m_B; c = tri.m_C; 
    }

}

[System.Serializable]
public class PlateSerial
{
    public Vector3Serial m_RotationAxis;
    public float m_PlateAngularSpeed;
    public float m_InitElevation;
    public float m_Mass;
    public float m_Type;
    public QuaternionSerial m_Transform;
    public Vector3Serial m_Centroid;

    public PlateSerial (Plate plate)
    {
        m_RotationAxis = new Vector3Serial(plate.m_RotationAxis);
        m_PlateAngularSpeed = plate.m_PlateAngularSpeed;
        m_InitElevation = plate.m_InitElevation;
        m_Mass = plate.m_Mass;
        m_Type = plate.m_Type;
        m_Transform = new QuaternionSerial(plate.m_Transform);
        m_Centroid = new Vector3Serial (plate.m_Centroid);
    }
}


[System.Serializable]
public class PlanetBinaryData
{
    public float tec_m_Radius;
    public List<Vector3Serial> tec_m_CrustVertices;
    public List<DRTriangleSerial> tec_m_CrustTriangles;
    public List<PointData> tec_m_CrustPointData;
}

public static class SaveManager
{
    public static string filename = "save.dat";
    public static string path = "";

    public static void Save (PlanetManager man)
    {
        PlanetBinaryData data = new PlanetBinaryData();
        data.tec_m_Radius = man.m_Planet.m_Radius;
        data.tec_m_CrustVertices = new List<Vector3Serial>();
        data.tec_m_CrustPointData = new List<PointData>();
        for (int i = 0; i < man.m_Planet.m_CrustVertices.Count; i++)
        {
            data.tec_m_CrustVertices.Add(new Vector3Serial(man.m_Planet.m_CrustVertices[i]));
            data.tec_m_CrustPointData.Add(new PointData(man.m_Planet.m_CrustPointData[i]));
        }
        data.tec_m_CrustTriangles = new List<DRTriangleSerial>();
        for (int i = 0; i < man.m_Planet.m_CrustTriangles.Count; i++)
        {
            data.tec_m_CrustTriangles.Add(new DRTriangleSerial(man.m_Planet.m_CrustTriangles[i]));
        }


        BinaryFormatter bf = new BinaryFormatter();
        filename = man.m_SaveFilename;
        FileStream fs = File.OpenWrite(filename);
        bf.Serialize(fs, data);
        fs.Close();
    }
    public static void Load(PlanetManager man)
    {
        /*
        BinaryFormatter bf = new BinaryFormatter();
        FileStream fs = File.OpenRead(filename);
        TecPlanetBinaryData saved_planet = new TecPlanetBinaryData();
        saved_planet = (TecPlanetBinaryData)bf.Deserialize(fs);
        return saved_planet;
        */
    }
}
