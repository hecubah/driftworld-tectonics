using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class SimpleReadStream
{
    public int m_StreamIndex;
    public byte[] m_Buffer;
    public int m_BufferSize;

    public SimpleReadStream()
    {
        m_StreamIndex = 0; 
        m_BufferSize = 0;
        m_Buffer = null;
    }

    public void Read(byte[] output, int offset, int length)
    {
        for (int i = 0; i < length; i++)
        {
            output[offset + i] = m_StreamIndex < m_BufferSize ? m_Buffer[m_StreamIndex++] : (byte)0;
        }
    }
}

[System.Serializable]
public class Vector3Serial
{
    public float x;
    public float y;
    public float z;

    public Vector3Serial()
    {
        x = 0; y = 0; z = 0;
    }

    public Vector3Serial(Vector3 tri)
    {
        x = tri.x; y = tri.y; z = tri.z;
    }

    public Vector3Serial(Vector3Serial tri)
    {
        x = tri.x; y = tri.y; z = tri.z;
    }
}

[System.Serializable]
public class QuaternionSerial
{
    public float x;
    public float y;
    public float z;
    public float w;

    public QuaternionSerial()
    {
        x = 0; y = 0; z = 0; w = 0;
    }

    public QuaternionSerial(Quaternion quat)
    {
        x = quat.x; y = quat.y; z = quat.z; w = quat.w;
    }

    public QuaternionSerial(QuaternionSerial quat)
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

    public DRTriangleSerial()
    {
        a = -1; b = -1; c = -1; // default nonsense values
    }

    public DRTriangleSerial(DRTriangle tri)
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

    public PlateSerial()
    {
        m_RotationAxis = null;
        m_PlateAngularSpeed = 0;
        m_InitElevation = 0;
        m_Mass = 0;
        m_Type = 0;
        m_Transform = null;
        m_Centroid = null;
    }

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
    public bool m_TectonicsPresent;
    public float m_Radius;
    public int m_TectonicStepsTaken;

    public List<Vector3Serial> m_CrustVertices;
    public List<DRTriangleSerial> m_CrustTriangles;
    public List<PointData> m_CrustPointData;
    public List<PlateSerial> m_TectonicPlates;

    public List<Vector3Serial> m_DataVertices;
    public List<DRTriangleSerial> m_DataTriangles;
    public List<PointData> m_DataPointData;
    public List<List<int>> m_DataVerticesNeighbours;
    public List<List<int>> m_DataTrianglesOfVertices;

    public List<Vector3Serial> m_RenderVertices;
    public List<DRTriangleSerial> m_RenderTriangles;
    public List<List<int>> m_RenderVerticesNeighbours;
    public List<List<int>> m_RenderTrianglesOfVertices;
    public List<PointData> m_RenderPointData;

}

public static class SaveManager
{
    public static string filename = "save.dat";
    public static string path = "";

