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
    public int neigh1;
    public int neigh2;
    public int neigh3;

    public DRTriangleSerial()
    {
        a = -1; b = -1; c = -1; neigh1 = -1; neigh2 = -1; neigh3 = -1; // default nonsense values
    }

    public DRTriangleSerial(DRTriangle tri)
    {
        a = tri.m_A; b = tri.m_B; c = tri.m_C; neigh1 = tri.m_Neighbours[0]; neigh2 = tri.m_Neighbours[1]; neigh3 = tri.m_Neighbours[2];
    }

}

[System.Serializable]
public class PlateSerial
{
    public Vector3Serial m_RotationAxis;
    public float m_PlateAngularSpeed;
    public QuaternionSerial m_Transform;
    public Vector3Serial m_Centroid;

    public PlateSerial()
    {
        m_RotationAxis = null;
        m_PlateAngularSpeed = 0;
        m_Transform = null;
        m_Centroid = null;
    }

    public PlateSerial (Plate plate)
    {
        m_RotationAxis = new Vector3Serial(plate.m_RotationAxis);
        m_PlateAngularSpeed = plate.m_PlateAngularSpeed;
        m_Transform = new QuaternionSerial(plate.m_Transform);
        m_Centroid = new Vector3Serial (plate.m_Centroid);
    }

}


[System.Serializable]
public class PlanetBinaryData
{
    public bool m_TectonicsPresent;
    public float m_Radius;
    public int m_TotalTectonicStepsTaken;
    public int m_TectonicStepsTakenWithoutResample;

    public List<Vector3Serial> m_CrustVertices;
    public List<DRTriangleSerial> m_CrustTriangles;
    public List<PointData> m_CrustPointData;
    public List<PlateSerial> m_TectonicPlates;

