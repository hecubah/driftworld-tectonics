using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Simplified reading stream to accelerate loading of larger files. Instances keep all data in memory.
/// </summary>
public class SimpleReadStream
{
    public int m_StreamIndex; // Byte position in a stream
    public byte[] m_Buffer; // byte array of data in a stream
    public int m_BufferSize; // length of the buffer

    public SimpleReadStream() // basic constructor
    {
        m_StreamIndex = 0; 
        m_BufferSize = 0;
        m_Buffer = null;
    }

    /// <summary>
    /// Modelled after C# stream function Read.
    /// </summary>
    /// <param name="output">output byte array</param>
    /// <param name="offset">where to start in the array</param>
    /// <param name="length">how many bytes read</param>
    public void Read(byte[] output, int offset, int length)
    {
        for (int i = 0; i < length; i++)
        {
            output[offset + i] = m_StreamIndex < m_BufferSize ? m_Buffer[m_StreamIndex++] : (byte)0;
        }
    }
}

/// <summary>
/// Serializable version of Vector3 for data files.
/// </summary>
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

/// <summary>
/// Serializable version of Quaternion.
/// </summary>
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

/// <summary>
/// Serializable version of DRTriangle. Simplified, only contains vertex indices and neighbours.
/// </summary>
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

/// <summary>
/// Serialized version of Plate. References must be supplied and BVH reconstructed upon load.
/// </summary>
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

/// <summary>
/// Main data model for files. Only contains data needed for reconstruction or data that would be too resource-intensive to reconstruct.
/// </summary>
[System.Serializable]
public class PlanetBinaryData
{
    public bool m_TectonicsPresent; // switch denoting whether tectonic data are included
    public float m_Radius; // planet radius
    public int m_TotalTectonicStepsTaken; // total tectonic steps taken for reference
    public int m_TectonicStepsTakenWithoutResample; // tectonic steps taken without resampling

    public List<uint> m_RandomMT;
    public uint m_RandomMTI;

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

/// <summary>
/// Facilitates loading of basic triangulation templates, saving and loading data files.
/// </summary>
public class DRFileManager
{
    public string filename; // placeholder filename
    public string path; // not used
    public PlanetManager man; // PlanetManager instance reference, used to get and assign data
    public string m_DataFileHeader; // short description of the data file format
    public int m_Version; // data file version

    /// <summary>
    /// Initialization constructor. Has to be called from the GUI Editor, as PlanetManager is derived from MonoBehaviour.
    /// </summary>
    /// <param name="man_p">PlanetManager instance reference</param>
    public DRFileManager(PlanetManager man_p)
    {
        man = man_p;
        filename = "save.dat"; // taken later from the PlanetManger instance as demanded
        path = ""; // not used
        m_DataFileHeader = "DRIFTWORLD"; // simple header
        m_Version = 1; // first planned version at the moment of initial public release
    }