    public static void SavePlanet (PlanetManager man)
    {
        PlanetBinaryData data = new PlanetBinaryData();
        data.m_TectonicsPresent = (man.m_Planet.m_TectonicPlates.Count > 0); 
        data.m_Radius = man.m_Planet.m_Radius;
        data.m_TectonicStepsTaken = man.m_Planet.m_TectonicStepsTaken;
        data.m_CrustVertices = new List<Vector3Serial>();
        data.m_CrustPointData = new List<PointData>();
        data.m_DataVertices = new List<Vector3Serial>();
        data.m_DataPointData = new List<PointData>();
        data.m_DataVerticesNeighbours = new List<List<int>>();
        data.m_DataTrianglesOfVertices = new List<List<int>>();
        for (int i = 0; i < man.m_Planet.m_DataVertices.Count; i++)
        {
            if (data.m_TectonicsPresent)
            {
                data.m_CrustVertices.Add(new Vector3Serial(man.m_Planet.m_CrustVertices[i]));
                data.m_CrustPointData.Add(new PointData(man.m_Planet.m_CrustPointData[i]));
            }
            data.m_DataVertices.Add(new Vector3Serial(man.m_Planet.m_DataVertices[i]));
            data.m_DataPointData.Add(new PointData(man.m_Planet.m_DataPointData[i]));
            data.m_DataVerticesNeighbours.Add(new List<int>(man.m_Planet.m_DataVerticesNeighbours[i]));
            data.m_DataTrianglesOfVertices.Add(new List<int>(man.m_Planet.m_DataTrianglesOfVertices[i]));
        }
        data.m_CrustTriangles = new List<DRTriangleSerial>();
        data.m_DataTriangles = new List<DRTriangleSerial>();
        for (int i = 0; i < man.m_Planet.m_DataTriangles.Count; i++)
        {
            if (data.m_TectonicsPresent)
            {
                data.m_CrustTriangles.Add(new DRTriangleSerial(man.m_Planet.m_CrustTriangles[i]));
            }
            data.m_DataTriangles.Add(new DRTriangleSerial(man.m_Planet.m_DataTriangles[i]));
        }
        data.m_TectonicPlates = new List<PlateSerial>();
        for (int i = 0; i < man.m_Planet.m_TectonicPlatesCount; i++)
        {
            if (data.m_TectonicsPresent) // maybe redundant
            {
                data.m_TectonicPlates.Add(new PlateSerial(man.m_Planet.m_TectonicPlates[i]));
            }
        }


        data.m_RenderVertices = new List<Vector3Serial>();
        data.m_RenderPointData = new List<PointData>();
        data.m_RenderVerticesNeighbours = new List<List<int>>();
        data.m_RenderTrianglesOfVertices = new List<List<int>>();
        for (int i = 0; i < man.m_Planet.m_RenderVertices.Count; i++)
        {
            data.m_RenderVertices.Add(new Vector3Serial(man.m_Planet.m_RenderVertices[i]));
            data.m_RenderPointData.Add(new PointData(man.m_Planet.m_RenderPointData[i]));
            data.m_RenderVerticesNeighbours.Add(new List<int>(man.m_Planet.m_RenderVerticesNeighbours[i]));
            data.m_RenderTrianglesOfVertices.Add(new List<int>(man.m_Planet.m_RenderTrianglesOfVertices[i]));
        }
        data.m_RenderTriangles = new List<DRTriangleSerial>();
        for (int i = 0; i < man.m_Planet.m_RenderTriangles.Count; i++)
        {
            data.m_RenderTriangles.Add(new DRTriangleSerial(man.m_Planet.m_RenderTriangles[i]));
        }
        filename = man.m_SaveFilename;
        Save(data);
    }