    public List<Vector3Serial> m_DataVertices;
    public List<DRTriangleSerial> m_DataTriangles;
    public List<PointData> m_DataPointData;
    public List<List<int>> m_DataVerticesNeighbours;
    public List<List<int>> m_DataTrianglesOfVertices;
    public List<Vector3Serial> m_VectorNoise;

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
        data.m_TectonicStepsTakenWithoutResample = man.m_Planet.m_TectonicStepsTakenWithoutResample;
        data.m_TotalTectonicStepsTaken = man.m_Planet.m_TotalTectonicStepsTaken;
        data.m_CrustVertices = new List<Vector3Serial>();
        data.m_CrustPointData = new List<PointData>();
        data.m_DataVertices = new List<Vector3Serial>();
        data.m_DataPointData = new List<PointData>();
        data.m_DataVerticesNeighbours = new List<List<int>>();
        data.m_DataTrianglesOfVertices = new List<List<int>>();
        data.m_VectorNoise = new List<Vector3Serial>();
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
            data.m_VectorNoise.Add(new Vector3Serial(man.m_Planet.m_VectorNoise[i]));
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
            value_buffer = BitConverter.GetBytes(data.m_TectonicStepsTakenWithoutResample);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_TotalTectonicStepsTaken);
            fs.Write(value_buffer , 0, 4);
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
                value_buffer = BitConverter.GetBytes(data.m_CrustPointData[i].age);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes((int)data.m_CrustPointData[i].orogeny);
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
            value_buffer = BitConverter.GetBytes(data.m_DataPointData[i].age);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes((int)data.m_DataPointData[i].orogeny);
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
                value_buffer = BitConverter.GetBytes(data.m_CrustTriangles[i].neigh1);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes(data.m_CrustTriangles[i].neigh2);
                fs.Write(value_buffer, 0, 4);
                value_buffer = BitConverter.GetBytes(data.m_CrustTriangles[i].neigh3);
                fs.Write(value_buffer, 0, 4);
            }
            value_buffer = BitConverter.GetBytes(data.m_DataTriangles[i].a);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_DataTriangles[i].b);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_DataTriangles[i].c);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_DataTriangles[i].neigh1);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_DataTriangles[i].neigh2);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_DataTriangles[i].neigh3);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_VectorNoise[i].x);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_VectorNoise[i].y);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_VectorNoise[i].z);
            fs.Write(value_buffer, 0, 4);
        }

        if (tectonics_present)
        {
            int n_plates = data.m_TectonicPlates.Count;
            value_buffer = BitConverter.GetBytes(n_plates);
            fs.Write(value_buffer, 0, 4);
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
            value_buffer = BitConverter.GetBytes(data.m_RenderPointData[i].age);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes((int)data.m_RenderPointData[i].orogeny);
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
            value_buffer = BitConverter.GetBytes(data.m_RenderTriangles[i].neigh1);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_RenderTriangles[i].neigh2);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_RenderTriangles[i].neigh3);
            fs.Write(value_buffer, 0, 4);
        }

        fs.Close();
    }


    public static void LoadPlanet(PlanetManager man)
    {
        filename = man.m_SaveFilename;
        PlanetBinaryData data = Load();
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

        int n_vertices, n_triangles;
        n_vertices = data.m_DataVertices.Count;
        man.m_Planet.m_CrustVertices = new List<Vector3>();
        man.m_Planet.m_DataVertices = new List<Vector3>();
        man.m_Planet.m_CrustPointData = new List<PointData>();
        man.m_Planet.m_DataPointData = new List<PointData>();
        man.m_Planet.m_DataVerticesNeighbours = new List<List<int>>();
        man.m_Planet.m_DataTrianglesOfVertices = new List<List<int>>();
        for (int i = 0; i < n_vertices; i++)
        {
            if (data.m_TectonicsPresent)
            {
                man.m_Planet.m_CrustVertices.Add(new Vector3(data.m_CrustVertices[i].x, data.m_CrustVertices[i].y, data.m_CrustVertices[i].z));
                man.m_Planet.m_CrustPointData.Add(new PointData(data.m_CrustPointData[i]));
            }
            man.m_Planet.m_DataVertices.Add(new Vector3(data.m_DataVertices[i].x, data.m_DataVertices[i].y, data.m_DataVertices[i].z));
            man.m_Planet.m_DataPointData.Add(new PointData(data.m_DataPointData[i]));
            man.m_Planet.m_DataVerticesNeighbours.Add(data.m_DataVerticesNeighbours[i]);
            man.m_Planet.m_DataTrianglesOfVertices.Add(data.m_DataTrianglesOfVertices[i]);
        }

        n_triangles = data.m_DataTriangles.Count;

        man.m_Planet.m_CrustTriangles = new List<DRTriangle>();
        man.m_Planet.m_DataTriangles = new List<DRTriangle>();
        man.m_Planet.m_VectorNoise = new List<Vector3>();
        DRTriangle new_tri;
        for (int i = 0; i < n_triangles; i++)
        {
            DRTriangleSerial source;
            if (data.m_TectonicsPresent)
            {
                source = data.m_CrustTriangles[i];
                new_tri = new DRTriangle(source.a, source.b, source.c, man.m_Planet.m_CrustVertices);
                new_tri.m_Neighbours.Add(source.neigh1);
                new_tri.m_Neighbours.Add(source.neigh2);
                new_tri.m_Neighbours.Add(source.neigh3);
                man.m_Planet.m_CrustTriangles.Add(new_tri);
            }
            source = data.m_DataTriangles[i];
            new_tri = new DRTriangle(source.a, source.b, source.c, man.m_Planet.m_DataVertices);
            new_tri.m_Neighbours.Add(source.neigh1);
            new_tri.m_Neighbours.Add(source.neigh2);
            new_tri.m_Neighbours.Add(source.neigh3);
            man.m_Planet.m_DataTriangles.Add(new_tri);
            man.m_Planet.m_VectorNoise.Add(new Vector3(data.m_VectorNoise[i].x, data.m_VectorNoise[i].y, data.m_VectorNoise[i].z));

        }

        man.m_Planet.m_TectonicStepsTakenWithoutResample = data.m_TectonicsPresent ? data.m_TectonicStepsTakenWithoutResample : 0;
        man.m_Planet.m_TotalTectonicStepsTaken = data.m_TectonicsPresent ? data.m_TotalTectonicStepsTaken : 0;
        if (data.m_TectonicsPresent)
        {
            man.m_Planet.m_TectonicPlatesCount = data.m_TectonicPlates.Count;
            man.m_Planet.m_TectonicPlates = new List<Plate>();
            for (int i = 0; i < data.m_TectonicPlates.Count; i++)
            {
                Plate target = new Plate(man.m_Planet);
                target.m_RotationAxis = new Vector3(data.m_TectonicPlates[i].m_RotationAxis.x, data.m_TectonicPlates[i].m_RotationAxis.y, data.m_TectonicPlates[i].m_RotationAxis.z);
                target.m_PlateAngularSpeed = data.m_TectonicPlates[i].m_PlateAngularSpeed;
                target.m_Transform = new Quaternion(data.m_TectonicPlates[i].m_Transform.x, data.m_TectonicPlates[i].m_Transform.y, data.m_TectonicPlates[i].m_Transform.z, data.m_TectonicPlates[i].m_Transform.w);
                target.m_Centroid = new Vector3(data.m_TectonicPlates[i].m_Centroid.x, data.m_TectonicPlates[i].m_Centroid.y, data.m_TectonicPlates[i].m_Centroid.z);
                man.m_Planet.m_TectonicPlates.Add(target);
            }
        }

        n_vertices = data.m_RenderVertices.Count;
        man.m_Planet.m_RenderVertices = new List<Vector3>();
        man.m_Planet.m_RenderPointData = new List<PointData>();
        man.m_Planet.m_RenderVerticesNeighbours = new List<List<int>>();
        man.m_Planet.m_RenderTrianglesOfVertices = new List<List<int>>();
        for (int i = 0; i < n_vertices; i++)
        {
            man.m_Planet.m_RenderVertices.Add(new Vector3(data.m_RenderVertices[i].x, data.m_RenderVertices[i].y, data.m_RenderVertices[i].z));
            man.m_Planet.m_RenderPointData.Add(new PointData(data.m_RenderPointData[i]));
            man.m_Planet.m_RenderVerticesNeighbours.Add(data.m_RenderVerticesNeighbours[i]);
            man.m_Planet.m_RenderTrianglesOfVertices.Add(data.m_RenderTrianglesOfVertices[i]);
        }

        n_triangles = data.m_RenderTriangles.Count;
        man.m_Planet.m_RenderTriangles = new List<DRTriangle>();
        for (int i = 0; i < n_triangles; i++)
        {
            DRTriangleSerial source = data.m_RenderTriangles[i];
            new_tri = new DRTriangle(source.a, source.b, source.c, man.m_Planet.m_RenderVertices);
            new_tri.m_Neighbours.Add(source.neigh1);
            new_tri.m_Neighbours.Add(source.neigh2);
            new_tri.m_Neighbours.Add(source.neigh3);
            man.m_Planet.m_RenderTriangles.Add(new_tri);

        }

        man.m_Planet.m_RenderVerticesCount = man.m_Planet.m_RenderVertices.Count; // set the render vertices count
        man.m_Planet.m_RenderTrianglesCount = man.m_Planet.m_RenderTriangles.Count; // set the render triangles count

        man.m_Planet.m_VerticesCount = man.m_Planet.m_DataVertices.Count;
        man.m_Planet.m_TrianglesCount = man.m_Planet.m_DataTriangles.Count;

        List<BoundingVolume> m_BVTLeaves = new List<BoundingVolume>();
        for (int i = 0; i < man.m_Planet.m_TrianglesCount; i++) // for all triangles in data
        {
            BoundingVolume new_bb = new BoundingVolume(man.m_Planet.m_DataTriangles[i].m_CCenter, man.m_Planet.m_DataTriangles[i].m_CUnitRadius); // create a leaf bounding box
            new_bb.m_TriangleIndex = i; // denote the triangle index to the leaf
            man.m_Planet.m_DataTriangles[i].m_BVolume = new_bb; // denote the leaf to the respective triangle
            m_BVTLeaves.Add(new_bb); // add the new bounding volume to the list of leaves
        }
        man.m_Planet.m_DataBVH = man.m_Planet.ConstructBVH(m_BVTLeaves); // construct BVH from bottom
        man.m_Planet.m_DataBVHArray = BoundingVolume.BuildBVHArray(man.m_Planet.m_DataBVH); //

        if (data.m_TectonicsPresent)
        {
            for (int i = 0; i < man.m_Planet.m_VerticesCount; i++)
            {
                man.m_Planet.m_TectonicPlates[man.m_Planet.m_CrustPointData[i].plate].m_PlateVertices.Add(i);
            }

            for (int i = 0; i < man.m_Planet.m_TrianglesCount; i++) // for all triangles
            {
                if ((man.m_Planet.m_CrustPointData[man.m_Planet.m_CrustTriangles[i].m_A].plate == man.m_Planet.m_CrustPointData[man.m_Planet.m_CrustTriangles[i].m_B].plate) && (man.m_Planet.m_CrustPointData[man.m_Planet.m_CrustTriangles[i].m_B].plate == man.m_Planet.m_CrustPointData[man.m_Planet.m_CrustTriangles[i].m_C].plate)) // if the triangle only has vertices of one type (qquivalence is a transitive relation)
                {
                    man.m_Planet.m_TectonicPlates[man.m_Planet.m_DataPointData[man.m_Planet.m_CrustTriangles[i].m_A].plate].m_PlateTriangles.Add(i);
                }
            }

            foreach (Plate it in man.m_Planet.m_TectonicPlates)
            {
                List<BoundingVolume> bvt_leaves = new List<BoundingVolume>();
                int plate_tricount = it.m_PlateTriangles.Count;
                for (int i = 0; i < plate_tricount; i++) // for all triangles in data
                {
                    int tri_index = it.m_PlateTriangles[i];
                    BoundingVolume new_bb = new BoundingVolume(man.m_Planet.m_CrustTriangles[tri_index].m_CCenter, man.m_Planet.m_CrustTriangles[tri_index].m_CUnitRadius); // create a leaf bounding box
                    new_bb.m_TriangleIndex = tri_index; // denote the triangle index to the leaf
                    man.m_Planet.m_CrustTriangles[tri_index].m_BVolume = new_bb; // denote the leaf to the respective triangle
                    bvt_leaves.Add(new_bb); // add the new bounding volume to the list of leaves
                }
                if (bvt_leaves.Count > 0)
                {
                    it.m_BVHPlate = man.m_Planet.ConstructBVH(bvt_leaves);
                    it.m_BVHArray = BoundingVolume.BuildBVHArray(it.m_BVHPlate);
                }
            }
            man.m_Planet.DetermineBorderTriangles();
            man.m_Planet.m_PlatesOverlap = man.m_Planet.CalculatePlatesVP();
            man.m_Planet.InitializeCBuffers();
        }

        //PLANET SHOULD BE CONSTRUCTED BY NOW

        man.m_Planet.InitializeCBuffers();
        man.RenderPlanet();
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
            data.m_TectonicStepsTakenWithoutResample = BitConverter.ToInt32(value_read, 0);
            ms.Read(value_read, 0, 4);
            data.m_TotalTectonicStepsTaken = BitConverter.ToInt32(value_read, 0);
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
                ms.Read(value_read, 0, 4);
                point.age = BitConverter.ToSingle(value_read, 0);
                ms.Read(value_read, 0, 4);
                point.orogeny = (OroType) BitConverter.ToInt32(value_read, 0);
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
            ms.Read(value_read, 0, 4);
            point.age = BitConverter.ToSingle(value_read, 0);
            ms.Read(value_read, 0, 4);
            point.orogeny = (OroType) BitConverter.ToInt32(value_read, 0);
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
        data.m_VectorNoise = new List<Vector3Serial>();

        for (int i = 0; i < n_triangles; i++)
        {

            DRTriangleSerial triangle;

            if (tectonics_present)
            {
                triangle = new DRTriangleSerial();
                ms.Read(value_read, 0, 4);
                triangle.a = BitConverter.ToInt32(value_read, 0);
                ms.Read(value_read, 0, 4);
                triangle.b = BitConverter.ToInt32(value_read, 0);
                ms.Read(value_read, 0, 4);
                triangle.c = BitConverter.ToInt32(value_read, 0);
                ms.Read(value_read, 0, 4);
                triangle.neigh1 = BitConverter.ToInt32(value_read, 0);
                ms.Read(value_read, 0, 4);
                triangle.neigh2 = BitConverter.ToInt32(value_read, 0);
                ms.Read(value_read, 0, 4);
                triangle.neigh3 = BitConverter.ToInt32(value_read, 0);
                data.m_CrustTriangles.Add(triangle);
            }

            triangle = new DRTriangleSerial();

            ms.Read(value_read, 0, 4);
            triangle.a = BitConverter.ToInt32(value_read, 0);
            ms.Read(value_read, 0, 4);
            triangle.b = BitConverter.ToInt32(value_read, 0);
            ms.Read(value_read, 0, 4);
            triangle.c = BitConverter.ToInt32(value_read, 0);
            ms.Read(value_read, 0, 4);
            triangle.neigh1 = BitConverter.ToInt32(value_read, 0);
            ms.Read(value_read, 0, 4);
            triangle.neigh2 = BitConverter.ToInt32(value_read, 0);
            ms.Read(value_read, 0, 4);
            triangle.neigh3 = BitConverter.ToInt32(value_read, 0);
            data.m_DataTriangles.Add(triangle);
            Vector3Serial new_vector = new Vector3Serial();
            ms.Read(value_read, 0, 4);
            new_vector.x = BitConverter.ToInt32(value_read, 0);
            ms.Read(value_read, 0, 4);
            new_vector.y = BitConverter.ToInt32(value_read, 0);
            ms.Read(value_read, 0, 4);
            new_vector.z = BitConverter.ToInt32(value_read, 0);
            data.m_VectorNoise.Add(new_vector);

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
            ms.Read(value_read, 0, 4);
            point.age = BitConverter.ToSingle(value_read, 0);
            ms.Read(value_read, 0, 4);
            point.orogeny = (OroType) BitConverter.ToInt32(value_read, 0);
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
            ms.Read(value_read, 0, 4);
            triangle.neigh1 = BitConverter.ToInt32(value_read, 0);
            ms.Read(value_read, 0, 4);
            triangle.neigh2 = BitConverter.ToInt32(value_read, 0);
            ms.Read(value_read, 0, 4);
            triangle.neigh3 = BitConverter.ToInt32(value_read, 0);
            data.m_RenderTriangles.Add(triangle);
        }

        return data;
    }


}