    /// <summary>
    /// Gathers neccessary data into PlanetBinaryData instance and calls Save() function.
    /// </summary>
    public void SavePlanet ()
    {
        PlanetBinaryData data = new PlanetBinaryData(); // new data instance
        data.m_TectonicsPresent = (man.m_Planet.m_TectonicPlates.Count > 0); // tectonics are included, if available 
        data.m_Radius = man.m_Planet.m_Radius;
        data.m_TectonicStepsTakenWithoutResample = man.m_Planet.m_TectonicStepsTakenWithoutResample;
        data.m_TotalTectonicStepsTaken = man.m_Planet.m_TotalTectonicStepsTaken;
        data.m_RandomMTI = man.m_Random.mti;
        data.m_RandomMT = new List<uint>(); // RNG state
        data.m_CrustVertices = new List<Vector3Serial>(); // lists are created as empty and later filled through PlanetManager
        data.m_CrustPointData = new List<PointData>();
        data.m_DataVertices = new List<Vector3Serial>();
        data.m_DataPointData = new List<PointData>();
        data.m_DataVerticesNeighbours = new List<List<int>>();
        data.m_DataTrianglesOfVertices = new List<List<int>>();
        data.m_VectorNoise = new List<Vector3Serial>();
        for (int i = 0; i < RandomMersenne.MERS_N; i++)
        {
            data.m_RandomMT.Add(man.m_Random.mt[i]); // fill the RNG state array
        }
        for (int i = 0; i < man.m_Planet.m_DataVertices.Count; i++) // copy data and crust (if available) vertices and auxiliary data
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
        for (int i = 0; i < man.m_Planet.m_DataTriangles.Count; i++) // copy data and crust (if available) triangles
        {
            if (data.m_TectonicsPresent)
            {
                data.m_CrustTriangles.Add(new DRTriangleSerial(man.m_Planet.m_CrustTriangles[i]));
            }
            data.m_DataTriangles.Add(new DRTriangleSerial(man.m_Planet.m_DataTriangles[i]));
            data.m_VectorNoise.Add(new Vector3Serial(man.m_Planet.m_VectorNoise[i]));
        }
        data.m_TectonicPlates = new List<PlateSerial>();
        if (data.m_TectonicsPresent)
        {
            for (int i = 0; i < man.m_Planet.m_TectonicPlatesCount; i++) // copy plates data
            {
                data.m_TectonicPlates.Add(new PlateSerial(man.m_Planet.m_TectonicPlates[i]));
            }
        }
        data.m_RenderVertices = new List<Vector3Serial>();
        data.m_RenderPointData = new List<PointData>();
        data.m_RenderVerticesNeighbours = new List<List<int>>();
        data.m_RenderTrianglesOfVertices = new List<List<int>>();
        for (int i = 0; i < man.m_Planet.m_RenderVertices.Count; i++) // copy render vertices and auxiliary data
        {
            data.m_RenderVertices.Add(new Vector3Serial(man.m_Planet.m_RenderVertices[i]));
            data.m_RenderPointData.Add(new PointData(man.m_Planet.m_RenderPointData[i]));
            data.m_RenderVerticesNeighbours.Add(new List<int>(man.m_Planet.m_RenderVerticesNeighbours[i]));
            data.m_RenderTrianglesOfVertices.Add(new List<int>(man.m_Planet.m_RenderTrianglesOfVertices[i]));
        }
        data.m_RenderTriangles = new List<DRTriangleSerial>();
        for (int i = 0; i < man.m_Planet.m_RenderTriangles.Count; i++) // copy render triangles
        {
            data.m_RenderTriangles.Add(new DRTriangleSerial(man.m_Planet.m_RenderTriangles[i]));
        }
        filename = man.m_SaveFilename; // read filename
        Save(data); // save data into file
        Debug.Log("File " + filename + " saved: header [" + m_DataFileHeader + "], version " + m_Version); // feedback
    }

