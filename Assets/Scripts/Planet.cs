using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TectonicPlanet
{
    public PlanetManager m_PlanetManager; // manager object for backcalls and configuration calls

    public float m_Radius; // radius of the planet

    public RandomMersenne m_Random; // reference object to manager RNG

    public List<Vector3> m_CrustVertices; // vertices belonging to the crust layer
    public List<DRTriangle> m_CrustTriangles; // All triangles belonging to the crust layer - crust movement flips some of them
    public List<PointData> m_CrustPointData;

    public List<Vector3> m_DataVertices;
    public List<DRTriangle> m_DataTriangles;
    public List<List<int>> m_DataVerticesNeighbours;
    public List<List<int>> m_DataTrianglesOfVertices;
    public List<PointData> m_DataPointData;
    public BoundingVolume m_DataBVH;
    public List<BoundingVolumeStruct> m_DataBVHArray;

    public int m_VerticesCount;
    public int m_TrianglesCount;
    public int m_TectonicStepsTakenWithoutResample;
    public int m_TotalTectonicStepsTaken;

    public List<int> m_LookupStartTriangles;

    public List<Vector3> m_RenderVertices;
    public List<DRTriangle> m_RenderTriangles;
    public List<List<int>> m_RenderVerticesNeighbours;
    public List<List<int>> m_RenderTrianglesOfVertices;
    public List<PointData> m_RenderPointData;

    public List<Vector3> m_VectorNoise;

    public int m_RenderVerticesCount;
    public int m_RenderTrianglesCount;

    public int m_TectonicPlatesCount;
    public List<Plate> m_TectonicPlates;

    public int[,] m_PlatesOverlap; // matrix saying if row overlaps column (1 if it does, -1 if it goes under)

    public Dictionary<string, ComputeBuffer> m_CBuffers;
    public Dictionary<string, bool> m_CBufferUpdatesNeeded;

    public TectonicPlanet(float radius)
    {
        m_PlanetManager = (PlanetManager)GameObject.Find("Planet").GetComponent(typeof(PlanetManager));

        m_Radius = radius;

        m_Random = m_PlanetManager.m_Random;

        m_CrustVertices = new List<Vector3>();
        m_CrustTriangles = new List<DRTriangle>();
        //m_CrustVerticesNeighbours = new List<List<int>>();
        //m_CrustTrianglesOfVertices = new List<List<int>>();
        m_CrustPointData = new List<PointData>();

        m_DataVertices = new List<Vector3>();
        m_DataTriangles = new List<DRTriangle>();
        m_DataVerticesNeighbours = new List<List<int>>();
        m_DataTrianglesOfVertices = new List<List<int>>();
        m_DataPointData = new List<PointData>();
        m_DataBVH = null;
        m_DataBVHArray = null;

        m_VerticesCount = 0;
        m_TrianglesCount = 0;

        m_LookupStartTriangles = new List<int>();

        m_RenderVertices = new List<Vector3>();
        m_RenderTriangles = new List<DRTriangle>();
        m_RenderVerticesNeighbours = new List<List<int>>();
        m_RenderTrianglesOfVertices = new List<List<int>>();
        m_RenderPointData = new List<PointData>();

        m_VectorNoise = new List<Vector3>();

        m_RenderVerticesCount = 0;
        m_RenderTrianglesCount = 0;

        m_TectonicPlates = new List<Plate>();
        m_PlatesOverlap = null;
        m_CBuffers = new Dictionary<string, ComputeBuffer>();
        m_CBufferUpdatesNeeded = new Dictionary<string, bool>();

        m_TectonicStepsTakenWithoutResample = 0;
        m_TotalTectonicStepsTaken = 0;

    InitializeCBuffers();
    }

    public void InitializeCBuffers()
    {
        m_CBufferUpdatesNeeded.Clear();
        foreach (KeyValuePair<string, ComputeBuffer> it in m_CBuffers)
        {
            if (it.Value != null)
            {
                it.Value.Release();
            }
        }
        m_CBuffers.Clear();
        List<string> reload_keys = new List<string>();
        reload_keys.Add("crust_vertex_locations");
        reload_keys.Add("crust_triangles");
        reload_keys.Add("crust_vertex_data");
        reload_keys.Add("plate_transforms");
        reload_keys.Add("plate_transforms_predictive");
        reload_keys.Add("overlap_matrix");
        reload_keys.Add("crust_BVH");
        reload_keys.Add("crust_BVH_sps");
        reload_keys.Add("crust_border_triangles");
        reload_keys.Add("crust_border_triangles_sps");
        reload_keys.Add("data_vertex_locations");
        reload_keys.Add("data_triangles");
        reload_keys.Add("data_vertex_data");
        reload_keys.Add("data_BVH");
        reload_keys.Add("render_vertex_locations");
        reload_keys.Add("render_vertex_data");
        reload_keys.Add("plate_motion_axes");
        reload_keys.Add("plate_motion_angular_speeds");

        foreach (string it in reload_keys)
        {
            m_CBuffers[it] = null;
            m_CBufferUpdatesNeeded[it] = true;
        }
    }

    public void UpdateCBBuffers()
    {
        if (m_CrustVertices.Count > 0) {
            if (m_CBufferUpdatesNeeded["crust_vertex_locations"])
            {
                if (m_CBuffers["crust_vertex_locations"] != null)
                {
                    m_CBuffers["crust_vertex_locations"].Release();
                }
                m_CBuffers["crust_vertex_locations"] = new ComputeBuffer(m_VerticesCount, 12, ComputeBufferType.Default);
                m_CBuffers["crust_vertex_locations"].SetData(m_CrustVertices.ToArray());
                m_CBufferUpdatesNeeded["crust_vertex_locations"] = false;
            }
            if (m_CBufferUpdatesNeeded["crust_triangles"])
            {
                if (m_CBuffers["crust_triangles"] != null)
                {
                    m_CBuffers["crust_triangles"].Release();

                }
                m_CBuffers["crust_triangles"] = new ComputeBuffer(m_TrianglesCount, 40, ComputeBufferType.Default);
                CS_Triangle[] crust_triangles_array = new CS_Triangle[m_TrianglesCount];
                for (int i = 0; i < m_TrianglesCount; i++)
                {
                    crust_triangles_array[i] = new CS_Triangle(m_CrustTriangles[i].m_A, m_CrustTriangles[i].m_B, m_CrustTriangles[i].m_C, m_CrustTriangles[i].m_Neighbours[0], m_CrustTriangles[i].m_Neighbours[1], m_CrustTriangles[i].m_Neighbours[2], m_CrustTriangles[i].m_CCenter, m_CrustTriangles[i].m_CUnitRadius);
                }
                m_CBuffers["crust_triangles"].SetData(crust_triangles_array);
                m_CBufferUpdatesNeeded["crust_triangles"] = false;
            }
            if (m_CBufferUpdatesNeeded["crust_vertex_data"])
            {
                if (m_CBuffers["crust_vertex_data"] != null)
                {
                    m_CBuffers["crust_vertex_data"].Release();
                }
                m_CBuffers["crust_vertex_data"] = new ComputeBuffer(m_VerticesCount, 16, ComputeBufferType.Default);
                CS_VertexData[] crust_vertex_data_array = new CS_VertexData[m_VerticesCount];
                for (int i = 0; i < m_VerticesCount; i++)
                {
                    crust_vertex_data_array[i] = new CS_VertexData(m_CrustPointData[i]);
                }
                m_CBuffers["crust_vertex_data"].SetData(crust_vertex_data_array);
                m_CBufferUpdatesNeeded["crust_vertex_data"] = false;
            }
            if (m_CBufferUpdatesNeeded["plate_transforms"])
            {
                if (m_CBuffers["plate_transforms"] != null)
                {
                    m_CBuffers["plate_transforms"].Release();
                }
                m_CBuffers["plate_transforms"] = new ComputeBuffer(m_TectonicPlatesCount, 16, ComputeBufferType.Default);
                Vector4[] plate_transforms_array = new Vector4[m_TectonicPlatesCount];
                Vector4 added_transform;
                for (int i = 0; i < m_TectonicPlatesCount; i++)
                {
                    added_transform = Vector4.zero;
                    added_transform.x = m_TectonicPlates[i].m_Transform.x;
                    added_transform.y = m_TectonicPlates[i].m_Transform.y;
                    added_transform.z = m_TectonicPlates[i].m_Transform.z;
                    added_transform.w = m_TectonicPlates[i].m_Transform.w;
                    plate_transforms_array[i] = added_transform;
                }
                m_CBuffers["plate_transforms"].SetData(plate_transforms_array);
                m_CBufferUpdatesNeeded["plate_transforms"] = false;
            }

            if (m_CBufferUpdatesNeeded["plate_transforms_predictive"])
            {
                if (m_CBuffers["plate_transforms_predictive"] != null)
                {
                    m_CBuffers["plate_transforms_predictive"].Release();
                }
                m_CBuffers["plate_transforms_predictive"] = new ComputeBuffer(m_TectonicPlatesCount, 16, ComputeBufferType.Default);
                Quaternion predicted_transform;
                Vector4[] plate_transforms_array = new Vector4[m_TectonicPlatesCount];
                Vector4 added_transform;
                for (int i = 0; i < m_TectonicPlatesCount; i++)
                {
                    predicted_transform = Quaternion.AngleAxis(m_PlanetManager.m_Settings.TectonicIterationStepTime * m_TectonicPlates[i].m_PlateAngularSpeed * 180.0f / Mathf.PI, m_TectonicPlates[i].m_RotationAxis) * m_TectonicPlates[i].m_Transform;
                    added_transform = Vector4.zero;
                    added_transform.x = predicted_transform.x;
                    added_transform.y = predicted_transform.y;
                    added_transform.z = predicted_transform.z;
                    added_transform.w = predicted_transform.w;
                    plate_transforms_array[i] = added_transform;
                }
                m_CBuffers["plate_transforms_predictive"].SetData(plate_transforms_array);
                m_CBufferUpdatesNeeded["plate_transforms_predictive"] = false;
            }


            if (m_CBufferUpdatesNeeded["overlap_matrix"])
            {
                if (m_CBuffers["overlap_matrix"] != null)
                {
                    m_CBuffers["overlap_matrix"].Release();
                }
                m_CBuffers["overlap_matrix"] = new ComputeBuffer(m_TectonicPlatesCount * m_TectonicPlatesCount, 4, ComputeBufferType.Default);
                int[] overlap_matrix_array = new int[m_TectonicPlatesCount * m_TectonicPlatesCount];
                for (int i = 0; i < m_TectonicPlatesCount; i++)
                {
                    for (int j = 0; j < m_TectonicPlatesCount; j++)
                    {
                        overlap_matrix_array[i * m_TectonicPlatesCount + j] = m_PlatesOverlap[i, j];
                    }
                }
                m_CBuffers["overlap_matrix"].SetData(overlap_matrix_array);
                m_CBufferUpdatesNeeded["overlap_matrix"] = false;
            }

            if ((m_CBufferUpdatesNeeded["crust_BVH"]) || (m_CBufferUpdatesNeeded["crust_BVH_sps"]))
            {
                if (m_CBuffers["crust_BVH"] != null)
                {
                    m_CBuffers["crust_BVH"].Release();
                }
                if (m_CBuffers["crust_BVH_sps"] != null)
                {
                    m_CBuffers["crust_BVH_sps"].Release();
                }
                List<BoundingVolumeStruct> crust_BVH_list = new List<BoundingVolumeStruct>();
                int[] crust_BVH_sps_array = new int[m_TectonicPlatesCount + 1];
                for (int i = 0; i < m_TectonicPlatesCount; i++)
                {
                    crust_BVH_sps_array[0] = 0;
                    if (m_TectonicPlates[i].m_BVHPlate != null)
                    {
                        Queue<BoundingVolume> queue_feed = new Queue<BoundingVolume>();
                        int border_index = 0;
                        queue_feed.Enqueue(m_TectonicPlates[i].m_BVHPlate);
                        BoundingVolume source;
                        BoundingVolumeStruct fill;
                        while (queue_feed.Count > 0)
                        {
                            source = queue_feed.Dequeue();
                            fill = new BoundingVolumeStruct();
                            if (source.m_Children.Count == 2)
                            {
                                fill.n_children = 2;
                                fill.left_child = ++border_index;
                                fill.right_child = ++border_index;
                                queue_feed.Enqueue(source.m_Children[0]);
                                queue_feed.Enqueue(source.m_Children[1]);
                                fill.triangle_index = 0;
                                fill.circumcenter = source.m_Circumcenter;
                                fill.circumradius = source.m_Circumradius;
                            }
                            else
                            {
                                fill.n_children = 0;
                                fill.left_child = 0;
                                fill.right_child = 0;
                                fill.triangle_index = source.m_TriangleIndex;
                                fill.circumcenter = source.m_Circumcenter;
                                fill.circumradius = source.m_Circumradius;
                            }
                            crust_BVH_sps_array[i + 1]++;
                            crust_BVH_list.Add(fill);
                        }
                        crust_BVH_sps_array[i + 1] += crust_BVH_sps_array[i];
                    }
                }
                m_CBuffers["crust_BVH"] = new ComputeBuffer(crust_BVH_list.Count, 32, ComputeBufferType.Default);
                m_CBuffers["crust_BVH_sps"] = new ComputeBuffer(m_TectonicPlatesCount + 1, 4, ComputeBufferType.Default);
                m_CBuffers["crust_BVH"].SetData(crust_BVH_list.ToArray());
                m_CBuffers["crust_BVH_sps"].SetData(crust_BVH_sps_array);
                m_CBufferUpdatesNeeded["crust_BVH"] = false;
                m_CBufferUpdatesNeeded["crust_BVH_sps"] = false;
            }


            if (m_CBufferUpdatesNeeded["crust_border_triangles"] || m_CBufferUpdatesNeeded["crust_border_triangles_sps"])
            {
                if (m_CBuffers["crust_border_triangles"] != null)
                {
                    m_CBuffers["crust_border_triangles"].Release();
                }
                if (m_CBuffers["crust_border_triangles_sps"] != null)
                {
                    m_CBuffers["crust_border_triangles_sps"].Release();
                }
                List<int> crust_border_triangles_list = new List<int>();
                int[] crust_border_triangles_sps_array = new int[m_TectonicPlatesCount + 1];
                crust_border_triangles_sps_array[0] = 0;
                for (int i = 0; i < m_TectonicPlatesCount; i++)
                {
                    int triangle_count = m_TectonicPlates[i].m_BorderTriangles.Count;
                    for (int j = 0; j < triangle_count; j++)
                    {
                        crust_border_triangles_list.Add(m_TectonicPlates[i].m_BorderTriangles[j]);
                        crust_border_triangles_sps_array[i + 1]++;
                    }
                    crust_border_triangles_sps_array[i + 1] += crust_border_triangles_sps_array[i];
                }
                m_CBuffers["crust_border_triangles"] = new ComputeBuffer(crust_border_triangles_list.Count, 4, ComputeBufferType.Default);
                m_CBuffers["crust_border_triangles_sps"] = new ComputeBuffer(m_TectonicPlatesCount + 1, 4, ComputeBufferType.Default);
                m_CBuffers["crust_border_triangles"].SetData(crust_border_triangles_list.ToArray());
                m_CBuffers["crust_border_triangles_sps"].SetData(crust_border_triangles_sps_array);
                m_CBufferUpdatesNeeded["crust_border_triangles"] = false;
                m_CBufferUpdatesNeeded["crust_border_triangles_sps"] = false;
            }

            if (m_CBufferUpdatesNeeded["plate_motion_axes"] || m_CBufferUpdatesNeeded["plate_motion_angular_speeds"])
            {
                if (m_CBuffers["plate_motion_axes"] != null)
                {
                    m_CBuffers["plate_motion_axes"].Release();
                }
                if (m_CBuffers["plate_motion_angular_speeds"] != null)
                {
                    m_CBuffers["plate_motion_angular_speeds"].Release();
                }
                Vector3[] plate_motion_axes_array = new Vector3[m_TectonicPlatesCount];
                float[] plate_motion_angular_speeds_array = new float[m_TectonicPlatesCount];
                for (int i = 0; i < m_TectonicPlatesCount; i++)
                {
                    plate_motion_axes_array[i] = m_TectonicPlates[i].m_RotationAxis;
                    plate_motion_angular_speeds_array[i] = m_TectonicPlates[i].m_PlateAngularSpeed;
                }
                m_CBuffers["plate_motion_axes"] = new ComputeBuffer(m_TectonicPlatesCount, 12, ComputeBufferType.Default);
                m_CBuffers["plate_motion_angular_speeds"] = new ComputeBuffer(m_TectonicPlatesCount, 4, ComputeBufferType.Default);
                m_CBuffers["plate_motion_axes"].SetData(plate_motion_axes_array);
                m_CBuffers["plate_motion_angular_speeds"].SetData(plate_motion_angular_speeds_array);
                m_CBufferUpdatesNeeded["plate_motion_axes"] = false;
                m_CBufferUpdatesNeeded["plate_motion_angular_speeds"] = false;
            }

        }
        if (m_CBufferUpdatesNeeded["data_vertex_locations"])
        {
            if (m_CBuffers["data_vertex_locations"] != null)
            {
                m_CBuffers["data_vertex_locations"].Release();
            }
            m_CBuffers["data_vertex_locations"] = new ComputeBuffer(m_VerticesCount, 12, ComputeBufferType.Default);
            m_CBuffers["data_vertex_locations"].SetData(m_DataVertices.ToArray());
            m_CBufferUpdatesNeeded["data_vertex_locations"] = false;
        }
        if (m_CBufferUpdatesNeeded["data_triangles"])
        {
            if (m_CBuffers["data_triangles"] != null)
            {
                m_CBuffers["data_triangles"].Release();

            }
            m_CBuffers["data_triangles"] = new ComputeBuffer(m_TrianglesCount, 40, ComputeBufferType.Default);
            CS_Triangle[] data_triangles_array = new CS_Triangle[m_TrianglesCount];
            for (int i = 0; i < m_TrianglesCount; i++)
            {
                data_triangles_array[i] = new CS_Triangle(m_DataTriangles[i].m_A, m_DataTriangles[i].m_B, m_DataTriangles[i].m_C, m_DataTriangles[i].m_Neighbours[0], m_DataTriangles[i].m_Neighbours[1], m_DataTriangles[i].m_Neighbours[2], m_DataTriangles[i].m_CCenter, m_DataTriangles[i].m_CUnitRadius);
            }
            m_CBuffers["data_triangles"].SetData(data_triangles_array);
            m_CBufferUpdatesNeeded["data_triangles"] = false;
        }
        if (m_CBufferUpdatesNeeded["data_vertex_data"])
        {
            if (m_CBuffers["data_vertex_data"] != null)
            {
                m_CBuffers["data_vertex_data"].Release();
            }
            m_CBuffers["data_vertex_data"] = new ComputeBuffer(m_VerticesCount, 16, ComputeBufferType.Default);
            CS_VertexData[] data_vertex_data_array = new CS_VertexData[m_VerticesCount];
            for (int i = 0; i < m_VerticesCount; i++)
            {
                data_vertex_data_array[i] = new CS_VertexData(m_DataPointData[i]);
            }
            m_CBuffers["data_vertex_data"].SetData(data_vertex_data_array);
            m_CBufferUpdatesNeeded["data_vertex_data"] = false;
        }


        if ((m_CBufferUpdatesNeeded["data_BVH"]))
        {
            if (m_CBuffers["data_BVH"] != null)
            {
                m_CBuffers["data_BVH"].Release();
            }
            List<BoundingVolumeStruct> data_BVH_list = new List<BoundingVolumeStruct>();
            if (m_DataBVH != null)
            {
                Queue<BoundingVolume> queue_feed = new Queue<BoundingVolume>();
                int border_index = 0;
                queue_feed.Enqueue(m_DataBVH);
                BoundingVolume source;
                BoundingVolumeStruct fill;
                while (queue_feed.Count > 0)
                {
                    source = queue_feed.Dequeue();
                    fill = new BoundingVolumeStruct();
                    if (source.m_Children.Count == 2)
                    {
                        fill.n_children = 2;
                        fill.left_child = ++border_index;
                        fill.right_child = ++border_index;
                        queue_feed.Enqueue(source.m_Children[0]);
                        queue_feed.Enqueue(source.m_Children[1]);
                        fill.triangle_index = 0;
                        fill.circumcenter = source.m_Circumcenter;
                        fill.circumradius = source.m_Circumradius;
                    }
                    else
                    {
                        fill.n_children = 0;
                        fill.left_child = 0;
                        fill.right_child = 0;
                        fill.triangle_index = source.m_TriangleIndex;
                        fill.circumcenter = source.m_Circumcenter;
                        fill.circumradius = source.m_Circumradius;
                    }
                    data_BVH_list.Add(fill);
                }
            }
            m_CBuffers["data_BVH"] = new ComputeBuffer(data_BVH_list.Count, 32, ComputeBufferType.Default);
            m_CBuffers["data_BVH"].SetData(data_BVH_list.ToArray());
            m_CBufferUpdatesNeeded["data_BVH"] = false;
        }

        if (m_CBufferUpdatesNeeded["render_vertex_locations"])
        {
            if (m_CBuffers["render_vertex_locations"] != null)
            {
                m_CBuffers["render_vertex_locations"].Release();
            }
            m_CBuffers["render_vertex_locations"] = new ComputeBuffer(m_VerticesCount, 12, ComputeBufferType.Default);
            m_CBuffers["render_vertex_locations"].SetData(m_RenderVertices.ToArray());
            m_CBufferUpdatesNeeded["render_vertex_locations"] = false;
        }

        if (m_CBufferUpdatesNeeded["render_vertex_data"])
        {
            if (m_CBuffers["render_vertex_data"] != null)
            {
                m_CBuffers["render_vertex_data"].Release();
            }
            m_CBuffers["render_vertex_data"] = new ComputeBuffer(m_VerticesCount, 16, ComputeBufferType.Default);
            CS_VertexData[] render_vertex_data_array = new CS_VertexData[m_VerticesCount];
            for (int i = 0; i < m_RenderVerticesCount; i++)
            {
                render_vertex_data_array[i] = new CS_VertexData(m_RenderPointData[i]);
            }
            m_CBuffers["render_vertex_data"].SetData(render_vertex_data_array);
            m_CBufferUpdatesNeeded["render_vertex_data"] = false;
        }

    }

    public static float UnitSphereDistance(Vector3 a, Vector3 b)
    {
        float dot = Vector3.Dot(a, b);
        return Mathf.Acos(dot <= 1.0f ? dot : 1.0f);
    }

    public void CrustToData()
    {
        if (m_TectonicPlates.Count == 0)
        {
            return;
        }
        ComputeShader work_shader = m_PlanetManager.m_Shaders.m_VertexDataInterpolationShader;

        int kernelHandle = work_shader.FindKernel("CSCrustToData");

        UpdateCBBuffers();

        work_shader.SetBuffer(kernelHandle, "crust_vertex_locations", m_CBuffers["crust_vertex_locations"]);
        work_shader.SetBuffer(kernelHandle, "crust_triangles", m_CBuffers["crust_triangles"]);
        work_shader.SetBuffer(kernelHandle, "crust_vertex_data", m_CBuffers["crust_vertex_data"]);
        work_shader.SetInt("n_plates", m_TectonicPlatesCount);
        work_shader.SetInt("tectonic_steps_taken_without_resample", m_TectonicStepsTakenWithoutResample);
        work_shader.SetFloat("tectonic_iteration_step_time", m_PlanetManager.m_Settings.TectonicIterationStepTime);

        work_shader.SetBuffer(kernelHandle, "overlap_matrix", m_CBuffers["overlap_matrix"]);
        work_shader.SetBuffer(kernelHandle, "crust_BVH_sps", m_CBuffers["crust_BVH_sps"]);
        work_shader.SetBuffer(kernelHandle, "crust_BVH", m_CBuffers["crust_BVH"]);
        work_shader.SetBuffer(kernelHandle, "plate_transforms", m_CBuffers["plate_transforms"]);

        work_shader.SetBuffer(kernelHandle, "data_vertex_locations", m_CBuffers["data_vertex_locations"]);
        work_shader.SetBuffer(kernelHandle, "data_vertex_data", m_CBuffers["data_vertex_data"]);
        work_shader.SetFloat("ocean_base_floor", m_PlanetManager.m_Settings.OceanBaseFloor);

        work_shader.SetFloat("highest_oceanic_ridge_elevation", m_PlanetManager.m_Settings.HighestOceanicRidgeElevation);
        work_shader.SetFloat("abyssal_plains_elevation", m_PlanetManager.m_Settings.AbyssalPlainsElevation);
        work_shader.SetFloat("oceanic_ridge_elevation_falloff", m_PlanetManager.m_Settings.OceanicRidgeElevationFalloff);

        work_shader.SetInt("n_data_vertices", m_VerticesCount);

        work_shader.SetBuffer(kernelHandle, "crust_border_triangles", m_CBuffers["crust_border_triangles"]);
        work_shader.SetBuffer(kernelHandle, "crust_border_triangles_sps", m_CBuffers["crust_border_triangles_sps"]);
        

        work_shader.Dispatch(kernelHandle, m_VerticesCount / 64 + (m_VerticesCount % 64 != 0 ? 1 : 0), 1, 1);

        CS_VertexData[] data_out = new CS_VertexData[m_VerticesCount];
        m_CBuffers["data_vertex_data"].GetData(data_out);
        for (int i = 0; i < m_VerticesCount; i++)
        {
            m_DataPointData[i].elevation = Mathf.Min(data_out[i].elevation, m_PlanetManager.m_Settings.HighestContinentalAltitude);
            m_DataPointData[i].plate = data_out[i].plate;
            m_DataPointData[i].age = data_out[i].age;
            m_DataPointData[i].orogeny = (OroType)data_out[i].orogeny;
        }
        m_CBufferUpdatesNeeded["data_vertex_data"] = true;
    }

    public void DataToRender(bool propagate_crust)
    {
        if (propagate_crust)
        {
            CrustToData();
        }
        ComputeShader work_shader = m_PlanetManager.m_Shaders.m_VertexDataInterpolationShader;

        int kernelHandle = work_shader.FindKernel("CSDataToRender");

        UpdateCBBuffers();

        work_shader.SetBuffer(kernelHandle, "data_vertex_locations", m_CBuffers["data_vertex_locations"]);
        work_shader.SetBuffer(kernelHandle, "data_triangles", m_CBuffers["data_triangles"]);
        work_shader.SetBuffer(kernelHandle, "data_vertex_data", m_CBuffers["data_vertex_data"]);

        work_shader.SetBuffer(kernelHandle, "data_BVH", m_CBuffers["data_BVH"]);

        work_shader.SetBuffer(kernelHandle, "render_vertex_locations", m_CBuffers["render_vertex_locations"]);
        work_shader.SetBuffer(kernelHandle, "render_vertex_data", m_CBuffers["render_vertex_data"]);

        work_shader.SetInt("n_render_vertices", m_RenderVerticesCount);

        work_shader.Dispatch(kernelHandle, m_RenderVerticesCount / 64 + (m_RenderVerticesCount % 64 != 0 ? 1 : 0), 1, 1);


        CS_VertexData[] render_out = new CS_VertexData[m_RenderVerticesCount];
        m_CBuffers["render_vertex_data"].GetData(render_out);
        for (int i = 0; i < m_RenderVerticesCount; i++)
        {
            m_RenderPointData[i].elevation = render_out[i].elevation;
            m_RenderPointData[i].plate = render_out[i].plate;
            m_RenderPointData[i].age = render_out[i].age;
            m_RenderPointData[i].orogeny = (OroType) render_out[i].orogeny;
        }
        m_CBufferUpdatesNeeded["render_vertex_data"] = true;

    }

    public void CrustMesh(out Vector3[] vertices_array, out int[] triangles_array)
    {
        vertices_array = new Vector3[m_VerticesCount];
        float elevation;
        for (int i = 0; i < m_VerticesCount; i++)
        {
            //elevation = (m_CrustPointData[i].elevation > 0 ? m_CrustPointData[i].elevation : 0);
            elevation = m_CrustPointData[i].elevation;
            if ((m_PlanetManager.m_ClampToOceanLevel) && (elevation < 0))
            {
                elevation = 0;
            }
            vertices_array[i] = (m_Radius + elevation * m_PlanetManager.m_ElevationScaleFactor) * (m_TectonicPlates[m_CrustPointData[i].plate].m_Transform * m_CrustVertices[i]);
        }
        List<int> triangles = new List<int>();
        for (int i = 0; i < m_TectonicPlatesCount; i++)
        {
            for (int j = 0; j < m_TectonicPlates[i].m_PlateTriangles.Count; j++)
            {
                triangles.Add(m_CrustTriangles[m_TectonicPlates[i].m_PlateTriangles[j]].m_A);
                triangles.Add(m_CrustTriangles[m_TectonicPlates[i].m_PlateTriangles[j]].m_B);
                triangles.Add(m_CrustTriangles[m_TectonicPlates[i].m_PlateTriangles[j]].m_C);
            }
        }
        triangles_array = triangles.ToArray();

    }

    public void DataMesh(out Vector3[] vertices_array, out int[] triangles_array, bool propagate_crust)
    {
        if (propagate_crust)
        {
            CrustToData();
            //CrustToDataRecalculateSamples();
        }
        vertices_array = m_DataVertices.ToArray();
        float elevation;
        for (int i = 0; i < m_VerticesCount; i++)
        {
            elevation = m_DataPointData[i].elevation;
            if ((m_PlanetManager.m_ClampToOceanLevel) && (elevation < 0))
            {
                elevation = 0;
            }
            vertices_array[i] = (m_Radius + elevation * m_PlanetManager.m_ElevationScaleFactor) * vertices_array[i];
        }
        List<int> triangles = new List<int>();
        for (int i = 0; i < m_TrianglesCount; i++)
        {
            triangles.Add(m_DataTriangles[i].m_A);
            triangles.Add(m_DataTriangles[i].m_B);
            triangles.Add(m_DataTriangles[i].m_C);
        }
        triangles_array = triangles.ToArray();

    }

    public void NormalMesh(out Vector3[] vertices_array, out int[] triangles_array, bool propagate_data, bool propagate_crust)
    {
        if (propagate_data)
        {
            DataToRender(propagate_crust);
        }
        vertices_array = m_RenderVertices.ToArray();
        float elevation;
        for (int i = 0; i < m_RenderVerticesCount; i++)
        {
            elevation = m_RenderPointData[i].elevation;
            if ((m_PlanetManager.m_ClampToOceanLevel) && (elevation < 0))
            {
                elevation = 0;
            }
            vertices_array[i] = (m_Radius + elevation * m_PlanetManager.m_ElevationScaleFactor) * vertices_array[i];
        }
        List<int> triangles = new List<int>();
        for (int i = 0; i < m_RenderTrianglesCount; i++)
        {
            triangles.Add(m_RenderTriangles[i].m_A);
            triangles.Add(m_RenderTriangles[i].m_B);
            triangles.Add(m_RenderTriangles[i].m_C);
        }
        triangles_array = triangles.ToArray();

    }

    // Loads topology data from two files - data part from which crust data has then to be initialized, and render part that should normally be displayed
    public void LoadDefaultTopology(string data_filename, string render_filename)
    {
        TopoMeshInterpreter.ReadMesh(out m_DataVertices, out m_DataTriangles, out m_DataVerticesNeighbours, out m_DataTrianglesOfVertices, data_filename); // Read the data part
        m_VerticesCount = m_DataVertices.Count; // set the data vertices count
        m_TrianglesCount = m_DataTriangles.Count; // set the data triangles count
        List<BoundingVolume> m_BVTLeaves = new List<BoundingVolume>();
        for (int i = 0; i < m_TrianglesCount; i++) // for all triangles in data
        {
            m_DataTriangles[i].EnsureClockwiseOrientation(); // switch two points if the triangle is not clockwise
            BoundingVolume new_bb = new BoundingVolume(m_DataTriangles[i].m_CCenter, m_DataTriangles[i].m_CUnitRadius); // create a leaf bounding box
            new_bb.m_TriangleIndex = i; // denote the triangle index to the leaf
            m_DataTriangles[i].m_BVolume = new_bb; // denote the leaf to the respective triangle
            m_BVTLeaves.Add(new_bb); // add the new bounding volume to the list of leaves
        }
        m_DataBVH = ConstructBVH(m_BVTLeaves); // construct BVH from bottom
        m_DataBVHArray = BoundingVolume.BuildBVHArray(m_DataBVH); //
        m_DataPointData.Clear(); // delete the list of crust point data - data
        for (int i = 0; i < m_VerticesCount; i++) // for all vertices
        {
            m_DataPointData.Add(new PointData()); // add new point data
        }

        TopoMeshInterpreter.ReadMesh(out m_RenderVertices, out m_RenderTriangles, out m_RenderVerticesNeighbours, out m_RenderTrianglesOfVertices, render_filename); // Read the render part
        m_RenderVerticesCount = m_RenderVertices.Count; // set the render vertices count
        m_RenderTrianglesCount = m_RenderTriangles.Count; // set the render triangles count
        foreach (DRTriangle it in m_RenderTriangles) // for all triangles in render
        {
            it.EnsureClockwiseOrientation(); // switch two points if the triangle is not clockwise
        }
        m_RenderPointData.Clear();
        for (int i = 0; i < m_RenderVertices.Count; i++)
        {
            m_RenderPointData.Add(new PointData()); // delete the list of crust point data - render
        }

    }

    public void GenerateFractalTerrain ()
    {

        ComputeShader work_shader = m_PlanetManager.m_Shaders.m_FractalTerrainCShader;

        int kernelHandle = work_shader.FindKernel("CSFractalTerrain");

        Vector3[] vertices_input = m_DataVertices.ToArray();
        Vector3[] random_input = new Vector3[m_PlanetManager.m_Settings.FractalTerrainIterations];
        float[] elevations_output = new float[m_DataVertices.Count];




        for (int i = 0; i < m_PlanetManager.m_Settings.FractalTerrainIterations; i++)
        {
            Vector3 cand_input;
            do
            {
                cand_input = new Vector3(m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f));
            } while (cand_input.magnitude == 0);
            random_input[i] = cand_input.normalized;
        }

        ComputeBuffer vertices_input_buffer = new ComputeBuffer(vertices_input.Length, 12, ComputeBufferType.Default);
        ComputeBuffer random_input_buffer = new ComputeBuffer(random_input.Length, 12, ComputeBufferType.Default);
        ComputeBuffer elevations_output_buffer = new ComputeBuffer(elevations_output.Length, 4, ComputeBufferType.Default);
        vertices_input_buffer.SetData(vertices_input);
        random_input_buffer.SetData(random_input);
        elevations_output_buffer.SetData(elevations_output);

        work_shader.SetBuffer(kernelHandle, "vertices_input", vertices_input_buffer);
        work_shader.SetBuffer(kernelHandle, "random_input", random_input_buffer);
        work_shader.SetBuffer(kernelHandle, "elevations_output", elevations_output_buffer);
        work_shader.SetInt("fractal_iterations", m_PlanetManager.m_Settings.FractalTerrainIterations);
        work_shader.SetInt("vertices_number", m_VerticesCount);
        work_shader.SetFloat("elevation_step", m_PlanetManager.m_Settings.FractalTerrainElevationStep);
        work_shader.Dispatch(kernelHandle, m_DataVertices.Count / 64 + (m_DataVertices.Count % 64 != 0 ? 1 : 0), 1, 1);

        vertices_input_buffer.Release();
        random_input_buffer.Release();
        elevations_output_buffer.GetData(elevations_output);

        for (int i = 0; i < m_VerticesCount; i++)
        {
            m_DataPointData[i].elevation = elevations_output[i];
        }
        elevations_output_buffer.Release();
        InitializeCBuffers();
    }

    // Create new crust as random centroid set Voronoi map
    public void InitializeRandomCrust()
    {
        List<Vector3> centroids = new List<Vector3>(); // vertices are assigned around these centroids
        List<Plate> plates = new List<Plate>(); // formal partition objects
        List<float> plate_elevations = new List<float>();
        for (int i = 0; i < m_PlanetManager.m_Settings.PlateInitNumberOfCentroids; i++) // for each centroid
        {
            Vector3 added_centroid = new Vector3(m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f)).normalized;
            centroids.Add(added_centroid); // set the centroid vector
            Plate new_plate = new Plate(this); // create a new plate
            new_plate.m_RotationAxis = new Vector3(m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f)).normalized; // randomized rotation axis
            new_plate.m_PlateAngularSpeed = m_Random.Range(0.0f, m_PlanetManager.m_Settings.MaximumPlateSpeed); // angular speed of the plate
            new_plate.m_Centroid = added_centroid;
            plates.Add(new_plate); // add new plate to the list
            plate_elevations.Add(m_Random.Range(0.0f, 1.0f) < m_PlanetManager.m_Settings.InitialContinentalProbability ? m_PlanetManager.m_Settings.InitialContinentalAltitude : m_PlanetManager.m_Settings.InitialOceanicDepth);
        }
        m_CrustVertices = new List<Vector3>();
        m_CrustPointData = new List<PointData>();
        for (int i = 0; i < m_DataVertices.Count; i++) // for all vertices on the global mesh
        {
            float mindist = Mathf.Infinity;
            int plate_index = 0;
            for (int j = 0; j < centroids.Count; j++)
            {
                float dist = UnitSphereDistance(m_DataVertices[i], centroids[j]);
                if (dist < mindist)
                {
                    mindist = dist;
                    plate_index = j;
                }
            }
            m_DataPointData[i].thickness = m_PlanetManager.m_Settings.NewCrustThickness;
            m_DataPointData[i].plate = plate_index;
            m_DataPointData[i].orogeny = OroType.UNKNOWN;
            m_DataPointData[i].age = 0;
        }

        for (int i = 0; i < m_PlanetManager.m_Settings.VoronoiBorderNoiseIterations; i++)
        {
            WarpCrustBordersGlobal();
        }

        for (int i = 0; i < m_DataVertices.Count; i++) // for all vertices on the global mesh
        {
            m_DataPointData[i].elevation = plate_elevations[m_DataPointData[i].plate];
            plates[m_DataPointData[i].plate].m_PlateVertices.Add(i);
            m_CrustVertices.Add(m_DataVertices[i]);
            m_CrustPointData.Add(new PointData(m_DataPointData[i]));
        }
        m_CrustTriangles = new List<DRTriangle>();
        for (int i = 0; i < m_DataTriangles.Count; i++) // for all triangles
        {
            if ((m_DataPointData[m_DataTriangles[i].m_A].plate == m_DataPointData[m_DataTriangles[i].m_B].plate) && (m_DataPointData[m_DataTriangles[i].m_B].plate == m_DataPointData[m_DataTriangles[i].m_C].plate)) // if the triangle only has vertices of one type (qquivalence is a transitive relation)
            {
                plates[m_DataPointData[m_DataTriangles[i].m_A].plate].m_PlateTriangles.Add(i);
            }
            m_CrustTriangles.Add(new DRTriangle(m_DataTriangles[i], m_CrustVertices));
        }
        foreach (Plate it in plates)
        {
            List<BoundingVolume> bvt_leaves = new List<BoundingVolume>();
            int plate_tricount = it.m_PlateTriangles.Count;
            for (int i = 0; i < plate_tricount; i++) // for all triangles in data
            {
                int tri_index = it.m_PlateTriangles[i];
                BoundingVolume new_bb = new BoundingVolume(m_CrustTriangles[tri_index].m_CCenter, m_CrustTriangles[tri_index].m_CUnitRadius); // create a leaf bounding box
                new_bb.m_TriangleIndex = tri_index; // denote the triangle index to the leaf
                m_CrustTriangles[tri_index].m_BVolume = new_bb; // denote the leaf to the respective triangle
                bvt_leaves.Add(new_bb); // add the new bounding volume to the list of leaves
            }
            if (bvt_leaves.Count != 0)
            {
                it.m_BVHPlate = ConstructBVH(bvt_leaves);
                it.m_BVHArray = BoundingVolume.BuildBVHArray(it.m_BVHPlate);
            } else
            {
                Debug.Log("bad plate: " + it.m_PlateVertices.Count + " vertices");
                Debug.Log("bad plate: " + it.m_PlateTriangles.Count + " triangles");
            }
        }

        m_TectonicPlates = plates;
        m_TectonicPlatesCount = plates.Count;

        m_PlatesOverlap = CalculatePlatesVP();
        DetermineBorderTriangles();

        m_CBufferUpdatesNeeded["crust_vertex_locations"] = true;
        m_CBufferUpdatesNeeded["crust_triangles"] = true;
        m_CBufferUpdatesNeeded["crust_vertex_data"] = true;
        m_CBufferUpdatesNeeded["plate_transforms"] = true;
        m_CBufferUpdatesNeeded["plate_transforms_predictive"] = true;
        m_CBufferUpdatesNeeded["overlap_matrix"] = true;
        m_CBufferUpdatesNeeded["crust_BVH"] = true;
        m_CBufferUpdatesNeeded["crust_BVH_sps"] = true;
        m_CBufferUpdatesNeeded["crust_border_triangles"] = true;
        m_CBufferUpdatesNeeded["crust_border_triangles_sps"] = true;
        m_CBufferUpdatesNeeded["plate_motion_axes"] = true;
        m_CBufferUpdatesNeeded["plate_motion_angular_speeds"] = true;
        m_TotalTectonicStepsTaken = 0;
        m_TectonicStepsTakenWithoutResample = 0;
    }

    public void DetermineBorderTriangles ()
    {
        bool is_border;
        foreach (Plate it in m_TectonicPlates)
        {
            int tri_count = it.m_PlateTriangles.Count;
            for (int i = 0; i < tri_count; i++)
            {
                is_border = false;
                int pi_a = m_CrustPointData[m_CrustTriangles[it.m_PlateTriangles[i]].m_A].plate;
                int pi_b = m_CrustPointData[m_CrustTriangles[it.m_PlateTriangles[i]].m_B].plate;
                int pi_c = m_CrustPointData[m_CrustTriangles[it.m_PlateTriangles[i]].m_C].plate;
                if ((pi_a == pi_b) && (pi_b == pi_c))
                {
                    foreach (int it2 in m_CrustTriangles[it.m_PlateTriangles[i]].m_Neighbours)
                    {
                        pi_a = m_CrustPointData[m_CrustTriangles[it2].m_A].plate;
                        pi_b = m_CrustPointData[m_CrustTriangles[it2].m_B].plate;
                        pi_c = m_CrustPointData[m_CrustTriangles[it2].m_C].plate;
                        if ((pi_a != pi_b) || (pi_b != pi_c))
                        {
                            is_border = true;
                        }

                    }
                } 
                if (is_border)
                {
                    it.m_BorderTriangles.Add(it.m_PlateTriangles[i]);
                }
            }
        }
    }

    public int[,] CalculatePlatesVP ()
    {
        int[,] retVal = new int[m_TectonicPlatesCount, m_TectonicPlatesCount];
        float[] plate_scores = new float[m_TectonicPlatesCount];
        int[] plate_ranks = new int[m_TectonicPlatesCount];
        List<int> ranked = new List<int>();
        for (int i = 0; i < m_TectonicPlatesCount; i++)
        {
            foreach (int it in m_TectonicPlates[i].m_PlateVertices)
            {
                plate_scores[i] += (m_CrustPointData[it].elevation < 0.0f ? -m_CrustPointData[it].elevation : 100 * m_CrustPointData[it].elevation);
            }
        }
        for (int i = 0; i < m_TectonicPlatesCount; i++)
        {
            float max_score = 0.0f;
            int best_in_round = -1;
            for (int j = 0; j < m_TectonicPlatesCount; j++)
            {
                if (!ranked.Contains(j))
                {
                    if (plate_scores[j] > max_score)
                    {
                        max_score = plate_scores[j];
                        best_in_round = j;
                    }
                }
            }
            if (best_in_round == -1)
            {
                Debug.LogError("Plate density sort failure!");
            }
            plate_ranks[best_in_round] = i;
            ranked.Add(best_in_round);
        }

        for (int i = 0; i < m_TectonicPlatesCount; i++)
        {
            for (int j = 0; j <= i; j++)
            {
                if (i == j)
                {
                    retVal[i, j] = 0;
                }
                else
                {
                    retVal[i, j] = (plate_ranks[i] > plate_ranks[j] ? 1 : -1);
                }
            }
        }


        for (int j = 0; j < m_TectonicPlatesCount; j++)
        {
            for (int i = 0; i < j; i++)
            {
                retVal[i, j] = -retVal[j, i];
            }
        }
        return retVal;

        /* old version
        int[,] retVal = new int[m_TectonicPlatesCount, m_TectonicPlatesCount];
        for (int i = 0; i < m_TectonicPlatesCount; i++)
        {
            for (int j = 0; j <= i; j++)
            {
                if (i == j)
                {
                    retVal[i, j] = 0;
                } else
                {
                    if (m_TectonicPlates[i].m_Type >= 0)
                    {
                        if (m_TectonicPlates[j].m_Type < 0)
                        {
                            retVal[i, j] = 1;
                        }
                        else
                        {
                            retVal[i, j] = ((float)m_TectonicPlates[i].m_Mass/(float)m_TectonicPlates[i].m_PlateVertices.Count >= (float)m_TectonicPlates[j].m_Mass/(float)m_TectonicPlates[j].m_PlateVertices.Count ? -1 : 1);
                        }
                    }
                    else
                    {
                        if (m_TectonicPlates[j].m_Type >= 0)
                        {
                            retVal[i, j] = -1;
                        }
                        else
                        {
                            retVal[i, j] = ((float)m_TectonicPlates[i].m_Mass / (float)m_TectonicPlates[i].m_PlateVertices.Count >= (float)m_TectonicPlates[j].m_Mass / (float)m_TectonicPlates[j].m_PlateVertices.Count ? -1 : 1);
                        }
                    }
                }
            }
        }
        for (int j = 0; j < m_TectonicPlatesCount; j++)
        {
            for (int i = 0; i < j; i++)
            {
                retVal[i, j] = -retVal[j, i];
            }
        }
        return retVal;
        */
    }

    public void MovePlates ()
    {
        for (int i = 0; i < m_TectonicPlatesCount; i++)
        {
            m_TectonicPlates[i].m_Transform = Quaternion.AngleAxis(m_PlanetManager.m_Settings.TectonicIterationStepTime * m_TectonicPlates[i].m_PlateAngularSpeed * 180.0f / Mathf.PI, m_TectonicPlates[i].m_RotationAxis) * m_TectonicPlates[i].m_Transform;
        }
        m_CBufferUpdatesNeeded["plate_transforms"] = true;
        m_CBufferUpdatesNeeded["plate_transforms_predictive"] = true;
    }

    public BoundingVolume ConstructBVH(List<BoundingVolume> volume_list)
    {
        List<int> initial_order_indices = BoundingVolume.MCodeRadixSort(volume_list);

        List<BoundingVolume> bvlist_in = new List<BoundingVolume>();
        List<BoundingVolume> bvlist_out = new List<BoundingVolume>();
        int list_size = volume_list.Count;

        for (int i = 0; i < list_size; i++)
        {
            bvlist_in.Add(volume_list[initial_order_indices[i]]);
        }
        while (list_size > 1)
        {


            int[] nearest_neighbours = new int[list_size];

            int kernelHandle = m_PlanetManager.m_Shaders.m_BVHNearestNeighbourShader.FindKernel("CSBVHNN");

            Vector3[] cluster_positions = new Vector3[list_size];
            for (int i = 0; i < list_size; i++)
            {
                cluster_positions[i] = bvlist_in[i].m_Circumcenter;
            }

            ComputeBuffer cluster_positions_buffer = new ComputeBuffer(list_size, 12, ComputeBufferType.Default);
            ComputeBuffer nearest_neighbours_buffer = new ComputeBuffer(list_size, 4, ComputeBufferType.Default);

            cluster_positions_buffer.SetData(cluster_positions);

            m_PlanetManager.m_Shaders.m_BVHNearestNeighbourShader.SetBuffer(kernelHandle, "cluster_positions", cluster_positions_buffer);
            m_PlanetManager.m_Shaders.m_BVHNearestNeighbourShader.SetBuffer(kernelHandle, "nearest_neighbours", nearest_neighbours_buffer);

            m_PlanetManager.m_Shaders.m_BVHNearestNeighbourShader.SetInt("array_size", list_size);
            m_PlanetManager.m_Shaders.m_BVHNearestNeighbourShader.SetInt("BVH_radius", m_PlanetManager.m_Settings.BVHConstructionRadius);
            m_PlanetManager.m_Shaders.m_BVHNearestNeighbourShader.Dispatch(kernelHandle, (list_size/64) + 1, 1, 1);

            cluster_positions_buffer.Release();
            nearest_neighbours_buffer.GetData(nearest_neighbours);

            nearest_neighbours_buffer.Release();


            for (int i = 0; i < list_size; i++)
            {
                if ((nearest_neighbours[i] < 0) || (nearest_neighbours[i] >= list_size))
                {
                    nearest_neighbours[i] = (i == 0 ? 1 : 0);
                }

            }

            int merges = 0;
            int left = 0;
            int non_correspondent = 0;
            for (int i = 0; i < list_size; i++)
            {
                if (nearest_neighbours[i] < 0)
                {
                    Debug.Log(i + " -> " + nearest_neighbours[i]);
                }
                if (nearest_neighbours[nearest_neighbours[i]] == i)
                {
                    if (i < nearest_neighbours[i])
                    {
                        bvlist_out.Add(BoundingVolume.MergeBV(bvlist_in[i], bvlist_in[nearest_neighbours[i]]));
                        merges++;
                    }
                    else
                    {
                        left++;
                    }
                }
                else
                {
                    bvlist_out.Add(bvlist_in[i]);
                    non_correspondent++;
                }
            }
            bvlist_in = bvlist_out;
            list_size = bvlist_in.Count();
            bvlist_out = new List<BoundingVolume>();
        }
        return bvlist_in[0];
    }

    public void CleanUpPlates()
    {
        bool overlap_matrix_recalculation_need = false;
        int n_iterations = m_TectonicPlatesCount;
        for (int i = n_iterations - 1; i >= 0; i--)
        {
            if (m_TectonicPlates[i].m_PlateVertices.Count < 1)
            {
                //Debug.Log("Cleaning plate " + i + "...");
                overlap_matrix_recalculation_need = true;
                //Debug.Log("Checking clean triangle sets...");
                if (m_TectonicPlates[i].m_PlateTriangles.Count > 0)
                {
                    Debug.Log("Error: empty plate with non-empty triangle set!");
                }
                if (m_TectonicPlates[i].m_BorderTriangles.Count > 0)
                {
                    Debug.Log("Error: empty plate with non-empty border triangle set!");
                }
                //Debug.Log("Correcting vertex plate indices...");
                for (int j = 0; j < m_CrustVertices.Count; j++)
                {
                    if (m_CrustPointData[j].plate >= i) { 
                        if (m_CrustPointData[j].plate == i)
                        {
                            Debug.Log("Error: crust vertex registered to an empty plate!");
                        }
                        m_CrustPointData[j].plate--;
                    }
                }
                //Debug.Log("Removing plate " + i + "...");
                m_TectonicPlates.RemoveAt(i);
                m_TectonicPlatesCount--;
                //Debug.Log("Plate " + i + " removed");
            }
        }
        if (overlap_matrix_recalculation_need)
        {
            //Debug.Log("Recalculating overlap matrix...");
            m_PlatesOverlap = CalculatePlatesVP();
            m_CBufferUpdatesNeeded["crust_vertex_data"] = true;
        }
        /*
        m_CBufferUpdatesNeeded["plate_transforms"] = true;
        m_CBufferUpdatesNeeded["crust_BVH"] = true;
        m_CBufferUpdatesNeeded["crust_BVH_sps"] = true;
        m_CBufferUpdatesNeeded["crust_border_triangles"] = true;
        m_CBufferUpdatesNeeded["crust_border_triangles_sps"] = true;
        */
        //Debug.Log("Plate clean-up complete.");
    }

    public void ResampleCrust(bool clean_empty_plates = true)
    {
        Vector3[] centroids = new Vector3[m_TectonicPlatesCount];
        foreach (Plate it in m_TectonicPlates)
        {
            it.m_BorderTriangles.Clear();
            it.m_PlateTriangles.Clear();
            it.m_PlateVertices.Clear();
        }
        for (int i = 0; i < m_DataVertices.Count; i++) // for all vertices on the global mesh
        {
            m_CrustVertices[i] = m_DataVertices[i];
            m_TectonicPlates[m_DataPointData[i].plate].m_PlateVertices.Add(i);
            m_CrustPointData[i] = new PointData(m_DataPointData[i]);
            centroids[m_DataPointData[i].plate] += m_DataVertices[i];
        }
        for (int i = 0; i < m_TectonicPlatesCount; i++)
        {
            m_TectonicPlates[i].m_Centroid = (centroids[i].magnitude == 0.0f ? new Vector3(m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f)).normalized : centroids[i].normalized);
        }
        for (int i = 0; i < m_DataTriangles.Count; i++) // for all triangles
        {
            if ((m_DataPointData[m_DataTriangles[i].m_A].plate == m_DataPointData[m_DataTriangles[i].m_B].plate) && (m_DataPointData[m_DataTriangles[i].m_B].plate == m_DataPointData[m_DataTriangles[i].m_C].plate)) // if the triangle only has vertices of one type (qquivalence is a transitive relation)
            {
                m_TectonicPlates[m_DataPointData[m_DataTriangles[i].m_A].plate].m_PlateTriangles.Add(i);
            }
        }
        foreach (Plate it in m_TectonicPlates)
        {
            it.m_Transform = Quaternion.identity;
            List<BoundingVolume> bvt_leaves = new List<BoundingVolume>();
            int plate_tricount = it.m_PlateTriangles.Count;
            for (int i = 0; i < plate_tricount; i++) // for all triangles in data
            {
                int tri_index = it.m_PlateTriangles[i];
                BoundingVolume new_bb = new BoundingVolume(m_CrustTriangles[tri_index].m_CCenter, m_CrustTriangles[tri_index].m_CUnitRadius); // create a leaf bounding box
                new_bb.m_TriangleIndex = tri_index; // denote the triangle index to the leaf
                m_CrustTriangles[tri_index].m_BVolume = new_bb; // denote the leaf to the respective triangle
                bvt_leaves.Add(new_bb); // add the new bounding volume to the list of leaves
            }
            if (bvt_leaves.Count > 0)
            {
                it.m_BVHPlate = ConstructBVH(bvt_leaves);
                it.m_BVHArray = BoundingVolume.BuildBVHArray(it.m_BVHPlate);
            }
        }
        if (clean_empty_plates)
        {
            CleanUpPlates();
        }
        DetermineBorderTriangles();
        m_CBufferUpdatesNeeded["plate_transforms"] = true;
        m_CBufferUpdatesNeeded["crust_vertex_data"] = true;
        m_CBufferUpdatesNeeded["crust_BVH"] = true;
        m_CBufferUpdatesNeeded["crust_BVH_sps"] = true;
        m_CBufferUpdatesNeeded["crust_border_triangles"] = true;
        m_CBufferUpdatesNeeded["crust_border_triangles_sps"] = true;
        m_TectonicStepsTakenWithoutResample = 0;
    }

    public void TectonicStep()
    {
        ComputeShader work_shader = m_PlanetManager.m_Shaders.m_PlateInteractionsShader;
        if (m_PlanetManager.m_ContinentalCollisions)
        {
            UpdateCBBuffers();
            int continentalContactsKernelHandle = work_shader.FindKernel("CSContinentalContacts");

            int n_total_triangles = m_CBuffers["crust_triangles"].count;
            int[] continental_triangle_contacts_table_output = new int[m_TectonicPlatesCount * n_total_triangles];
            int[] continental_triangle_contacts_output = new int[n_total_triangles];

            ComputeBuffer continental_triangle_contacts_table_buffer = new ComputeBuffer(continental_triangle_contacts_table_output.Length, 4, ComputeBufferType.Default);
            ComputeBuffer continental_triangle_contacts_buffer = new ComputeBuffer(continental_triangle_contacts_output.Length, 4, ComputeBufferType.Default);

            continental_triangle_contacts_table_buffer.SetData(continental_triangle_contacts_table_output);
            continental_triangle_contacts_buffer.SetData(continental_triangle_contacts_output);

            work_shader.SetInt("n_crust_triangles", m_CrustTriangles.Count);
            work_shader.SetInt("n_plates", m_TectonicPlatesCount);
            work_shader.SetBuffer(continentalContactsKernelHandle, "crust_vertex_locations", m_CBuffers["crust_vertex_locations"]);
            work_shader.SetBuffer(continentalContactsKernelHandle, "crust_triangles", m_CBuffers["crust_triangles"]);
            work_shader.SetBuffer(continentalContactsKernelHandle, "crust_vertex_data", m_CBuffers["crust_vertex_data"]);
            work_shader.SetBuffer(continentalContactsKernelHandle, "overlap_matrix", m_CBuffers["overlap_matrix"]);
            work_shader.SetBuffer(continentalContactsKernelHandle, "crust_BVH", m_CBuffers["crust_BVH"]);
            work_shader.SetBuffer(continentalContactsKernelHandle, "crust_BVH_sps", m_CBuffers["crust_BVH_sps"]);
            work_shader.SetBuffer(continentalContactsKernelHandle, "plate_transforms", m_CBuffers["plate_transforms"]);
            work_shader.SetBuffer(continentalContactsKernelHandle, "plate_transforms_predictive", m_CBuffers["plate_transforms_predictive"]);

            work_shader.SetBuffer(continentalContactsKernelHandle, "continental_triangle_contacts_table", continental_triangle_contacts_table_buffer);
            work_shader.SetBuffer(continentalContactsKernelHandle, "continental_triangle_contacts", continental_triangle_contacts_buffer);

            work_shader.Dispatch(continentalContactsKernelHandle, n_total_triangles / 64 + (n_total_triangles % 64 != 0 ? 1 : 0), 1, 1);

            continental_triangle_contacts_table_buffer.GetData(continental_triangle_contacts_table_output);
            continental_triangle_contacts_buffer.GetData(continental_triangle_contacts_output);

            int[] continental_vertex_collisions = new int[m_VerticesCount];
            int[] continental_vertex_collisions_table = new int[m_VerticesCount * m_TectonicPlatesCount];
            bool collision_occured = false;
            int n_triangles = m_CrustTriangles.Count;
            for (int i = 0; i < n_triangles; i++)
            {
                if (continental_triangle_contacts_output[i] != 0)
                {
                    collision_occured = true;
                    continental_vertex_collisions[m_CrustTriangles[i].m_A] = 1;
                    for (int j = 0; j < m_TectonicPlatesCount; j++)
                    {
                        if (continental_triangle_contacts_table_output[j * n_triangles + i] != 0)
                        {
                            continental_vertex_collisions_table[j * m_VerticesCount + m_CrustTriangles[i].m_A] = 1;
                            continental_vertex_collisions_table[j * m_VerticesCount + m_CrustTriangles[i].m_B] = 1;
                            continental_vertex_collisions_table[j * m_VerticesCount + m_CrustTriangles[i].m_C] = 1;
                        }
                    }
                }
            }
            if (collision_occured)
            {
                Debug.Log("Continental collision detected :<");
                List<CollidingTerrane> c_terranes = new List<CollidingTerrane>();

                int[] continental_vertex_collisions_terranes = new int[m_CrustVertices.Count];
                int[] continental_vertex_collisions_plates = new int[m_CrustVertices.Count];

                for (int i = 0; i < m_CrustVertices.Count; i++)
                {
                    continental_vertex_collisions_plates[i] = -1;
                }

                int terrane_count_index = 0;

                for (int i = 0; i < m_CrustVertices.Count; i++)
                {
                    if (continental_vertex_collisions[i] != 0)
                    {
                        for (int j = 0; j < m_TectonicPlatesCount; j++)
                        {
                            if (continental_vertex_collisions_table[j * m_CrustVertices.Count + i] != 0)
                            {
                                terrane_count_index++;
                                int colliding_plate = m_CrustPointData[i].plate;
                                int collided_plate = j;
                                CollidingTerrane new_c_terrane = new CollidingTerrane();
                                Queue<int> to_search = new Queue<int>();
                                to_search.Enqueue(i);
                                continental_vertex_collisions_terranes[i] = terrane_count_index;
                                continental_vertex_collisions[i] = 0;
                                continental_vertex_collisions_plates[i] = collided_plate;
                                int active_vertex_index;
                                while (to_search.Count > 0)
                                {
                                    active_vertex_index = to_search.Dequeue();
                                    new_c_terrane.m_Vertices.Add(active_vertex_index);
                                    foreach (int it in m_DataVerticesNeighbours[active_vertex_index]) // Data should be initialized and filled
                                    {
                                        if ((continental_vertex_collisions_terranes[it] == 0) && (m_CrustPointData[it].elevation >= 0) && (m_CrustPointData[it].plate == colliding_plate))
                                        {
                                            to_search.Enqueue(it);
                                            continental_vertex_collisions_terranes[it] = terrane_count_index;
                                            continental_vertex_collisions[it] = 0;
                                            continental_vertex_collisions_plates[it] = collided_plate;
                                        }
                                    }
                                }
                                new_c_terrane.colliding_plate = colliding_plate;
                                new_c_terrane.collided_plate = collided_plate;
                                new_c_terrane.index = terrane_count_index;
                                c_terranes.Add(new_c_terrane);
                                break;
                            }
                        }
                    }
                }

                // IMPLEMENT ACTUAL COLLISION - START

                List<int> terrane_colliding_plates = new List<int>();
                List<int> terrane_collided_plates = new List<int>();
                List<int> terrane_vertices = new List<int>();
                List<int> terrane_vertices_sps = new List<int>();
                terrane_vertices_sps.Add(0);

                foreach (CollidingTerrane it in c_terranes)
                {
                    terrane_colliding_plates.Add(it.colliding_plate);
                    terrane_collided_plates.Add(it.collided_plate);
                    foreach (int it2 in it.m_Vertices)
                    {
                        terrane_vertices.Add(it2);
                    }
                    terrane_vertices_sps.Add(terrane_vertices.Count);
                }

                ComputeBuffer terrane_colliding_plates_buffer = new ComputeBuffer(terrane_colliding_plates.Count, 4);
                ComputeBuffer terrane_collided_plates_buffer = new ComputeBuffer(terrane_collided_plates.Count, 4);
                ComputeBuffer terrane_vertices_buffer = new ComputeBuffer(terrane_vertices.Count, 4);
                ComputeBuffer terrane_vertices_sps_buffer = new ComputeBuffer(terrane_vertices_sps.Count, 4);

                terrane_colliding_plates_buffer.SetData(terrane_colliding_plates.ToArray());
                terrane_collided_plates_buffer.SetData(terrane_collided_plates.ToArray());
                terrane_vertices_buffer.SetData(terrane_vertices.ToArray());
                terrane_vertices_sps_buffer.SetData(terrane_vertices_sps.ToArray());



                int continentalCollisionUpliftKernelHandle = work_shader.FindKernel("CSContinentalCollisionUplift");

                float[] uplift_output = new float[m_VerticesCount];
                ComputeBuffer uplift_buffer = new ComputeBuffer(m_VerticesCount, 4, ComputeBufferType.Default);
                uplift_buffer.SetData(uplift_output);

                work_shader.SetInt("n_crust_vertices", m_CrustVertices.Count);
                work_shader.SetInt("n_terranes", c_terranes.Count);
                work_shader.SetFloat("maximum_plate_speed", m_PlanetManager.m_Settings.MaximumPlateSpeed);
                work_shader.SetFloat("collision_coefficient", m_PlanetManager.m_Settings.ContinentalCollisionCoefficient);
                work_shader.SetFloat("global_collision_distance", m_PlanetManager.m_Settings.ContinentalCollisionGlobalDistance);
                work_shader.SetFloat("initial_average_vertex_area", (float)m_CrustVertices.Count/m_PlanetManager.m_Settings.PlateInitNumberOfCentroids);

                work_shader.SetBuffer(continentalCollisionUpliftKernelHandle, "crust_vertex_locations", m_CBuffers["crust_vertex_locations"]);
                work_shader.SetBuffer(continentalCollisionUpliftKernelHandle, "crust_vertex_data", m_CBuffers["crust_vertex_data"]);
                work_shader.SetBuffer(continentalCollisionUpliftKernelHandle, "plate_transforms", m_CBuffers["plate_transforms"]);
                work_shader.SetBuffer(continentalCollisionUpliftKernelHandle, "overlap_matrix", m_CBuffers["overlap_matrix"]);
                work_shader.SetBuffer(continentalCollisionUpliftKernelHandle, "plate_motion_axes", m_CBuffers["plate_motion_axes"]);
                work_shader.SetBuffer(continentalCollisionUpliftKernelHandle, "plate_motion_angular_speeds", m_CBuffers["plate_motion_angular_speeds"]);
                work_shader.SetBuffer(continentalCollisionUpliftKernelHandle, "terrane_colliding_plates", terrane_colliding_plates_buffer);
                work_shader.SetBuffer(continentalCollisionUpliftKernelHandle, "terrane_collided_plates", terrane_collided_plates_buffer);
                work_shader.SetBuffer(continentalCollisionUpliftKernelHandle, "terrane_vertices", terrane_vertices_buffer);
                work_shader.SetBuffer(continentalCollisionUpliftKernelHandle, "terrane_vertices_sps", terrane_vertices_sps_buffer);

                work_shader.SetBuffer(continentalCollisionUpliftKernelHandle, "uplift", uplift_buffer);

                work_shader.Dispatch(continentalCollisionUpliftKernelHandle, m_VerticesCount / 64 + (m_VerticesCount % 64 != 0 ? 1 : 0), 1, 1);


                uplift_buffer.GetData(uplift_output);
                //Debug.Log("Extrema of uplift - min: " + Mathf.Min(uplift_output) + "; max: " + Mathf.Max(uplift_output));
                float el_old, el_new;
                float el_max = Mathf.NegativeInfinity;
                float el_min = Mathf.Infinity;
                for (int i = 0; i < m_VerticesCount; i++)
                {
                    el_old = m_CrustPointData[i].elevation;
                    el_new = Mathf.Min(el_old + uplift_output[i], m_PlanetManager.m_Settings.HighestContinentalAltitude);
                    m_CrustPointData[i].elevation = el_new;
                    if ((el_old < 0) && (el_new >= 0))
                    {
                        m_CrustPointData[i].orogeny = OroType.HIMALAYAN;
                    }
                    el_max = (uplift_output[i] > el_max ? uplift_output[i] : el_max);
                    el_min = (uplift_output[i] < el_min ? uplift_output[i] : el_min);
                }

                terrane_colliding_plates_buffer.Release();
                terrane_collided_plates_buffer.Release();
                terrane_vertices_buffer.Release();
                terrane_vertices_sps_buffer.Release();
                uplift_buffer.Release();
                m_CBufferUpdatesNeeded["crust_vertex_data"] = true;





                // IMPLEMENT ACTUAL COLLISION - END

                CrustToData();
                ResampleCrust(false);
                foreach (CollidingTerrane it in c_terranes)
                {
                    foreach (int it2 in m_TectonicPlates[it.colliding_plate].m_PlateVertices)
                    {
                        m_DataPointData[it2].plate = it.collided_plate;
                    }
                    //Debug.Log("Terrain " + it.index + " in plate " + it.colliding_plate + ": " + it.m_Vertices.Count + " into plate " + it.collided_plate);
                    /*
                    for (int i = 0; i < it.m_Vertices.Count; i++)
                    {
                        m_DataPointData[it.m_Vertices[i]].plate = it.collided_plate;
                    }
                    */
                }
                ResampleCrust();

            }


            continental_triangle_contacts_table_buffer.Release();
            continental_triangle_contacts_buffer.Release();
        }
        if (m_PlanetManager.m_StepMovePlates)
        {
            MovePlates();
        }
        UpdateCBBuffers();

        int plateContactsKernelHandle = work_shader.FindKernel("CSTrianglePlateContacts");

        int n_total_border_triangles = m_CBuffers["crust_border_triangles"].count;
        CS_PlateContact[] contact_points_output = new CS_PlateContact[m_TectonicPlatesCount * n_total_border_triangles];

        ComputeBuffer contact_points_buffer = new ComputeBuffer(contact_points_output.Length, 28, ComputeBufferType.Default);
        work_shader.SetInt("n_crust_triangles", m_CrustTriangles.Count); //m_VerticesCount was here
        work_shader.SetInt("n_plates", m_TectonicPlatesCount);
        work_shader.SetInt("n_crust_border_triangles", n_total_border_triangles);
        work_shader.SetBuffer(plateContactsKernelHandle, "crust_vertex_locations", m_CBuffers["crust_vertex_locations"]);
        work_shader.SetBuffer(plateContactsKernelHandle, "crust_triangles", m_CBuffers["crust_triangles"]);
        work_shader.SetBuffer(plateContactsKernelHandle, "crust_vertex_data", m_CBuffers["crust_vertex_data"]);
        work_shader.SetBuffer(plateContactsKernelHandle, "overlap_matrix", m_CBuffers["overlap_matrix"]);
        work_shader.SetBuffer(plateContactsKernelHandle, "crust_BVH", m_CBuffers["crust_BVH"]);
        work_shader.SetBuffer(plateContactsKernelHandle, "crust_BVH_sps", m_CBuffers["crust_BVH_sps"]);
        work_shader.SetBuffer(plateContactsKernelHandle, "plate_transforms", m_CBuffers["plate_transforms"]);
        work_shader.SetBuffer(plateContactsKernelHandle, "crust_border_triangles", m_CBuffers["crust_border_triangles"]);
        work_shader.SetBuffer(plateContactsKernelHandle, "crust_border_triangles_sps", m_CBuffers["crust_border_triangles_sps"]);

        work_shader.SetBuffer(plateContactsKernelHandle, "contact_points", contact_points_buffer);

        work_shader.Dispatch(plateContactsKernelHandle, n_total_border_triangles / 64 + (n_total_border_triangles % 64 != 0 ? 1 : 0), 1, 1);

        contact_points_buffer.GetData(contact_points_output);

        List<Vector3> pointies = new List<Vector3>();
        for (int i = 0; i < n_total_border_triangles; i++)
        {
            for (int j = 0; j < m_TectonicPlatesCount; j++)
            {
                if (contact_points_output[j*n_total_border_triangles + i].contact_occured == 1)
                {
                    int plate = contact_points_output[j * n_total_border_triangles + i].contacting_plate;
                    pointies.Add(m_TectonicPlates[plate].m_Transform * contact_points_output[j * n_total_border_triangles + i].contact_point);
                    break;
                }
            }
        }

        /*
        Vector3[] vertex_locations_array = m_CrustVertices.ToArray();

        ComputeBuffer vertex_locations_buffer = new ComputeBuffer(m_VerticesCount, 12, ComputeBufferType.Default);

        vertex_locations_buffer.SetData(vertex_locations_array);
        */
        if (m_PlanetManager.m_StepSubductionUplift)
        {
            
            //contact_points_buffer.GetData(contact_points_output);

            int subductionKernelHandle = work_shader.FindKernel("CSSubductionUplift");
            
            work_shader.SetInt("n_crust_vertices", m_VerticesCount);
            work_shader.SetInt("n_crust_border_triangles", n_total_border_triangles);
            work_shader.SetFloat("subduction_control_distance", m_PlanetManager.m_Settings.SubductionDistanceTransferControlDistance);
            work_shader.SetFloat("subduction_max_distance", m_PlanetManager.m_Settings.SubductionDistanceTransferMaxDistance);
            

            float[] uplift_output = new float[m_VerticesCount];
            ComputeBuffer uplift_buffer = new ComputeBuffer(m_VerticesCount, 4, ComputeBufferType.Default);

            work_shader.SetFloat("subduction_uplift", m_PlanetManager.m_Settings.SubductionUplift);
            work_shader.SetFloat("oceanic_trench_elevation", m_PlanetManager.m_Settings.OceanicTrenchElevation);
            work_shader.SetFloat("highest_continental_altitude", m_PlanetManager.m_Settings.HighestContinentalAltitude);
            work_shader.SetFloat("maximum_plate_speed", m_PlanetManager.m_Settings.MaximumPlateSpeed);

            work_shader.SetBuffer(subductionKernelHandle, "crust_vertex_locations", m_CBuffers["crust_vertex_locations"]);
            work_shader.SetBuffer(subductionKernelHandle, "crust_vertex_data", m_CBuffers["crust_vertex_data"]);
            work_shader.SetBuffer(subductionKernelHandle, "plate_transforms", m_CBuffers["plate_transforms"]);
            work_shader.SetBuffer(subductionKernelHandle, "contact_points", contact_points_buffer);
            work_shader.SetBuffer(subductionKernelHandle, "overlap_matrix", m_CBuffers["overlap_matrix"]);
            work_shader.SetBuffer(subductionKernelHandle, "plate_motion_axes", m_CBuffers["plate_motion_axes"]);
            work_shader.SetBuffer(subductionKernelHandle, "plate_motion_angular_speeds", m_CBuffers["plate_motion_angular_speeds"]);

            work_shader.SetBuffer(subductionKernelHandle, "uplift", uplift_buffer);

            work_shader.Dispatch(subductionKernelHandle, m_VerticesCount / 64 + (m_VerticesCount % 64 != 0 ? 1 : 0), 1, 1);


            uplift_buffer.GetData(uplift_output);
            //Debug.Log("Extrema of uplift - min: " + Mathf.Min(uplift_output) + "; max: " + Mathf.Max(uplift_output));
            float el_old, el_new;
            for (int i = 0; i < m_VerticesCount; i++)
            {
                el_old = m_CrustPointData[i].elevation;
                el_new = Mathf.Min(el_old + uplift_output[i] * m_PlanetManager.m_Settings.TectonicIterationStepTime, m_PlanetManager.m_Settings.HighestContinentalAltitude);
                m_CrustPointData[i].elevation = el_new;
                if ((el_old < 0) && (el_new >= 0))
                {
                    m_CrustPointData[i].orogeny = OroType.ANDEAN;
                }
            }
            uplift_buffer.Release();
            m_CBufferUpdatesNeeded["crust_vertex_data"] = true;
        }
        if (m_PlanetManager.m_StepErosionDamping)
        {
            UpdateCBBuffers();
            int erosionDampingSedimentKernelHandle = work_shader.FindKernel("CSErosionDampingSediments");
            work_shader.SetInt("n_crust_vertices", m_VerticesCount);
            work_shader.SetFloat("oceanic_trench_elevation", m_PlanetManager.m_Settings.OceanicTrenchElevation);
            work_shader.SetFloat("highest_continental_altitude", m_PlanetManager.m_Settings.HighestContinentalAltitude);
            work_shader.SetFloat("oceanic_elevation_damping", m_PlanetManager.m_Settings.OceanicElevationDamping);
            work_shader.SetFloat("continental_erosion", m_PlanetManager.m_Settings.ContinentalErosion);
            work_shader.SetFloat("sediment_accretion", m_PlanetManager.m_Settings.SedimentAccretion);
            work_shader.SetFloat("average_oceanic_depth", m_PlanetManager.m_Settings.AverageOceanicDepth);


            float[] erosion_damping_output = new float[m_VerticesCount];
            float[] sediment_output = new float[m_VerticesCount];

            ComputeBuffer erosion_damping_buffer = new ComputeBuffer(m_VerticesCount, 4, ComputeBufferType.Default);
            ComputeBuffer sediment_buffer = new ComputeBuffer(m_VerticesCount, 4, ComputeBufferType.Default);
            

            work_shader.SetBuffer(erosionDampingSedimentKernelHandle, "crust_vertex_data", m_CBuffers["crust_vertex_data"]);

            work_shader.SetBuffer(erosionDampingSedimentKernelHandle, "erosion_damping", erosion_damping_buffer);
            work_shader.SetBuffer(erosionDampingSedimentKernelHandle, "sediment", sediment_buffer);
            work_shader.Dispatch(erosionDampingSedimentKernelHandle, m_VerticesCount / 64 + (m_VerticesCount % 64 != 0 ? 1 : 0), 1, 1);

            erosion_damping_buffer.GetData(erosion_damping_output);
            sediment_buffer.GetData(sediment_output);
            for (int i = 0; i < m_VerticesCount; i++)
            {
                m_CrustPointData[i].elevation = Mathf.Min(m_CrustPointData[i].elevation + (erosion_damping_output[i] + (m_PlanetManager.m_SedimentAccretion ? sediment_output[i] : 0.0f)) * m_PlanetManager.m_Settings.TectonicIterationStepTime, m_PlanetManager.m_Settings.HighestContinentalAltitude);
            }
            erosion_damping_buffer.Release();
            sediment_buffer.Release();
            m_CBufferUpdatesNeeded["crust_vertex_data"] = true;
        }

        if (m_PlanetManager.m_StepSlabPull)
        {
            UpdateCBBuffers();
            int slabContributionsKernelHandle = work_shader.FindKernel("CSPlateVerticesSlabContributions");
            work_shader.SetInt("n_crust_vertices", m_VerticesCount);


            int[] pull_contributions_output = new int[m_VerticesCount];

            ComputeBuffer pull_contributions_buffer = new ComputeBuffer(m_VerticesCount, 4, ComputeBufferType.Default);

            pull_contributions_buffer.SetData(pull_contributions_output);

            work_shader.SetBuffer(slabContributionsKernelHandle, "crust_vertex_locations", m_CBuffers["crust_vertex_locations"]);
            work_shader.SetBuffer(slabContributionsKernelHandle, "crust_vertex_data", m_CBuffers["crust_vertex_data"]);
            work_shader.SetBuffer(slabContributionsKernelHandle, "crust_triangles", m_CBuffers["crust_triangles"]);
            work_shader.SetBuffer(slabContributionsKernelHandle, "plate_transforms", m_CBuffers["plate_transforms"]);
            work_shader.SetBuffer(slabContributionsKernelHandle, "overlap_matrix", m_CBuffers["overlap_matrix"]);
            work_shader.SetBuffer(slabContributionsKernelHandle, "crust_BVH", m_CBuffers["crust_BVH"]);
            work_shader.SetBuffer(slabContributionsKernelHandle, "crust_BVH_sps", m_CBuffers["crust_BVH_sps"]);


            work_shader.SetBuffer(slabContributionsKernelHandle, "pull_contributions", pull_contributions_buffer);
            work_shader.Dispatch(slabContributionsKernelHandle, m_VerticesCount / 64 + (m_VerticesCount % 64 != 0 ? 1 : 0), 1, 1);

            pull_contributions_buffer.GetData(pull_contributions_output);
            Vector3[] axis_corrections = new Vector3[m_TectonicPlatesCount];
            for (int i = 0; i < m_VerticesCount; i++)
            {
                if (pull_contributions_output[i] == 1)
                {
                    Vector3 correction = Vector3.Cross(m_TectonicPlates[m_CrustPointData[i].plate].m_Centroid, m_CrustVertices[i]);
                    if (correction.magnitude > 0)
                    {
                        axis_corrections[m_CrustPointData[i].plate] += correction.normalized;
                    }
                }
            }
            for (int i = 0; i < m_TectonicPlatesCount; i++)
            {
                Vector3 new_axis = m_TectonicPlates[i].m_RotationAxis + m_PlanetManager.m_Settings.SlabPullPerturbation * axis_corrections[i] * m_PlanetManager.m_Settings.TectonicIterationStepTime;
                m_TectonicPlates[i].m_RotationAxis = (new_axis.magnitude > 0 ? new_axis.normalized : new Vector3(m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f)).normalized);
            }
            pull_contributions_buffer.Release();
            m_CBufferUpdatesNeeded["plate_motion_axes"] = true;
        }

        if (m_PlanetManager.m_PlateRifting)
        {
            float initial_average_vertex_area = (float)m_CrustVertices.Count / m_PlanetManager.m_Settings.PlateInitNumberOfCentroids;
            float adjusted_rift_frequency;
            int plate_count = m_TectonicPlates.Count;
            int ocean_count, continental_count;
            float ratio_weight;
            bool rift_occured = false;

            /*
            for (int i = 0; i < plate_count; i++)
            {
                if (m_TectonicPlates[i].m_PlateVertices.Count < 2)
                {
                    continue;
                }
                ocean_count = 0;
                continental_count = 0;
                for (int j = 0; j < m_TectonicPlates[i].m_PlateVertices.Count; j++)
                {
                    if (m_CrustPointData[m_TectonicPlates[i].m_PlateVertices[j]].elevation < 0) {
                        ocean_count++;
                    } else
                    {
                        continental_count++;
                    }
                }
                ratio_weight = (float)continental_count / (continental_count + ocean_count) * 0.9f + 0.1f;
                adjusted_rift_frequency = m_PlanetManager.m_Settings.PlateRiftsPerTectonicIterationStep * ratio_weight * (m_TectonicPlates[i].m_PlateVertices.Count) / initial_average_vertex_area;
                if (m_Random.Random() < adjusted_rift_frequency * Mathf.Exp(-adjusted_rift_frequency))
                {
                    Debug.Log("Rift occured at plate " + i);
                    PlateRift(i);
                    rift_occured = true;
                }
            }
            */

            int max_vertices_plate = -1;
            int max_vertices_n = 0;
            for (int i = 0; i < plate_count; i++)
            {
                if (m_TectonicPlates[i].m_PlateVertices.Count > max_vertices_n)
                {
                    max_vertices_plate = i;
                    max_vertices_n = m_TectonicPlates[i].m_PlateVertices.Count;
                }
            }

            if (m_TectonicPlates[max_vertices_plate].m_PlateVertices.Count >= 2)
            {
                ocean_count = 0;
                continental_count = 0;
                for (int j = 0; j < m_TectonicPlates[max_vertices_plate].m_PlateVertices.Count; j++)
                {
                    if (m_CrustPointData[m_TectonicPlates[max_vertices_plate].m_PlateVertices[j]].elevation < 0)
                    {
                        ocean_count++;
                    }
                    else
                    {
                        continental_count++;
                    }
                }
                ratio_weight = (float)continental_count / (continental_count + ocean_count) * 0.9f + 0.1f;
                adjusted_rift_frequency = m_PlanetManager.m_Settings.PlateRiftsPerTectonicIterationStep * ratio_weight * (m_TectonicPlates[max_vertices_plate].m_PlateVertices.Count) / initial_average_vertex_area;
                if (m_Random.Random() < adjusted_rift_frequency * Mathf.Exp(-adjusted_rift_frequency))
                {
                    Debug.Log("Rift occured at plate " + max_vertices_plate);
                    PlateRift(max_vertices_plate);
                    rift_occured = true;
                }
            }

            if (rift_occured)
            {
                ResampleCrust();
                CalculatePlatesVP();
            }
            

        }

        for (int i = 0; i < m_CrustVertices.Count; i++)
        {
            m_CrustPointData[i].age += m_PlanetManager.m_Settings.TectonicIterationStepTime;
        }

        contact_points_buffer.Release();
        m_TotalTectonicStepsTaken++;
        m_TectonicStepsTakenWithoutResample++;
    }

    public void BVHDiagnostics ()
    {
        Debug.Log("---------Data BVH Diagnostics---------");
        int max_depth = 0;
        Stack<BoundingVolume> searchstack = new Stack<BoundingVolume>();
        searchstack.Push(m_DataBVH);
        BoundingVolume cand;
        max_depth = searchstack.Count;
        while (searchstack.Count > 0)
        {
            cand = searchstack.Peek();
            if (cand.m_Children.Count > 0)
            {
                searchstack.Push(cand.m_Children[0]);
            } else
            {
                cand = searchstack.Pop();
                if (cand == searchstack.Peek().m_Children[0])
                {
                    searchstack.Push(searchstack.Peek().m_Children[1]);
                } else
                {
                    do
                    {
                        cand = searchstack.Pop();
                    } while ((searchstack.Count > 0) && (cand != searchstack.Peek().m_Children[0]));
                    if (searchstack.Count > 0)
                    {
                        searchstack.Push(searchstack.Peek().m_Children[1]);
                    } else
                    {
                        break;
                    }
                }
            }
            max_depth = Mathf.Max(max_depth, searchstack.Count);
        }
        Debug.Log("BVH binary tree depth is " + max_depth);
        if (m_TectonicPlatesCount > 0)
        {
            Debug.Log("---------Crust BVH Diagnostics---------");

            for (int i = 0; i < m_TectonicPlatesCount; i++)
            {
                searchstack.Clear();
                searchstack.Push(m_TectonicPlates[i].m_BVHPlate);
                max_depth = searchstack.Count;
                while (searchstack.Count > 0)
                {
                    cand = searchstack.Peek();
                    if (cand.m_Children.Count > 0)
                    {
                        searchstack.Push(cand.m_Children[0]);
                    }
                    else
                    {
                        cand = searchstack.Pop();
                        if (cand == searchstack.Peek().m_Children[0])
                        {
                            searchstack.Push(searchstack.Peek().m_Children[1]);
                        }
                        else
                        {
                            do
                            {
                                cand = searchstack.Pop();
                            } while ((searchstack.Count > 0) && (cand != searchstack.Peek().m_Children[0]));
                            if (searchstack.Count > 0)
                            {
                                searchstack.Push(searchstack.Peek().m_Children[1]);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    max_depth = Mathf.Max(max_depth, searchstack.Count);
                }
                Debug.Log("BVH binary tree depth for plate " + i + " is " + max_depth);
            }
        }
    }

    public void ElevationValueDiagnostics()
    {
        float tolerance = 0.01f;
        Debug.Log("Checking mesh health...");
        bool healthy = true;
        if (m_TectonicPlatesCount > 0)
        {
            Debug.Log("Tectonic plates present, checking crust...");
            int n_vertices = m_CrustVertices.Count;
            for (int i = 0; i < n_vertices; i++)
            {
                if (Mathf.Abs(1 - m_CrustVertices[i].magnitude) > tolerance)
                {
                    Debug.LogError("Anomalous crust vertex magnitude: " + i + "(" + m_CrustVertices[i].magnitude + ")");
                    healthy = false;
                    continue;
                }
                if (float.IsInfinity(m_CrustPointData[i].elevation))
                {
                    Debug.LogError("Anomalous crust vertex elevation: " + i + "(" + m_CrustPointData[i].elevation + ")");
                    healthy = false;
                    continue;
                }
                if (float.IsNaN(m_CrustPointData[i].elevation))
                {
                    Debug.LogError("Anomalous crust vertex elevation: " + i + "(" + m_CrustPointData[i].elevation + ")");
                    healthy = false;
                }
            }
            Debug.Log((healthy ? "Crust is healthy" : "Crust is not healthy"));
        }
        for (int i = 0; i < m_DataVertices.Count; i++)
        {
            if (Mathf.Abs(1 - m_DataVertices[i].magnitude) > tolerance)
            {
                Debug.LogError("Anomalous data vertex magnitude: " + i + "(" + m_CrustVertices[i].magnitude + ")");
                healthy = false;
            }
            if (float.IsInfinity(m_DataPointData[i].elevation))
            {
                Debug.LogError("Anomalous data vertex elevation: " + i + "(" + m_DataPointData[i].elevation + ")");
                healthy = false;
                continue;
            }
            if (float.IsNaN(m_DataPointData[i].elevation))
            {
                Debug.LogError("Anomalous data vertex elevation: " + i + "(" + m_DataPointData[i].elevation + ")");
                healthy = false;
            }
        }
        Debug.Log((healthy ? "Data is healthy" : "Data is not healthy"));
        for (int i = 0; i < m_RenderVertices.Count; i++)
        {
            if (Mathf.Abs(1 - m_DataVertices[i].magnitude) > tolerance)
            {
                Debug.LogError("Anomalous render vertex magnitude: " + i + "(" + m_CrustVertices[i].magnitude + ")");
                healthy = false;
            }
            if (float.IsInfinity(m_RenderPointData[i].elevation))
            {
                Debug.LogError("Anomalous render vertex elevation: " + i + "(" + m_RenderPointData[i].elevation + ")");
                healthy = false;
                continue;
            }
            if (float.IsNaN(m_RenderPointData[i].elevation))
            {
                Debug.LogError("Anomalous render vertex elevation: " + i + "(" + m_RenderPointData[i].elevation + ")");
                healthy = false;
            }
        }
        Debug.Log((healthy ? "Render is healthy" : "Render is not healthy"));
    }

    public void SmoothElevation ()
    {
        if (m_PlanetManager.m_PropagateCrust)
        {
            CrustToData();
        }
        int n_vertices = m_DataVertices.Count;
        float [] el_values = new float[n_vertices];
        float nsw = m_PlanetManager.m_Settings.NeighbourSmoothWeight;
        for (int i = 0; i < n_vertices; i++)
        {
            el_values[i] += m_DataPointData[i].elevation;
            foreach (int it in m_DataVerticesNeighbours[i])
            {
                el_values[i] += m_DataPointData[it].elevation * nsw;
            }
            el_values[i] /= nsw * m_DataVerticesNeighbours[i].Count + 1;
        }
        for (int i = 0; i < n_vertices; i++)
        {
            m_DataPointData[i].elevation = el_values[i];
        }
        m_CBufferUpdatesNeeded["data_vertex_data"] = true;
        if (m_TectonicPlates.Count > 0)
        {
            ResampleCrust();
        }
    }

    public void LaplacianSmoothElevation()
    {
        if (m_PlanetManager.m_PropagateCrust)
        {
            CrustToData();
        }
        int n_vertices = m_DataVertices.Count;
        float[] el_values = new float[n_vertices];
        float nsw;
        for (int i = 0; i < n_vertices; i++)
        {
            nsw = 0;
            foreach (int it in m_DataVerticesNeighbours[i])
            {
                nsw += m_DataPointData[it].elevation - m_DataPointData[i].elevation;
            }
            nsw = Mathf.Abs(nsw)/((m_PlanetManager.m_Settings.HighestContinentalAltitude - m_PlanetManager.m_Settings.OceanicTrenchElevation) * m_DataVerticesNeighbours[i].Count);
            el_values[i] += m_DataPointData[i].elevation;
            foreach (int it in m_DataVerticesNeighbours[i])
            {
                el_values[i] += m_DataPointData[it].elevation * nsw;
            }
            el_values[i] /= nsw * m_DataVerticesNeighbours[i].Count + 1;
        }
        for (int i = 0; i < n_vertices; i++)
        {
            m_DataPointData[i].elevation = el_values[i];
        }
        m_CBufferUpdatesNeeded["data_vertex_data"] = true;
        if (m_TectonicPlates.Count > 0)
        {
            ResampleCrust();
        }
    }

    public void CalculateThickness()
    {
        for (int i = 0; i < m_CrustVertices.Count; i++)
        {
            m_CrustPointData[i].thickness = m_PlanetManager.m_Settings.NewCrustThickness + m_CrustPointData[i].elevation;
        }
        m_CBufferUpdatesNeeded["crust_vertex_data"] = true;
    }

    public void PlateRift(int rifted_plate)
    {
        if (m_TectonicPlates[rifted_plate].m_PlateVertices.Count < 2)
        {
            return;
        }
        CrustToData();
        List<Vector3> centroids = new List<Vector3>(); // vertices are assigned around these centroids
        int new_plate_index = m_TectonicPlates.Count;
        int centroid1_index = m_Random.IRandom(0, m_TectonicPlates[rifted_plate].m_PlateVertices.Count);
        int centroid2_index;
        do { 
            centroid2_index = m_Random.IRandom(0, m_TectonicPlates[rifted_plate].m_PlateVertices.Count);
        } while (centroid2_index == centroid1_index);
        Vector3 centroid1 = m_DataVertices[centroid1_index];
        Vector3 centroid2 = m_DataVertices[centroid2_index];
        Vector3 adjusted_centroid1 = Vector3.zero;
        Vector3 adjusted_centroid2 = Vector3.zero;
        float dist1, dist2;
        for (int i = 0; i < m_TectonicPlates[rifted_plate].m_PlateVertices.Count; i++)
        {
            dist1 = UnitSphereDistance(m_DataVertices[m_TectonicPlates[rifted_plate].m_PlateVertices[i]], centroid1);
            dist2 = UnitSphereDistance(m_DataVertices[m_TectonicPlates[rifted_plate].m_PlateVertices[i]], centroid2);
            if (dist2 < dist1)
            {
                m_DataPointData[m_TectonicPlates[rifted_plate].m_PlateVertices[i]].plate = new_plate_index;
            }
        }

        for (int i = 0; i < m_PlanetManager.m_Settings.VoronoiBorderNoiseIterations; i++)
        {
            WarpCrustBordersTwoPlates(rifted_plate, new_plate_index);
        }


        for (int i = 0; i < m_TectonicPlates[rifted_plate].m_PlateVertices.Count; i++)
        {
            if (m_DataPointData[m_TectonicPlates[rifted_plate].m_PlateVertices[i]].plate == rifted_plate) {
                adjusted_centroid1 += m_DataVertices[m_TectonicPlates[rifted_plate].m_PlateVertices[i]];
            } else
            {
                adjusted_centroid2 += m_DataVertices[m_TectonicPlates[rifted_plate].m_PlateVertices[i]];
            }
        }

        Plate new_plate = new Plate(this);

        adjusted_centroid1 = adjusted_centroid1.magnitude > 0 ? adjusted_centroid1.normalized : new Vector3(m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f)).normalized;
        adjusted_centroid2 = adjusted_centroid2.magnitude > 0 ? adjusted_centroid2.normalized : new Vector3(m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f)).normalized;
        m_TectonicPlates[rifted_plate].m_Centroid = adjusted_centroid1;
        new_plate.m_Centroid = adjusted_centroid2;

        m_TectonicPlates[rifted_plate].m_PlateAngularSpeed = m_Random.Range(0.0f, m_PlanetManager.m_Settings.MaximumPlateSpeed);
        new_plate.m_PlateAngularSpeed = m_Random.Range(0.0f, m_PlanetManager.m_Settings.MaximumPlateSpeed);

        //new_plate.m_PlateAngularSpeed = m_TectonicPlates[rifted_plate].m_PlateAngularSpeed;
        Vector3 new_axis1, new_axis2;
        new_axis1 = Vector3.Cross(centroid1, centroid2);
        new_axis1 = new_axis1.magnitude > 0 ? new_axis1.normalized : new Vector3(m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f)).normalized;
        new_axis2 = -new_axis1;
        m_TectonicPlates[rifted_plate].m_RotationAxis = new_axis1;
        new_plate.m_RotationAxis = new_axis2;
        m_TectonicPlates.Add(new_plate);
        m_TectonicPlatesCount++;
    }

    public void ForcedPlateRift()
    {
        int plate_count = m_TectonicPlates.Count;

        int max_vertices_plate = -1;
        int max_vertices_n = 0;
        for (int i = 0; i < plate_count; i++)
        {
            if (m_TectonicPlates[i].m_PlateVertices.Count > max_vertices_n)
            {
                max_vertices_plate = i;
                max_vertices_n = m_TectonicPlates[i].m_PlateVertices.Count;
            }
        }

        if (m_TectonicPlates[max_vertices_plate].m_PlateVertices.Count >= 2)
        {
            Debug.Log("Rifting plate " + max_vertices_plate);
            PlateRift(max_vertices_plate);
            ResampleCrust();
            CalculatePlatesVP();
            m_CBufferUpdatesNeeded["plate_motion_axes"] = true;
            m_CBufferUpdatesNeeded["plate_motion_angular_speeds"] = true;
            m_CBufferUpdatesNeeded["crust_vertex_data"] = true;
        }
        else {
            Debug.LogError("WTF plate, no rift because reasons.");
        }
    }

    public void CreateVectorNoise ()
    {
        Vector3 noise_vec;
        for (int i = 0; i < m_DataTriangles.Count; i++)
        {
            do
            {
                noise_vec = new Vector3(m_Random.Range(0.0f, 1.0f), m_Random.Range(0.0f, 1.0f), m_Random.Range(0.0f, 1.0f));
                noise_vec = noise_vec - (Vector3.Dot(noise_vec, m_DataTriangles[i].m_BCenter)) * m_DataTriangles[i].m_BCenter;
            } while (noise_vec.magnitude == 0.0f);
            noise_vec = noise_vec.normalized;
            m_VectorNoise.Add(noise_vec);
        }
        for (int i = 0; i < m_PlanetManager.m_Settings.VectorNoiseAveragingIterations; i++)
        {
            List<Vector3> work_noise = new List<Vector3>(m_VectorNoise);
            for (int j = 0; j < m_DataTriangles.Count; j++)
            {
                foreach (int it in m_DataTriangles[j].m_Neighbours)
                {
                    Vector3 contrib = m_VectorNoise[it];
                    work_noise[j] += contrib - (Vector3.Dot(contrib, m_DataTriangles[j].m_BCenter)) * m_DataTriangles[j].m_BCenter;

                }

            }
            m_VectorNoise = work_noise;

        }

    }

    public void WarpCrustBordersGlobal ()
    {
        int[] vertex_plates = new int[m_DataVertices.Count];
        for (int i = 0; i < m_DataVertices.Count; i++)
        {
            vertex_plates[i] = m_DataPointData[i].plate;
        }

        for (int i = 0; i < m_DataTriangles.Count; i++)
        {
            int p1, p2, p3;
            Vector3 v1, v2, v3;
            float maxdot = Mathf.NegativeInfinity;
            float mindot = Mathf.Infinity;
            int maxdotind = -1;
            int mindotind = -1;
            p1 = m_DataPointData[m_DataTriangles[i].m_A].plate;
            p2 = m_DataPointData[m_DataTriangles[i].m_B].plate;
            p3 = m_DataPointData[m_DataTriangles[i].m_C].plate;
            bool candidate = ((p1 == p2) && (p1 != p3)) || ((p2 == p3) && (p2 != p1)) || ((p3 == p1) && (p3 != p2));
            if (!candidate)
            {
                continue;
            } else
            {
                if (m_Random.Range(0.0f, 1.0f) < m_VectorNoise[i].magnitude)
                {
                    v1 = m_DataVertices[m_DataTriangles[i].m_A] - m_DataTriangles[i].m_BCenter;
                    v2 = m_DataVertices[m_DataTriangles[i].m_B] - m_DataTriangles[i].m_BCenter;
                    v3 = m_DataVertices[m_DataTriangles[i].m_C] - m_DataTriangles[i].m_BCenter;
                    if (Vector3.Dot(m_VectorNoise[i], v1) > maxdot)
                    {
                        maxdot = Vector3.Dot(m_VectorNoise[i], v1);
                        maxdotind = m_DataTriangles[i].m_A;
                    }
                    if (Vector3.Dot(m_VectorNoise[i], v1) < mindot)
                    {
                        mindot = Vector3.Dot(m_VectorNoise[i], v1);
                        mindotind = m_DataTriangles[i].m_A;
                    }
                    if (Vector3.Dot(m_VectorNoise[i], v2) > maxdot)
                    {
                        maxdot = Vector3.Dot(m_VectorNoise[i], v2);
                        maxdotind = m_DataTriangles[i].m_B;
                    }
                    if (Vector3.Dot(m_VectorNoise[i], v2) < mindot)
                    {
                        mindot = Vector3.Dot(m_VectorNoise[i], v2);
                        mindotind = m_DataTriangles[i].m_B;
                    }
                    if (Vector3.Dot(m_VectorNoise[i], v3) > maxdot)
                    {
                        maxdotind = m_DataTriangles[i].m_C;
                    }
                    if (Vector3.Dot(m_VectorNoise[i], v3) < mindot)
                    {
                        mindotind = m_DataTriangles[i].m_C;
                    }
                    vertex_plates[maxdotind] = m_DataPointData[mindotind].plate;
                }
            }
        }
        for (int i = 0; i < m_DataVertices.Count; i++)
        {
            m_DataPointData[i].plate = vertex_plates[i];
        }
    }

    public void WarpCrustBordersTwoPlates(int a, int b)
    {
        int[] vertex_plates = new int[m_DataVertices.Count];
        for (int i = 0; i < m_DataVertices.Count; i++)
        {
            vertex_plates[i] = m_DataPointData[i].plate;
        }

        HashSet<int> allowed = new HashSet<int> { a, b };
        HashSet<int> present = new HashSet<int>();
        for (int i = 0; i < m_DataTriangles.Count; i++)
        {
            int p1, p2, p3;
            Vector3 v1, v2, v3;
            float maxdot = Mathf.NegativeInfinity;
            float mindot = Mathf.Infinity;
            int maxdotind = -1;
            int mindotind = -1;
            p1 = m_DataPointData[m_DataTriangles[i].m_A].plate;
            p2 = m_DataPointData[m_DataTriangles[i].m_B].plate;
            p3 = m_DataPointData[m_DataTriangles[i].m_C].plate;
            allowed = new HashSet<int>{a, b};
            present.Clear();
            present.Add(p1);
            present.Add(p2);
            present.Add(p3);

            if (!present.IsSubsetOf(allowed))
            {
                continue;
            }

            bool candidate = ((p1 == p2) && (p1 != p3)) || ((p2 == p3) && (p2 != p1)) || ((p3 == p1) && (p3 != p2));
            if (!candidate)
            {
                continue;
            }
            else
            {
                if (m_Random.Range(0.0f, 1.0f) < m_VectorNoise[i].magnitude)
                {
                    v1 = m_DataVertices[m_DataTriangles[i].m_A] - m_DataTriangles[i].m_BCenter;
                    v2 = m_DataVertices[m_DataTriangles[i].m_B] - m_DataTriangles[i].m_BCenter;
                    v3 = m_DataVertices[m_DataTriangles[i].m_C] - m_DataTriangles[i].m_BCenter;
                    if (Vector3.Dot(m_VectorNoise[i], v1) > maxdot)
                    {
                        maxdot = Vector3.Dot(m_VectorNoise[i], v1);
                        maxdotind = m_DataTriangles[i].m_A;
                    }
                    if (Vector3.Dot(m_VectorNoise[i], v1) < mindot)
                    {
                        mindot = Vector3.Dot(m_VectorNoise[i], v1);
                        mindotind = m_DataTriangles[i].m_A;
                    }
                    if (Vector3.Dot(m_VectorNoise[i], v2) > maxdot)
                    {
                        maxdot = Vector3.Dot(m_VectorNoise[i], v2);
                        maxdotind = m_DataTriangles[i].m_B;
                    }
                    if (Vector3.Dot(m_VectorNoise[i], v2) < mindot)
                    {
                        mindot = Vector3.Dot(m_VectorNoise[i], v2);
                        mindotind = m_DataTriangles[i].m_B;
                    }
                    if (Vector3.Dot(m_VectorNoise[i], v3) > maxdot)
                    {
                        maxdotind = m_DataTriangles[i].m_C;
                    }
                    if (Vector3.Dot(m_VectorNoise[i], v3) < mindot)
                    {
                        mindotind = m_DataTriangles[i].m_C;
                    }
                    vertex_plates[maxdotind] = m_DataPointData[mindotind].plate;
                }
            }
        }
        for (int i = 0; i < m_DataVertices.Count; i++)
        {
            m_DataPointData[i].plate = vertex_plates[i];
        }
    }

}


