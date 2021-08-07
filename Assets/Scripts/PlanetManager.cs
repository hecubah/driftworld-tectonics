﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlanetManager : MonoBehaviour
{
    public float m_Radius = 1.0f; // multiple of 1000 km
    [HideInInspector] public GameObject m_Surface = null;
    public TectonicPlanet m_DataMathSphere = null;
    public TectonicPlanet m_RenderMathSphere = null;
    public TectonicPlanet m_Planet = null;

    public string m_DataMeshFilename = "";
    public string m_RenderMeshFilename = "";
    public ComputeShader m_DefaultTerrainTextureCShader = null;
    public ComputeShader m_PlatesAreaTextureCShader = null;
    public ComputeShader m_FractalTerrainCShader = null;
    public ComputeShader m_TriangleCollisionTestCShader = null;
    public ComputeShader m_CircleMergeShader = null;
    public ComputeShader m_BVHNearestNeighbourShader = null;
    public ComputeShader m_CrustToDataShader = null;
    public ComputeShader m_DataToRenderShader = null;
    public ComputeShader m_TerrainesConstructShader = null;
    //public ComputeShader m_BVHContureTestShader = null;

    public uint m_RandomSeed = 0;

    public uint m_BVHTreeDebugLevel = 0;
    public float m_BVHTreeDebugBand = 0.1f;
    public int m_DebugTreeLevel = 0;
    public float m_DebugPointRange = 0.1f;
    public int m_NumberOfMissing = 0;

    public RandomMersenne m_Random;

    [HideInInspector] public string m_RenderMode = "";
    [HideInInspector] public bool m_PropagateCrust = false;
    [HideInInspector] public bool m_PropagateData = false;
    [HideInInspector] public bool m_ClampToOceanLevel = false;


    public void DebugFunction()
    {
        ComputeShader work_shader = m_TerrainesConstructShader;
        int n_loops = 100000;
        int kernelHandle = work_shader.FindKernel("CSTerrainesConstruct");
        int size = 1500;
        int [] array = new int[size];
        for (int i = 0; i < size; i++)
        {
            array[i] = 0;
        }

        ComputeBuffer array_buffer = new ComputeBuffer(size, 4, ComputeBufferType.Default);
        array_buffer.SetData(array);
        work_shader.SetBuffer(kernelHandle, "checks", array_buffer);
        work_shader.SetInt("n_loops", n_loops);
        work_shader.SetInt("size", size);
        work_shader.Dispatch(kernelHandle, size / 64 + (size % 64 != 0 ? 1 : 0), 1, 1);

        array_buffer.GetData(array);
        array_buffer.Release();

        int sum = 0;
        for (int i = 0; i < size; i++)
        {
            sum += array[i];
        }
        Debug.Log("Součet je " + sum);




    }

    public void DebugFunction2()
    {
    }

    public void DebugFunction3()
    {
    }

    public void DebugFunction4()
    {
    }

    // Start is called before the first frame update
    void Start()
    {
        m_Random = new RandomMersenne(m_RandomSeed);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadNewPlanet()
    {
        if (m_Surface == null)
        {
            m_Surface = new GameObject("Surface");
            m_Surface.transform.parent = transform;
            MeshFilter newMeshFilter = m_Surface.AddComponent<MeshFilter>();
            m_Surface.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Custom/SphereTextureShader"));
            newMeshFilter.sharedMesh = new Mesh();
        }
        if (m_Random == null)
        {
            m_Random = new RandomMersenne(m_RandomSeed);
        }
        m_Planet = new TectonicPlanet(m_Radius);
        m_Planet.LoadDefaultTopology(m_DataMeshFilename, m_RenderMeshFilename);
        m_RenderMode = "normal";
        RenderSurfaceMesh();
        m_Surface.GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", null);
        GameObject.Find("TexturePlane").GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", null);
    }

    public void RenderSurfaceMesh()
    {
        MeshFilter meshFilter = m_Surface.GetComponent<MeshFilter>();
        meshFilter.sharedMesh.Clear();
        Vector3[] vertices;
        int[] triangles;
        switch (m_RenderMode)
        {
            case "plates":
                m_Planet.CrustMesh(out vertices, out triangles);
                break;
            case "data":
                m_Planet.DataMesh(out vertices, out triangles, m_PropagateCrust);
                break;
            case "normal":
                m_Planet.NormalMesh(out vertices, out triangles, m_PropagateData, m_PropagateCrust);
                break;
            default:
                m_Planet.NormalMesh(out vertices, out triangles, m_PropagateData, m_PropagateCrust);
                break;
        }
        meshFilter.sharedMesh.vertices = vertices;
        meshFilter.sharedMesh.triangles = triangles;
        meshFilter.sharedMesh.RecalculateNormals();

    }

    public void CAPTerrainTexture(TectonicPlanet sphere)
    {
        int kernelHandle = m_DefaultTerrainTextureCShader.FindKernel("CSDefaultTerrainTexture");

        RenderTexture com_tex = new RenderTexture(4096, 4096, 24);
        com_tex.enableRandomWrite = true;
        com_tex.Create();

        Vector3[] triangle_points = new Vector3[3 * sphere.m_TrianglesCount];
        float[] point_values = new float[3 * sphere.m_TrianglesCount];
        int[] triangle_neighbours = new int[3 * sphere.m_TrianglesCount];
        for (int i = 0; i < sphere.m_TrianglesCount; i++)
        {
            triangle_points[3 * i + 0] = sphere.m_DataVertices[sphere.m_DataTriangles[i].m_A];
            triangle_points[3 * i + 1] = sphere.m_DataVertices[sphere.m_DataTriangles[i].m_B];
            triangle_points[3 * i + 2] = sphere.m_DataVertices[sphere.m_DataTriangles[i].m_C];
            point_values[3 * i + 0] = sphere.m_DataPointData[sphere.m_DataTriangles[i].m_A].elevation;
            point_values[3 * i + 1] = sphere.m_DataPointData[sphere.m_DataTriangles[i].m_B].elevation;
            point_values[3 * i + 2] = sphere.m_DataPointData[sphere.m_DataTriangles[i].m_C].elevation;
            triangle_neighbours[3 * i + 0] = sphere.m_DataTriangles[i].m_Neighbours[0];
            triangle_neighbours[3 * i + 1] = sphere.m_DataTriangles[i].m_Neighbours[1];
            triangle_neighbours[3 * i + 2] = sphere.m_DataTriangles[i].m_Neighbours[2];
        }

        ComputeBuffer triangle_points_buffer = new ComputeBuffer(triangle_points.Length, 12, ComputeBufferType.Default);
        ComputeBuffer point_values_buffer = new ComputeBuffer(point_values.Length, 4, ComputeBufferType.Default);
        ComputeBuffer triangle_neighbours_buffer = new ComputeBuffer(triangle_neighbours.Length, 4, ComputeBufferType.Default);
        triangle_points_buffer.SetData(triangle_points);
        point_values_buffer.SetData(point_values);
        triangle_neighbours_buffer.SetData(triangle_neighbours);

        m_DefaultTerrainTextureCShader.SetBuffer(kernelHandle, "triangle_points", triangle_points_buffer);
        m_DefaultTerrainTextureCShader.SetBuffer(kernelHandle, "point_values", point_values_buffer);
        m_DefaultTerrainTextureCShader.SetBuffer(kernelHandle, "triangle_neighbours", triangle_neighbours_buffer);
        m_DefaultTerrainTextureCShader.SetInt("trianglesNumber", sphere.m_TrianglesCount);
        m_DefaultTerrainTextureCShader.SetTexture(kernelHandle, "Result", com_tex);
        m_DefaultTerrainTextureCShader.Dispatch(kernelHandle, 256, 1024, 1);
        triangle_points_buffer.Release();
        point_values_buffer.Release();
        triangle_neighbours_buffer.Release();

        RenderTexture.active = com_tex;
        Texture2D tex = new Texture2D(com_tex.width, com_tex.height);
        tex.ReadPixels(new Rect(0, 0, com_tex.width, com_tex.height), 0, 0);
        RenderTexture.active = null;
        com_tex.Release();
        tex.Apply();
        GameObject.Find("TexturePlane").GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", tex);
        m_Surface.GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", tex);
    }

    public void CAPPlatesAreaTexture(TectonicPlanet sphere)
    {


        int kernelHandle = m_PlatesAreaTextureCShader.FindKernel("CSPlatesAreaTexture");

        RenderTexture com_tex = new RenderTexture(4096, 4096, 24);
        com_tex.enableRandomWrite = true;
        com_tex.Create();

        Vector3[] triangle_points = new Vector3[3 * sphere.m_TrianglesCount];
        int[] point_values = new int[3 * sphere.m_TrianglesCount];
        int[] triangle_neighbours = new int[3 * sphere.m_TrianglesCount];
        for (int i = 0; i < sphere.m_TrianglesCount; i++)
        {
            triangle_points[3 * i + 0] = sphere.m_CrustVertices[sphere.m_CrustTriangles[i].m_A];
            triangle_points[3 * i + 1] = sphere.m_CrustVertices[sphere.m_CrustTriangles[i].m_B];
            triangle_points[3 * i + 2] = sphere.m_CrustVertices[sphere.m_CrustTriangles[i].m_C];
            point_values[3 * i + 0] = sphere.m_CrustPointData[sphere.m_CrustTriangles[i].m_A].plate;
            point_values[3 * i + 1] = sphere.m_CrustPointData[sphere.m_CrustTriangles[i].m_B].plate;
            point_values[3 * i + 2] = sphere.m_CrustPointData[sphere.m_CrustTriangles[i].m_C].plate;
            triangle_neighbours[3 * i + 0] = sphere.m_CrustTriangles[i].m_Neighbours[0];
            triangle_neighbours[3 * i + 1] = sphere.m_CrustTriangles[i].m_Neighbours[1];
            triangle_neighbours[3 * i + 2] = sphere.m_CrustTriangles[i].m_Neighbours[2];
        }

        int[] overlap_matrix = new int[sphere.m_TectonicPlatesCount * sphere.m_TectonicPlatesCount];
        int[] BVH_array_sizes = new int[sphere.m_TectonicPlatesCount];
        List<BoundingVolumeStruct> BVArray_pass = new List<BoundingVolumeStruct>();
        Vector4 [] plate_transforms = new Vector4[sphere.m_TectonicPlatesCount];

        int[] vertex_plates = new int[sphere.m_VerticesCount];
        for (int i = 0; i < sphere.m_VerticesCount; i++)
        {
            vertex_plates[i] = sphere.m_CrustPointData[i].plate;
        }

        for (int i = 0; i < sphere.m_TectonicPlatesCount; i++)
        {
            for (int j = 0; j < sphere.m_TectonicPlatesCount; j++)
            {
                overlap_matrix[i * sphere.m_TectonicPlatesCount + j] = sphere.m_PlatesOverlap[i, j];
            }
            BVH_array_sizes[i] = sphere.m_TectonicPlates[i].m_BVHArray.Count;
            BVArray_pass.AddRange(sphere.m_TectonicPlates[i].m_BVHArray);
            Vector4 added_transform = new Vector4();
            added_transform.x = sphere.m_TectonicPlates[i].m_Transform.x;
            added_transform.y = sphere.m_TectonicPlates[i].m_Transform.y;
            added_transform.z = sphere.m_TectonicPlates[i].m_Transform.z;
            added_transform.w = sphere.m_TectonicPlates[i].m_Transform.w;
            plate_transforms[i] = added_transform;
              
        }
        BoundingVolumeStruct[] BVArray_finished = BVArray_pass.ToArray();

        ComputeBuffer triangle_points_buffer = new ComputeBuffer(triangle_points.Length, 12, ComputeBufferType.Default);
        ComputeBuffer point_values_buffer = new ComputeBuffer(point_values.Length, 4, ComputeBufferType.Default);
        ComputeBuffer overlap_matrix_buffer = new ComputeBuffer(overlap_matrix.Length, 4, ComputeBufferType.Default);
        ComputeBuffer BVH_array_sizes_buffer = new ComputeBuffer(BVH_array_sizes.Length, 4, ComputeBufferType.Default);
        ComputeBuffer BVArray_finished_buffer = new ComputeBuffer(BVArray_finished.Length, 32, ComputeBufferType.Default);
        ComputeBuffer plate_transforms_buffer = new ComputeBuffer(plate_transforms.Length, 16, ComputeBufferType.Default);
        ComputeBuffer triangle_neighbours_buffer = new ComputeBuffer(triangle_neighbours.Length, 4, ComputeBufferType.Default);
        ComputeBuffer vertex_plates_buffer = new ComputeBuffer(vertex_plates.Length, 4, ComputeBufferType.Default);


        triangle_points_buffer.SetData(triangle_points);
        point_values_buffer.SetData(point_values);
        triangle_neighbours_buffer.SetData(triangle_neighbours);
        vertex_plates_buffer.SetData(vertex_plates);

        overlap_matrix_buffer.SetData(overlap_matrix);
        BVH_array_sizes_buffer.SetData(BVH_array_sizes);
        BVArray_finished_buffer.SetData(BVArray_finished);
        plate_transforms_buffer.SetData(plate_transforms);

        m_PlatesAreaTextureCShader.SetBuffer(kernelHandle, "triangle_points", triangle_points_buffer);
        m_PlatesAreaTextureCShader.SetBuffer(kernelHandle, "point_values", point_values_buffer);
        m_PlatesAreaTextureCShader.SetInt("n_plates", sphere.m_TectonicPlatesCount);

        m_PlatesAreaTextureCShader.SetBuffer(kernelHandle, "overlap_matrix", overlap_matrix_buffer);
        m_PlatesAreaTextureCShader.SetBuffer(kernelHandle, "BVH_array_sizes", BVH_array_sizes_buffer);
        m_PlatesAreaTextureCShader.SetBuffer(kernelHandle, "BVH_array", BVArray_finished_buffer);
        m_PlatesAreaTextureCShader.SetBuffer(kernelHandle, "plate_transforms", plate_transforms_buffer);
        m_PlatesAreaTextureCShader.SetBuffer(kernelHandle, "triangle_neighbours", triangle_neighbours_buffer);
        m_PlatesAreaTextureCShader.SetBuffer(kernelHandle, "vertex_plates", vertex_plates_buffer);

        m_PlatesAreaTextureCShader.SetTexture(kernelHandle, "Result", com_tex);
        m_PlatesAreaTextureCShader.Dispatch(kernelHandle, 256, 1024, 1);

        triangle_points_buffer.Release();
        point_values_buffer.Release();

        overlap_matrix_buffer.Release();
        BVH_array_sizes_buffer.Release();
        BVArray_finished_buffer.Release();
        plate_transforms_buffer.Release();

        triangle_neighbours_buffer.Release();
        vertex_plates_buffer.Release();

        RenderTexture.active = com_tex;
        Texture2D tex = new Texture2D(com_tex.width, com_tex.height);
        tex.ReadPixels(new Rect(0, 0, com_tex.width, com_tex.height), 0, 0);
        RenderTexture.active = null;
        tex.Apply();
        com_tex.Release();
        GameObject.Find("TexturePlane").GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", tex);
        m_Surface.GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", tex);
    }

    public void CAPTriangleCollisionTestTexture()
    {
        float mintR = 0.5f;
        float maxtR = 2f;
        List<Vector3> vertices = new List<Vector3>();

        Vector3 random_first = new Vector3(m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f)).normalized;
        Vector3 r_planex = new Vector3(random_first.y, -random_first.x, 0f).normalized;
        Vector3 r_planey = Vector3.Cross(random_first, r_planex);

        float randR = m_Random.Range(mintR, maxtR);
        for (int i = 0; i < 3; i++)
        {
            float randPhi = m_Random.Range(0f, 2 * Mathf.PI);
            vertices.Add((random_first + randR * Mathf.Cos(randPhi) * r_planex + randR * Mathf.Sin(randPhi) * r_planey).normalized);
        }

        Vector3 random_second = new Vector3(m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f)).normalized;
        r_planex = new Vector3(random_second.y, -random_second.x, 0f).normalized;
        r_planey = Vector3.Cross(random_second, r_planex);

        randR = m_Random.Range(mintR, maxtR);
        for (int i = 0; i < 3; i++)
        {
            float randPhi = m_Random.Range(0f, 2 * Mathf.PI);
            vertices.Add((random_second + randR * Mathf.Cos(randPhi) * r_planex + randR * Mathf.Sin(randPhi) * r_planey).normalized);
        }

        DRTriangle a = new DRTriangle(0, 1, 2, vertices);
        DRTriangle b = new DRTriangle(3, 4, 5, vertices);
        a.EnsureClockwiseOrientation();
        b.EnsureClockwiseOrientation();

        Debug.Log(DRTriangle.Collision(a,b).ToString());

        int kernelHandle = m_TriangleCollisionTestCShader.FindKernel("CSTriangleCollisionTest");

        RenderTexture com_tex = new RenderTexture(4096, 4096, 24);
        com_tex.enableRandomWrite = true;
        com_tex.Create();


        Vector3[] triangle_points = vertices.ToArray();
        int[] triangle_vertices = new int[6];

        triangle_vertices[0] = a.m_A;
        triangle_vertices[1] = a.m_B;
        triangle_vertices[2] = a.m_C;
        triangle_vertices[3] = b.m_A;
        triangle_vertices[4] = b.m_B;
        triangle_vertices[5] = b.m_C;
        
        ComputeBuffer triangle_points_buffer = new ComputeBuffer(triangle_points.Length, 12, ComputeBufferType.Default);
        ComputeBuffer triangle_vertices_buffer = new ComputeBuffer(triangle_vertices.Length, 4, ComputeBufferType.Default);

        triangle_points_buffer.SetData(triangle_points);
        triangle_vertices_buffer.SetData(triangle_vertices);

        m_TriangleCollisionTestCShader.SetBuffer(kernelHandle, "triangle_points", triangle_points_buffer);
        m_TriangleCollisionTestCShader.SetBuffer(kernelHandle, "triangle_vertices", triangle_vertices_buffer);
        m_TriangleCollisionTestCShader.SetTexture(kernelHandle, "Result", com_tex);
        m_TriangleCollisionTestCShader.Dispatch(kernelHandle, 256, 1024, 1);
        triangle_points_buffer.Release();
        triangle_vertices_buffer.Release();
        RenderTexture.active = com_tex;
        Texture2D tex = new Texture2D(com_tex.width, com_tex.height);
        tex.ReadPixels(new Rect(0, 0, com_tex.width, com_tex.height), 0, 0);
        RenderTexture.active = null;
        tex.Apply();
        com_tex.Release();
        GameObject.Find("TexturePlane").GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", tex);
        m_Surface.GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTex", tex);
    }

}