    public static void Save(PlanetBinaryData data)
    {
        FileStream fs = new FileStream(filename, FileMode.Create);
        byte[] value_buffer;
        bool tectonics_present = data.m_TectonicsPresent;
        value_buffer = BitConverter.GetBytes((data.m_TectonicsPresent) ? 1 : 0);
        fs.Write(value_buffer, 0, 4); // this might be an oopsie, dunno
        value_buffer = BitConverter.GetBytes(data.m_Radius);
        fs.Write(value_buffer, 0, 4);
        if (tectonics_present)
        {
            value_buffer = BitConverter.GetBytes(data.m_TectonicStepsTaken);
            fs.Write(value_buffer, 0, 4);
        }
        int n_vertices = data.m_DataVertices.Count;
        value_buffer = BitConverter.GetBytes(n_vertices);
        fs.Write(value_buffer, 0, 4);
        int n_neighbours;
        for (int i = 0; i < n_vertices; i++)
        {
            if (tectonics_present)
            {
                value_buffer = BitConverter.GetBytes(data.m_CrustVertices[i].x);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes(data.m_CrustVertices[i].y);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes(data.m_CrustVertices[i].z);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes(data.m_CrustPointData[i].elevation);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes(data.m_CrustPointData[i].thickness);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes(data.m_CrustPointData[i].plate);
                fs.Write(value_buffer, 0, 4);
            }
            value_buffer = BitConverter.GetBytes(data.m_DataVertices[i].x);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_DataVertices[i].y);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_DataVertices[i].z);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_DataPointData[i].elevation);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_DataPointData[i].thickness);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_DataPointData[i].plate);
            fs.Write(value_buffer, 0, 4);
            n_neighbours = data.m_DataVerticesNeighbours[i].Count;
            value_buffer = BitConverter.GetBytes(n_neighbours);
            fs.Write(value_buffer, 0, 4);
            for (int j = 0; j < n_neighbours; j++)
            {
                value_buffer = BitConverter.GetBytes(data.m_DataVerticesNeighbours[i][j]);
                fs.Write(value_buffer, 0, 4);
            }
            n_neighbours = data.m_DataTrianglesOfVertices[i].Count;
            value_buffer = BitConverter.GetBytes(n_neighbours);
            fs.Write(value_buffer, 0, 4);
            for (int j = 0; j < n_neighbours; j++)
            {
                value_buffer = BitConverter.GetBytes(data.m_DataTrianglesOfVertices[i][j]);
                fs.Write(value_buffer, 0, 4);
            }
        }

        int n_triangles = data.m_DataTriangles.Count;
        value_buffer = BitConverter.GetBytes(n_triangles);
        fs.Write(value_buffer, 0, 4);
        for (int i = 0; i < n_triangles; i++)
        {
            if (tectonics_present)
            {
                value_buffer = BitConverter.GetBytes(data.m_CrustTriangles[i].a);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes(data.m_CrustTriangles[i].b);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes(data.m_CrustTriangles[i].c);
                fs.Write(value_buffer, 0, 4);
            }
            value_buffer = BitConverter.GetBytes(data.m_DataTriangles[i].a);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_DataTriangles[i].b);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_DataTriangles[i].c);
            fs.Write(value_buffer, 0, 4);
        }

        if (tectonics_present)
        {
            int n_plates = data.m_TectonicPlates.Count;
            for (int i = 0; i < n_plates; i++)
            {
                value_buffer = BitConverter.GetBytes(data.m_TectonicPlates[i].m_RotationAxis.x);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes(data.m_TectonicPlates[i].m_RotationAxis.y);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes(data.m_TectonicPlates[i].m_RotationAxis.z);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes(data.m_TectonicPlates[i].m_PlateAngularSpeed);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes(data.m_TectonicPlates[i].m_InitElevation);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes(data.m_TectonicPlates[i].m_Mass);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes(data.m_TectonicPlates[i].m_Type);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes(data.m_TectonicPlates[i].m_Transform.x);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes(data.m_TectonicPlates[i].m_Transform.y);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes(data.m_TectonicPlates[i].m_Transform.z);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes(data.m_TectonicPlates[i].m_Transform.w);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes(data.m_TectonicPlates[i].m_Centroid.x);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes(data.m_TectonicPlates[i].m_Centroid.y);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes(data.m_TectonicPlates[i].m_Centroid.z);
                fs.Write(value_buffer, 0, 4);
            }
        }

        n_vertices = data.m_RenderVertices.Count;
        value_buffer = BitConverter.GetBytes(n_vertices);
        fs.Write(value_buffer, 0, 4);
        for (int i = 0; i < n_vertices; i++)
        {
            value_buffer = BitConverter.GetBytes(data.m_RenderVertices[i].x);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_RenderVertices[i].y);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_RenderVertices[i].z);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_RenderPointData[i].elevation);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_RenderPointData[i].thickness);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_RenderPointData[i].plate);
            fs.Write(value_buffer, 0, 4);
            n_neighbours = data.m_RenderVerticesNeighbours[i].Count;
            value_buffer = BitConverter.GetBytes(n_neighbours);
            fs.Write(value_buffer, 0, 4);
            for (int j = 0; j < n_neighbours; j++)
            {
                value_buffer = BitConverter.GetBytes(data.m_RenderVerticesNeighbours[i][j]);
                fs.Write(value_buffer, 0, 4);
            }
            n_neighbours = data.m_RenderTrianglesOfVertices[i].Count;
            value_buffer = BitConverter.GetBytes(n_neighbours);
            fs.Write(value_buffer, 0, 4);
            for (int j = 0; j < n_neighbours; j++)
            {
                value_buffer = BitConverter.GetBytes(data.m_RenderTrianglesOfVertices[i][j]);
                fs.Write(value_buffer, 0, 4);
            }
        }

        n_triangles = data.m_RenderTriangles.Count;
        value_buffer = BitConverter.GetBytes(n_triangles);
        fs.Write(value_buffer, 0, 4);
        for (int i = 0; i < n_triangles; i++)
        {
            value_buffer = BitConverter.GetBytes(data.m_RenderTriangles[i].a);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_RenderTriangles[i].b);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_RenderTriangles[i].c);
            fs.Write(value_buffer, 0, 4);
        }

        fs.Close();
    }


    public static void LoadPlanet(PlanetManager man)
    {
        filename = man.m_SaveFilename;
        PlanetBinaryData data = Load();
        Debug.Log(data.m_DataPointData.Count);
        if (man.m_Surface == null)
        {
            man.m_Surface = new GameObject("Surface");
            man.m_Surface.transform.parent = man.transform;
            MeshFilter newMeshFilter = man.m_Surface.AddComponent<MeshFilter>();
            man.m_Surface.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Custom/SphereTextureShader"));
            newMeshFilter.sharedMesh = new Mesh();
        }
        if (man.m_Random == null)
        {
            man.m_Random = new RandomMersenne(man.m_RandomSeed);
        }
        man.m_Planet = new TectonicPlanet(man.m_Settings.PlanetRadius);

        //CONSTRUCT PLANET HERE



        //PLANET SHOULD BE CONSTRUCTED BY NOW

        man.m_RenderMode = "normal";
        man.RenderSurfaceMesh();
        man.m_Surface.GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", null);
        GameObject.Find("TexturePlane").GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", null);
        man.m_Planet.InitializeCBuffers();
    }

    public static PlanetBinaryData Load()
    {
        PlanetBinaryData data = new PlanetBinaryData();
        FileStream fs = new FileStream(filename, FileMode.Open);
        byte[] bytes = new byte[fs.Length];
        fs.Read(bytes, 0, (int)fs.Length);
        SimpleReadStream ms = new SimpleReadStream();
        ms.m_BufferSize = bytes.Length;
        ms.m_StreamIndex = 0;
        ms.m_Buffer = bytes;
        fs.Close();


        byte[] value_read = new byte[4];

        ms.Read(value_read, 0, 4); // bool as an int
        data.m_TectonicsPresent = (BitConverter.ToInt32(value_read, 0) > 0 ? true : false);
        bool tectonics_present = data.m_TectonicsPresent;
        ms.Read(value_read, 0, 4);
        data.m_Radius = BitConverter.ToSingle(value_read, 0);

        if (tectonics_present)
        {
            ms.Read(value_read, 0, 4);
            data.m_TectonicStepsTaken = BitConverter.ToInt32(value_read, 0);
        }

        ms.Read(value_read, 0, 4);
        int n_vertices = BitConverter.ToInt32(value_read, 0);

        data.m_CrustVertices = new List<Vector3Serial>();
        data.m_CrustPointData = new List<PointData>();
        data.m_DataVertices = new List<Vector3Serial>();
        data.m_DataPointData = new List<PointData>();
        data.m_DataVerticesNeighbours = new List<List<int>>();
        data.m_DataTrianglesOfVertices = new List<List<int>>();

        int n_neighbours;

        for (int i = 0; i < n_vertices; i++)
        {

            Vector3Serial vertex = new Vector3Serial();
            PointData point = new PointData();

            if (tectonics_present)
            {
                ms.Read(value_read, 0, 4);
                vertex.x = BitConverter.ToSingle(value_read, 0);
                ms.Read(value_read, 0, 4);
                vertex.y = BitConverter.ToSingle(value_read, 0);
                ms.Read(value_read, 0, 4);
                vertex.z = BitConverter.ToSingle(value_read, 0);
                data.m_CrustVertices.Add(vertex);

                ms.Read(value_read, 0, 4);
                point.elevation = BitConverter.ToSingle(value_read, 0);
                ms.Read(value_read, 0, 4);
                point.thickness = BitConverter.ToSingle(value_read, 0);
                ms.Read(value_read, 0, 4);
                point.plate = BitConverter.ToInt32(value_read, 0);
                data.m_CrustPointData.Add(point);
            }

            ms.Read(value_read, 0, 4);
            vertex.x = BitConverter.ToSingle(value_read, 0);
            ms.Read(value_read, 0, 4);
            vertex.y = BitConverter.ToSingle(value_read, 0);
            ms.Read(value_read, 0, 4);
            vertex.z = BitConverter.ToSingle(value_read, 0);
            data.m_DataVertices.Add(vertex);

            ms.Read(value_read, 0, 4);
            point.elevation = BitConverter.ToSingle(value_read, 0);
            ms.Read(value_read, 0, 4);
            point.thickness = BitConverter.ToSingle(value_read, 0);
            ms.Read(value_read, 0, 4);
            point.plate = BitConverter.ToInt32(value_read, 0);
            data.m_DataPointData.Add(point);

            ms.Read(value_read, 0, 4);
            n_neighbours = BitConverter.ToInt32(value_read, 0);
            List<int> int_list = new List<int>();
            for (int j = 0; j < n_neighbours; j++)
            {
                ms.Read(value_read, 0, 4);
                int_list.Add(BitConverter.ToInt32(value_read, 0));
            }
            data.m_DataVerticesNeighbours.Add(int_list);

            ms.Read(value_read, 0, 4);
            n_neighbours = BitConverter.ToInt32(value_read, 0);
            int_list = new List<int>();
            for (int j = 0; j < n_neighbours; j++)
            {
                ms.Read(value_read, 0, 4);
                int_list.Add(BitConverter.ToInt32(value_read, 0));
            }
            data.m_DataTrianglesOfVertices.Add(int_list);
        }

        ms.Read(value_read, 0, 4);
        int n_triangles = BitConverter.ToInt32(value_read, 0);

        data.m_CrustTriangles = new List<DRTriangleSerial>();
        data.m_DataTriangles = new List<DRTriangleSerial>();

        for (int i = 0; i < n_triangles; i++)
        {

            DRTriangleSerial triangle = new DRTriangleSerial();

            if (tectonics_present)
            {
                ms.Read(value_read, 0, 4);
                triangle.a = BitConverter.ToInt32(value_read, 0);
                ms.Read(value_read, 0, 4);
                triangle.b = BitConverter.ToInt32(value_read, 0);
                ms.Read(value_read, 0, 4);
                triangle.c = BitConverter.ToInt32(value_read, 0);
                data.m_CrustTriangles.Add(triangle);
            }

            ms.Read(value_read, 0, 4);
            triangle.a = BitConverter.ToInt32(value_read, 0);
            ms.Read(value_read, 0, 4);
            triangle.b = BitConverter.ToInt32(value_read, 0);
            ms.Read(value_read, 0, 4);
            triangle.c = BitConverter.ToInt32(value_read, 0);
            data.m_DataTriangles.Add(triangle);
        }

        if (tectonics_present)
        {
            ms.Read(value_read, 0, 4);
            int n_plates = BitConverter.ToInt32(value_read, 0);

            data.m_TectonicPlates = new List<PlateSerial>();

            for (int i = 0; i < n_plates; i++)
            {

                PlateSerial plate = new PlateSerial();

                Vector3Serial vector = new Vector3Serial();
                QuaternionSerial transform = new QuaternionSerial();

                ms.Read(value_read, 0, 4);
                vector.x = BitConverter.ToSingle(value_read, 0);
                ms.Read(value_read, 0, 4);
                vector.y = BitConverter.ToSingle(value_read, 0);
                ms.Read(value_read, 0, 4);
                vector.z = BitConverter.ToSingle(value_read, 0);
                plate.m_RotationAxis = new Vector3Serial(vector);

                ms.Read(value_read, 0, 4);
                plate.m_PlateAngularSpeed = BitConverter.ToSingle(value_read, 0);

                ms.Read(value_read, 0, 4);
                plate.m_InitElevation = BitConverter.ToSingle(value_read, 0);

                ms.Read(value_read, 0, 4);
                plate.m_Mass = BitConverter.ToSingle(value_read, 0);

                ms.Read(value_read, 0, 4);
                plate.m_Type = BitConverter.ToSingle(value_read, 0);

                ms.Read(value_read, 0, 4);
                transform.x = BitConverter.ToSingle(value_read, 0);
                ms.Read(value_read, 0, 4);
                transform.y = BitConverter.ToSingle(value_read, 0);
                ms.Read(value_read, 0, 4);
                transform.z = BitConverter.ToSingle(value_read, 0);
                ms.Read(value_read, 0, 4);
                transform.w = BitConverter.ToSingle(value_read, 0);
                plate.m_Transform = new QuaternionSerial(transform);

                ms.Read(value_read, 0, 4);
                vector.x = BitConverter.ToSingle(value_read, 0);
                ms.Read(value_read, 0, 4);
                vector.y = BitConverter.ToSingle(value_read, 0);
                ms.Read(value_read, 0, 4);
                vector.z = BitConverter.ToSingle(value_read, 0);
                plate.m_Centroid = new Vector3Serial(vector);

                data.m_TectonicPlates.Add(plate);
            }

        }

        ms.Read(value_read, 0, 4);
        n_vertices = BitConverter.ToInt32(value_read, 0);

        data.m_RenderVertices = new List<Vector3Serial>();
        data.m_RenderPointData = new List<PointData>();
        data.m_RenderVerticesNeighbours = new List<List<int>>();
        data.m_RenderTrianglesOfVertices = new List<List<int>>();

        for (int i = 0; i < n_vertices; i++)
        {

            Vector3Serial vertex = new Vector3Serial();
            PointData point = new PointData();

            ms.Read(value_read, 0, 4);
            vertex.x = BitConverter.ToSingle(value_read, 0);
            ms.Read(value_read, 0, 4);
            vertex.y = BitConverter.ToSingle(value_read, 0);
            ms.Read(value_read, 0, 4);
            vertex.z = BitConverter.ToSingle(value_read, 0);
            data.m_RenderVertices.Add(vertex);

            ms.Read(value_read, 0, 4);
            point.elevation = BitConverter.ToSingle(value_read, 0);
            ms.Read(value_read, 0, 4);
            point.thickness = BitConverter.ToSingle(value_read, 0);
            ms.Read(value_read, 0, 4);
            point.plate = BitConverter.ToInt32(value_read, 0);
            data.m_RenderPointData.Add(point);

            ms.Read(value_read, 0, 4);
            n_neighbours = BitConverter.ToInt32(value_read, 0);
            List<int> int_list = new List<int>();
            for (int j = 0; j < n_neighbours; j++)
            {
                ms.Read(value_read, 0, 4);
                int_list.Add(BitConverter.ToInt32(value_read, 0));
            }
            data.m_RenderVerticesNeighbours.Add(int_list);

            ms.Read(value_read, 0, 4);
            n_neighbours = BitConverter.ToInt32(value_read, 0);
            int_list = new List<int>();
            for (int j = 0; j < n_neighbours; j++)
            {
                ms.Read(value_read, 0, 4);
                int_list.Add(BitConverter.ToInt32(value_read, 0));
            }
            data.m_RenderTrianglesOfVertices.Add(int_list);
        }

        ms.Read(value_read, 0, 4);
        n_triangles = BitConverter.ToInt32(value_read, 0);

        data.m_RenderTriangles = new List<DRTriangleSerial>();

        for (int i = 0; i < n_triangles; i++)
        {

            DRTriangleSerial triangle = new DRTriangleSerial();

            ms.Read(value_read, 0, 4);
            triangle.a = BitConverter.ToInt32(value_read, 0);
            ms.Read(value_read, 0, 4);
            triangle.b = BitConverter.ToInt32(value_read, 0);
            ms.Read(value_read, 0, 4);
            triangle.c = BitConverter.ToInt32(value_read, 0);
            data.m_RenderTriangles.Add(triangle);
        }

        return data;
    }


}