    /// <summary>
    /// Saves data read out from PlanetManager into a file. Called by SavePlanet().
    /// </summary>
    /// <param name="data">data to be saved</param>
    public void Save(PlanetBinaryData data)
    {
        FileStream fs = new FileStream(filename, FileMode.Create); // create file at the beginning
        byte[] value_buffer; // universal 4 byte buffer used almost exclusively (save for header)
        bool tectonics_present = data.m_TectonicsPresent; // if the tectonics are included
        byte[] header_buffer = Encoding.ASCII.GetBytes(m_DataFileHeader); // byte array with the header
        value_buffer = BitConverter.GetBytes(header_buffer.Length); // standard assignment, used for all values
        fs.Write(value_buffer, 0, 4); // standard write call
        fs.Write(header_buffer, 0, header_buffer.Length);
        value_buffer = BitConverter.GetBytes(m_Version);
        fs.Write(value_buffer, 0, 4);
        value_buffer = BitConverter.GetBytes((data.m_TectonicsPresent) ? 1 : 0); // bool stored as an integer
        fs.Write(value_buffer, 0, 4);
        value_buffer = BitConverter.GetBytes(data.m_Radius);
        fs.Write(value_buffer, 0, 4);
        value_buffer = BitConverter.GetBytes(data.m_RandomMTI);
        fs.Write(value_buffer, 0, 4);
        for (int i = 0; i < RandomMersenne.MERS_N; i++)
        {
            value_buffer = BitConverter.GetBytes(data.m_RandomMT[i]);
            fs.Write(value_buffer, 0, 4);
        }
        if (tectonics_present)
        {
            value_buffer = BitConverter.GetBytes(data.m_TectonicStepsTakenWithoutResample);
            fs.Write(value_buffer, 0, 4);
            value_buffer = BitConverter.GetBytes(data.m_TotalTectonicStepsTaken);
            fs.Write(value_buffer , 0, 4);
        }
        int n_vertices = data.m_DataVertices.Count; // bytestream has to contain the number of values before every array
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

    /// <summary>
    /// Reads data from a file into a PlanetBinaryData instance and reconstructs the project.
    /// </summary>
    public void LoadPlanet()
    {
        filename = man.m_SaveFilename;
        PlanetBinaryData data = Load(); // load a file using SimpleReadStream
        if (man.m_Surface == null) // reconstruct the mesh object, if neccessary
        {
            man.m_Surface = new GameObject("Surface");
            man.m_Surface.transform.parent = man.transform;
            MeshFilter newMeshFilter = man.m_Surface.AddComponent<MeshFilter>();
            man.m_Surface.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Custom/SphereTextureShader"));
            newMeshFilter.sharedMesh = new Mesh();
        }
        if (man.m_Random == null) // new RandomMersenne, if needed - actually overriden by random state from the loaded file
        {
            man.m_Random = new RandomMersenne(man.m_RandomSeed);
        }
        man.m_Random.mti = data.m_RandomMTI; // set the random state
        for (int i = 0; i < RandomMersenne.MERS_N; i++)
        {
            man.m_Random.mt[i] = data.m_RandomMT[i];
        }
        man.m_Planet = new TectonicPlanet(man.m_Settings.PlanetRadius);

        // Start of planet reconstruction.
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
            for (int i = 0; i < data.m_TectonicPlates.Count; i++) // plates are only created, filled later
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

        man.m_Planet.m_RenderVerticesCount = man.m_Planet.m_RenderVertices.Count;
        man.m_Planet.m_RenderTrianglesCount = man.m_Planet.m_RenderTriangles.Count;

        man.m_Planet.m_VerticesCount = man.m_Planet.m_DataVertices.Count;
        man.m_Planet.m_TrianglesCount = man.m_Planet.m_DataTriangles.Count;

        List<BoundingVolume> m_BVTLeaves = new List<BoundingVolume>(); // reconstruct the data layer BVH
        for (int i = 0; i < man.m_Planet.m_TrianglesCount; i++)
        {
            BoundingVolume new_bb = new BoundingVolume(man.m_Planet.m_DataTriangles[i].m_CCenter, man.m_Planet.m_DataTriangles[i].m_CUnitRadius); // create a leaf bounding volume
            new_bb.m_TriangleIndex = i; // denote the triangle index to the leaf
            man.m_Planet.m_DataTriangles[i].m_BVolume = new_bb; // denote the leaf to the respective triangle
            m_BVTLeaves.Add(new_bb); // add the new bounding volume to the list of leaves
        }
        man.m_Planet.m_DataBVH = man.m_Planet.ConstructBVH(m_BVTLeaves); // construct BVH from bottom
        man.m_Planet.m_DataBVHArray = BoundingVolume.BuildBVHArray(man.m_Planet.m_DataBVH); // set the BVH array for shader use

        if (data.m_TectonicsPresent)
        {
            for (int i = 0; i < man.m_Planet.m_VerticesCount; i++)
            {
                man.m_Planet.m_TectonicPlates[man.m_Planet.m_CrustPointData[i].plate].m_PlateVertices.Add(i);
            }

            for (int i = 0; i < man.m_Planet.m_TrianglesCount; i++)
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
                for (int i = 0; i < plate_tricount; i++) // reconstruct the plate BVH
                {
                    int tri_index = it.m_PlateTriangles[i];
                    BoundingVolume new_bb = new BoundingVolume(man.m_Planet.m_CrustTriangles[tri_index].m_CCenter, man.m_Planet.m_CrustTriangles[tri_index].m_CUnitRadius); // create a leaf bounding volume
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
            man.m_Planet.CleanUpPlates();
            man.m_Planet.DetermineBorderTriangles(); // find all border triangles and assign
            man.m_Planet.m_PlatesOverlap = man.m_Planet.CalculatePlatesVP(); // recalculate overlap matrix
        }
        // End of planet reconstruction.

        man.m_Planet.InitializeCBuffers(); // initialize the buffers
        man.RenderPlanet(); // finally, render
    }

    /// <summary>
    /// Loads data from a file. Called by LoadPlanet().
    /// </summary>
    /// <returns>data from which the planet is reconstructed</returns>
    public PlanetBinaryData Load()
    {
        PlanetBinaryData data = new PlanetBinaryData();
        FileStream fs = new FileStream(filename, FileMode.Open); // open the file for memory copy
        byte[] bytes = new byte[fs.Length]; // file contents variable
        fs.Read(bytes, 0, (int)fs.Length); // load the whole file at once
        SimpleReadStream ms = new SimpleReadStream(); // stream providing the data as needed
        ms.m_BufferSize = bytes.Length;
        ms.m_StreamIndex = 0;
        ms.m_Buffer = bytes;
        fs.Close(); // close the file

        string header;
        int header_size, version;
        byte[] value_read = new byte[4]; // universal buffer, used almost exclusively (save for header)

        ms.Read(value_read, 0, 4); // standard bytes read
        header_size = BitConverter.ToInt32(value_read, 0); // standard conversion and assignment

        byte[] header_read = new byte[header_size];
        ms.Read(header_read, 0, header_size);
        header = Encoding.ASCII.GetString(header_read);

        ms.Read(value_read, 0, 4);
        version = BitConverter.ToInt32(value_read, 0);


        ms.Read(value_read, 0, 4); // bool as an int
        data.m_TectonicsPresent = BitConverter.ToInt32(value_read, 0) > 0 ? true : false;
        bool tectonics_present = data.m_TectonicsPresent;
        ms.Read(value_read, 0, 4);
        data.m_RandomMTI = BitConverter.ToUInt32(value_read, 0);
        data.m_RandomMT = new List<uint>();
        for (int i = 0; i < RandomMersenne.MERS_N; i++)
        {
            ms.Read(value_read, 0, 4);
            data.m_RandomMT.Add(BitConverter.ToUInt32(value_read, 0));
        }
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
        Debug.Log("File " + filename + " loaded: header [" + header + "], version " + version); // feedback
        return data;
    }

    /// <summary>
    /// Save overlay texture for reference.
    /// </summary>
    public void TextureSave()
    {
        Texture2D tex = (Texture2D)man.m_Surface.GetComponent<Renderer>().sharedMaterial.GetTexture("_MainTex"); // read directly from painted object, does not work if texture is unassigned

        if (tex != null)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(man.m_TextureSaveFilenamePNG, bytes);
        } else
        {
            Debug.LogError("No texture, cannot export!");
        }
    }

    // a single binary file must be formatted in the following manner:
    // first 4 byte int is the number of vertices -> V_n
    // V_n groups of 3*8 byte values (double) follow with the specific XYZ coordinates - the interpreter switches Y for Z because of Unity's coordinate system
    // 4 byte int for the number of triangles -> T_n
    // T_n 3*4 byte values (int) with the vertex indices for each triangle - matched to the read array of vertices
    // V_n groups of vertex neighbours, each starts with 4 byte int for the number of neighbours -> Vng_n, then Vng_n 4 byte int values for neighbours indices in the vertex array
    // T_n groups of triangle neighbours, each has three 4 byte int values as neighbours indices in the triangle array (assume correct spherical topology)
    /// <summary>
    /// Loads template triangulation and auxiliary data from a file.
    /// </summary>
    /// <param name="vertices_p">vertex locations</param>
    /// <param name="triangles_p">triangle indices</param>
    /// <param name="vertices_neighbours_p">triangulation vertex neighbours</param>
    /// <param name="triangles_of_vertices_p">triangles belonging to vertices</param>
    /// <param name="filename">filename to be read</param>
    public void ReadMesh(out List<Vector3> vertices_p, out List<DRTriangle> triangles_p, out List<List<int>> vertices_neighbours_p, out List<List<int>> triangles_of_vertices_p, string filename)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<DRTriangle> triangles = new List<DRTriangle>();
        List<List<int>> vertices_neighbours = new List<List<int>>();
        List<List<int>> triangles_of_vertices = new List<List<int>>();
        List<int> vertex_neighbours;
        string file_path = Application.dataPath + @"\Data/" + filename; // templates are in a subdirectory
        if (!File.Exists(file_path))
        {
            Debug.LogError("file " + file_path + " does not exist");
        }
        else
        {
            FileStream ps = new FileStream(file_path, FileMode.Open); // contents are read into a SimpleReadStream and the file closed
            byte[] buffer = new byte[ps.Length];
            ps.Read(buffer, 0, buffer.Length);
            SimpleReadStream fs = new SimpleReadStream(); // fs is recycled, so that old code could be kept
            fs.m_StreamIndex = 0;
            fs.m_BufferSize = buffer.Length;
            fs.m_Buffer = buffer;
            ps.Close();
            byte[] int_read = new byte[4];
            int n_vertices, n_triangles, n_vertex_neighbours, vertex_neighbour_index;
            fs.Read(int_read, 0, 4);
            n_vertices = BitConverter.ToInt32(int_read, 0);
            byte[] vectorx_read = new byte[8];
            byte[] vectory_read = new byte[8];
            byte[] vectorz_read = new byte[8];
            for (int i = 0; i < n_vertices; i++)
            {
                fs.Read(vectorx_read, 0, 8);
                fs.Read(vectory_read, 0, 8);
                fs.Read(vectorz_read, 0, 8);
                Vector3 new_vector = Vector3.zero;
                new_vector.x = (float)BitConverter.ToDouble(vectorx_read, 0);
                new_vector.z = (float)BitConverter.ToDouble(vectory_read, 0);
                new_vector.y = (float)BitConverter.ToDouble(vectorz_read, 0);
                vertices.Add(new_vector.normalized);
                triangles_of_vertices.Add(new List<int>());
            }

            fs.Read(int_read, 0, 4);
            n_triangles = BitConverter.ToInt32(int_read, 0);
            byte[] trianglea_read = new byte[4];
            byte[] triangleb_read = new byte[4];
            byte[] trianglec_read = new byte[4];
            int a, b, c;
            for (int i = 0; i < n_triangles; i++)
            {
                fs.Read(trianglea_read, 0, 4);
                fs.Read(triangleb_read, 0, 4);
                fs.Read(trianglec_read, 0, 4);
                a = BitConverter.ToInt32(trianglea_read, 0);
                b = BitConverter.ToInt32(triangleb_read, 0);
                c = BitConverter.ToInt32(trianglec_read, 0);
                DRTriangle new_triangle = new DRTriangle(a, b, c, vertices);
                triangles.Add(new_triangle);
            }
            byte[] n_vertex_neighbours_read = new byte[4];
            byte[] neighbour_read = new byte[4];
            for (int i = 0; i < n_vertices; i++)
            {
                vertex_neighbours = new List<int>();

                fs.Read(n_vertex_neighbours_read, 0, 4);
                n_vertex_neighbours = BitConverter.ToInt32(n_vertex_neighbours_read, 0);
                vertex_neighbours.Add(n_vertex_neighbours);
                for (int j = 0; j < n_vertex_neighbours; j++)
                {
                    fs.Read(neighbour_read, 0, 4);
                    vertex_neighbour_index = BitConverter.ToInt32(neighbour_read, 0);
                    vertex_neighbours.Add(vertex_neighbour_index);
                }
                vertices_neighbours.Add(vertex_neighbours);
            }
            for (int i = 0; i < n_triangles; i++)
            {
                fs.Read(trianglea_read, 0, 4);
                fs.Read(triangleb_read, 0, 4);
                fs.Read(trianglec_read, 0, 4);
                a = BitConverter.ToInt32(trianglea_read, 0);
                b = BitConverter.ToInt32(triangleb_read, 0);
                c = BitConverter.ToInt32(trianglec_read, 0);
                triangles[i].m_Neighbours.Add(a);
                triangles[i].m_Neighbours.Add(b);
                triangles[i].m_Neighbours.Add(c);
                triangles_of_vertices[triangles[i].m_A].Add(i);
                triangles_of_vertices[triangles[i].m_B].Add(i);
                triangles_of_vertices[triangles[i].m_C].Add(i);
            }
        }
        vertices_p = vertices; // final assignments
        triangles_p = triangles;
        vertices_neighbours_p = vertices_neighbours;
        triangles_of_vertices_p = triangles_of_vertices;
    }


}
