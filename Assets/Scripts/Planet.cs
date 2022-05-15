using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Main project object. Holds most of the project information. Sphere is considered unit unless actual dimensions are required.
/// </summary>
public class TectonicPlanet
{
    public PlanetManager m_PlanetManager; // holds settings, shaders and other supporting information

    public float m_Radius; // radius of the planet

    public RandomMersenne m_Random; // reference object to RNG

    public List<Vector3> m_CrustVertices; // vertex locations belonging to the crust layer
    public List<DRTriangle> m_CrustTriangles; // all triangles belonging to the crust layer
    public List<PointData> m_CrustPointData; // crust vertex data like elevation, age, orogeny and plate index the vertex belongs to

    public List<Vector3> m_DataVertices; // vertex locations belonging to the data layer
    public List<DRTriangle> m_DataTriangles; // triangles belonging to the data layer
    public List<List<int>> m_DataVerticesNeighbours; // triangulation neighbours of vertices - data layer
    public List<List<int>> m_DataTrianglesOfVertices; // triangles belonging to vertices - data layer
    public List<PointData> m_DataPointData; // data layer vertex information like m_CrustPointData
    public BoundingVolume m_DataBVH; // bounding volume hiearchy of the data layer
    public List<BoundingVolumeStruct> m_DataBVHArray; // BVH array for shader use

    public int m_VerticesCount; // number of vertices, maybe unreliable
    public int m_TrianglesCount; // number of triangles, maybe unreliable
    public int m_TectonicStepsTakenWithoutResample; // tectonic steps taken from the last resample
    public int m_TotalTectonicStepsTaken; // total tectonic steps taken for reference

    public List<Vector3> m_RenderVertices; // vertex locations belonging to the render layer
    public List<DRTriangle> m_RenderTriangles; // triangles belonging to the render layer
    public List<List<int>> m_RenderVerticesNeighbours; // triangulation neighbours of vertices - render layer
    public List<List<int>> m_RenderTrianglesOfVertices; // triangles belonging to vertices - render layer
    public List<PointData> m_RenderPointData; // vertex data on the render layer

    public List<Vector3> m_VectorNoise; // vector noise used for warping plate boundaries

    public int m_RenderVerticesCount; // number of render vertices
    public int m_RenderTrianglesCount; // number of render triangles

    public int m_TectonicPlatesCount; // number of tectonic plates
    public List<Plate> m_TectonicPlates; // tectonic plates collection

    public int[,] m_PlatesOverlap; // matrix saying if row overlaps column (1 if it does, -1 if it goes under)

    public Dictionary<string, ComputeBuffer> m_CBuffers; // persistent shader input buffers, update on demand to save code and time, referenced by name
    public Dictionary<string, bool> m_CBufferUpdatesNeeded; // switches to denote currently needed updates, referenced by name

    /// <summary>
    /// Initialization constructor. All data have to be filled by calling other functions.
    /// </summary>
    /// <param name="radius">radius of the planet in multiples of 1000 km</param>
    public TectonicPlanet(float radius)
    {
        m_PlanetManager = (PlanetManager)GameObject.Find("Planet").GetComponent(typeof(PlanetManager)); // scene look-up

        m_Radius = radius; // initialize planet radius

        m_Random = m_PlanetManager.m_Random; // 

        m_CrustVertices = new List<Vector3>(); // all lists start empty
        m_CrustTriangles = new List<DRTriangle>();
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
        m_CBuffers = new Dictionary<string, ComputeBuffer>(); // buffers are created  in buffer initialization
        m_CBufferUpdatesNeeded = new Dictionary<string, bool>();

        m_TectonicStepsTakenWithoutResample = 0;
        m_TotalTectonicStepsTaken = 0;

        InitializeCBuffers(); // initialize all shader input buffers
    }

    /// <summary>
    /// Release all existing buffers, initialize new set of names and assign null ComputerBuffer references to them with a flag to be updated.
    /// </summary>
    public void InitializeCBuffers()
    {
        m_CBufferUpdatesNeeded.Clear(); // completely erase all buffers
        foreach (KeyValuePair<string, ComputeBuffer> it in m_CBuffers)
        {
            if (it.Value != null)
            {
                it.Value.Release();
            }
        }
        m_CBuffers.Clear();
        List<string> reload_keys = new List<string>();
        reload_keys.Add("crust_vertex_locations"); // locations of crust vertices - crust layer deals with plate motion and topological changes
        reload_keys.Add("crust_triangles"); // crust triangles
        reload_keys.Add("crust_vertex_data"); // crust points information
        reload_keys.Add("plate_transforms"); // transforms used to compute actual point locations from their original positions
        reload_keys.Add("plate_transforms_predictive"); // transforms simulating future plate motion by one step used to test continental collisions
        reload_keys.Add("overlap_matrix"); // plate overlap matrix
        reload_keys.Add("crust_BVH"); // all plate bounding volume hiearchies, concatenated
        reload_keys.Add("crust_BVH_sps"); // indices referencing plate array boundaries in crust_BVH - index corresponding to plate order in m_TectonicPlates is starting offset, next one is upper limit, where the next BVH starts - last is total upper limit
        reload_keys.Add("crust_border_triangles"); // all plate border triangles, concatenated
        reload_keys.Add("crust_border_triangles_sps"); // indices referencing plate array boundaries in crust_border_triangles - index corresponding to plate order in m_TectonicPlates is starting offset, next one is upper limit, where the next BVH starts - last is total upper limit
        reload_keys.Add("data_vertex_locations"); // locations of data vertices - data layer hold high precision data
        reload_keys.Add("data_triangles"); // data triangles
        reload_keys.Add("data_vertex_data"); // data points information
        reload_keys.Add("data_BVH"); // data layer bounding volume hiearchy
        reload_keys.Add("render_vertex_locations"); // locations of render vertices
        reload_keys.Add("render_vertex_data"); // render point data - render layer generally has fewer vertices if high resolution object is not needed or there are too many data layer vertices for Unity to render the object whole
        reload_keys.Add("plate_motion_axes"); // axes around which the plates rotate
        reload_keys.Add("plate_motion_angular_speeds"); // angular speeds with which the plates rotate

        foreach (string it in reload_keys)
        {
            m_CBuffers[it] = null;
            m_CBufferUpdatesNeeded[it] = true;
        }
    }

    /// <summary>
    /// Buffer management function. Should be called every time before a shader kernel needing the buffers is dispatched.
    /// </summary>
    public void UpdateCBBuffers()
    {
        if (m_CrustVertices.Count > 0) { // crust buffers
            if (m_CBufferUpdatesNeeded["crust_vertex_locations"])
            {
                if (m_CBuffers["crust_vertex_locations"] != null) // release old buffer, if it exists
                {
                    m_CBuffers["crust_vertex_locations"].Release();
                }
                m_CBuffers["crust_vertex_locations"] = new ComputeBuffer(m_VerticesCount, 12, ComputeBufferType.Default); // initialize new buffer with precomputed size
                m_CBuffers["crust_vertex_locations"].SetData(m_CrustVertices.ToArray()); // set data
                m_CBufferUpdatesNeeded["crust_vertex_locations"] = false; // uncheck the update flag
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
                List<BoundingVolumeStruct> crust_BVH_list = new List<BoundingVolumeStruct>(); // internally indexed bounding volume array, see BoundingVolume.BuildBVHArray(...) for details
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


            if (m_CBufferUpdatesNeeded["crust_border_triangles"] || m_CBufferUpdatesNeeded["crust_border_triangles_sps"]) // if any buffer needs udpating, update both
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

    /// <summary>
    /// Distance on a unit sphere, corrected for rounding error at Acos (cases of Cos > 1).
    /// </summary>
    /// <param name="a">first point position</param>
    /// <param name="b">second point position</param>
    /// <returns></returns>
    public static float UnitSphereDistance(Vector3 a, Vector3 b)
    {
        float dot = Vector3.Dot(a, b);
        return Mathf.Acos(dot <= 1.0f ? dot : 1.0f);
    }

    /// <summary>
    /// Interpolate crust data from crust layer to data layer.
    /// </summary>
    public void CrustToData()
    {
        if (m_TectonicPlates.Count == 0) // impossible without initialized tectonics
        {
            return;
        }
        ComputeShader work_shader = m_PlanetManager.m_Shaders.m_VertexDataInterpolationShader; // standard shader assignment

        int kernelHandle = work_shader.FindKernel("CSCrustToData"); // kernel assignment

        UpdateCBBuffers(); // update shader buffers

        work_shader.SetBuffer(kernelHandle, "crust_vertex_locations", m_CBuffers["crust_vertex_locations"]); // for point look-up and interpolation
        work_shader.SetBuffer(kernelHandle, "crust_triangles", m_CBuffers["crust_triangles"]); // look-up and interpolation
        work_shader.SetBuffer(kernelHandle, "crust_vertex_data", m_CBuffers["crust_vertex_data"]); // interpolation
        work_shader.SetInt("n_plates", m_TectonicPlatesCount); // number of tectonics plate
        work_shader.SetInt("tectonic_steps_taken_without_resample", m_TectonicStepsTakenWithoutResample); // determine new crust age
        work_shader.SetFloat("tectonic_iteration_step_time", m_PlanetManager.m_Settings.TectonicIterationStepTime); // determine new crust age

        work_shader.SetBuffer(kernelHandle, "overlap_matrix", m_CBuffers["overlap_matrix"]); // determine the highest plate at a point
        work_shader.SetBuffer(kernelHandle, "crust_BVH_sps", m_CBuffers["crust_BVH_sps"]); // look-up delimiters
        work_shader.SetBuffer(kernelHandle, "crust_BVH", m_CBuffers["crust_BVH"]); // look-up
        work_shader.SetBuffer(kernelHandle, "plate_transforms", m_CBuffers["plate_transforms"]); // calculating actual positions on plates

        work_shader.SetBuffer(kernelHandle, "data_vertex_locations", m_CBuffers["data_vertex_locations"]); // shader looks up these
        work_shader.SetBuffer(kernelHandle, "data_vertex_data", m_CBuffers["data_vertex_data"]); // interpolation goal

        work_shader.SetFloat("highest_oceanic_ridge_elevation", m_PlanetManager.m_Settings.HighestOceanicRidgeElevation); // new crust parameters, ocean ridges
        work_shader.SetFloat("abyssal_plains_elevation", m_PlanetManager.m_Settings.AbyssalPlainsElevation); // new crust parameters
        work_shader.SetFloat("oceanic_ridge_elevation_falloff", m_PlanetManager.m_Settings.OceanicRidgeElevationFalloff); // new crust parameters (shape)

        work_shader.SetInt("n_data_vertices", m_VerticesCount); // thread batch cut off at the number of vertices

        work_shader.SetBuffer(kernelHandle, "crust_border_triangles", m_CBuffers["crust_border_triangles"]); // new crust distances
        work_shader.SetBuffer(kernelHandle, "crust_border_triangles_sps", m_CBuffers["crust_border_triangles_sps"]); // distance to plates calculation delimiters
        

        work_shader.Dispatch(kernelHandle, m_VerticesCount / 64 + (m_VerticesCount % 64 != 0 ? 1 : 0), 1, 1); // 64 threads batches called at once, cut off at n_data_vertices

        CS_VertexData[] data_out = new CS_VertexData[m_VerticesCount]; // to fill the data layer
        m_CBuffers["data_vertex_data"].GetData(data_out); // shader output readout
        for (int i = 0; i < m_VerticesCount; i++) // assign values from shader
        {
            m_DataPointData[i].elevation = Mathf.Min(data_out[i].elevation, m_PlanetManager.m_Settings.HighestContinentalAltitude);
            m_DataPointData[i].plate = data_out[i].plate;
            m_DataPointData[i].age = data_out[i].age;
            m_DataPointData[i].orogeny = (OroType)data_out[i].orogeny;
        }
        m_CBufferUpdatesNeeded["data_vertex_data"] = true; // update needed
    }

    /// <summary>
    /// Interpolate data from data layer to render layer.
    /// </summary>
    public void DataToRender(bool propagate_crust)
    {
        if (propagate_crust)
        {
            CrustToData();
        }
        ComputeShader work_shader = m_PlanetManager.m_Shaders.m_VertexDataInterpolationShader; // same shader as CrustToData()

        int kernelHandle = work_shader.FindKernel("CSDataToRender"); // different kernel

        UpdateCBBuffers();
        // no tectonic topological data needed
        work_shader.SetBuffer(kernelHandle, "data_vertex_locations", m_CBuffers["data_vertex_locations"]);
        work_shader.SetBuffer(kernelHandle, "data_triangles", m_CBuffers["data_triangles"]);
        work_shader.SetBuffer(kernelHandle, "data_vertex_data", m_CBuffers["data_vertex_data"]);

        work_shader.SetBuffer(kernelHandle, "data_BVH", m_CBuffers["data_BVH"]);

        work_shader.SetBuffer(kernelHandle, "render_vertex_locations", m_CBuffers["render_vertex_locations"]);
        work_shader.SetBuffer(kernelHandle, "render_vertex_data", m_CBuffers["render_vertex_data"]); // target is render layer

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

    /// <summary>
    /// Assign variables the renderer uses when displaying crust layer.
    /// </summary>
    /// <param name="vertices_array">array of vertex vectors with elevations</param>
    /// <param name="triangles_array">rendered triangle indices for faces</param>
    public void CrustMesh(out Vector3[] vertices_array, out int[] triangles_array)
    {
        vertices_array = new Vector3[m_VerticesCount]; // vertex array to be fed to the renderer
        float elevation; // elevation correction
        for (int i = 0; i < m_VerticesCount; i++)
        {
            elevation = m_CrustPointData[i].elevation; // crust layer data
            if ((m_PlanetManager.m_ClampToOceanLevel) && (elevation < 0)) // clamp to ocean cuts off negative elevations
            {
                elevation = 0;
            }
            vertices_array[i] = (m_Radius + elevation * m_PlanetManager.m_ElevationScaleFactor) * (m_TectonicPlates[m_CrustPointData[i].plate].m_Transform * m_CrustVertices[i]); // elevation is in absolute units, scaled for details
        }
        List<int> triangles = new List<int>(); // triangle indices to be fed to the renderer
        for (int i = 0; i < m_TectonicPlatesCount; i++) // 
        {
            for (int j = 0; j < m_TectonicPlates[i].m_PlateTriangles.Count; j++) // assign each triangle indices manually to ensure clockwise orientation
            {
                triangles.Add(m_CrustTriangles[m_TectonicPlates[i].m_PlateTriangles[j]].m_A);
                triangles.Add(m_CrustTriangles[m_TectonicPlates[i].m_PlateTriangles[j]].m_B);
                triangles.Add(m_CrustTriangles[m_TectonicPlates[i].m_PlateTriangles[j]].m_C);
            }
        }
        triangles_array = triangles.ToArray(); // list to array for convenience

    }

    /// <summary>
    /// Assign variables the renderer uses when displaying data layer.
    /// </summary>
    /// <param name="vertices_array">array of vertex vectors with elevations</param>
    /// <param name="triangles_array">rendered triangle indices for faces</param>
    public void DataMesh(out Vector3[] vertices_array, out int[] triangles_array, bool propagate_crust)
    {
        if (propagate_crust) // if set, interpolate crust into data layer first
        {
            CrustToData();
        }
        vertices_array = m_DataVertices.ToArray(); // similar to CrustMesh(...)
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

    /// <summary>
    /// Assign variables the renderer uses when displaying render layer.
    /// </summary>
    /// <param name="vertices_array">array of vertex vectors with elevations</param>
    /// <param name="triangles_array">rendered triangle indices for faces</param>
    public void NormalMesh(out Vector3[] vertices_array, out int[] triangles_array, bool propagate_data, bool propagate_crust)
    {
        if (propagate_data) // if set, interpolate data layer into render layer first
        {
            DataToRender(propagate_crust);
        }
        vertices_array = m_RenderVertices.ToArray(); // similar to CrustMesh(...) and DataMesh(...)
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

    /// <summary>
    /// Loads topology data from two files - data part from which crust data has then to be initialized, and render part that should normally be displayed.
    /// </summary>
    /// <param name="data_filename">name of the template file for data layer</param>
    /// <param name="render_filename">name of the template file for render layer</param>
    public void LoadDefaultTopology(string data_filename, string render_filename)
    {
        m_PlanetManager.m_FileManager.ReadMesh(out m_DataVertices, out m_DataTriangles, out m_DataVerticesNeighbours, out m_DataTrianglesOfVertices, data_filename); // Read the data layer triangulation
        m_VerticesCount = m_DataVertices.Count; // set the data vertices count
        m_TrianglesCount = m_DataTriangles.Count; // set the data triangles count
        List<BoundingVolume> m_BVTLeaves = new List<BoundingVolume>(); // base list for bounding volume hiearchy
        for (int i = 0; i < m_TrianglesCount; i++) // ensure clockwise orientation of all triangles for rendering and build a base for constructing bounding volume hiearchy
        {
            m_DataTriangles[i].EnsureClockwiseOrientation(); // switch two points if the triangle is not clockwise
            BoundingVolume new_bb = new BoundingVolume(m_DataTriangles[i].m_CCenter, m_DataTriangles[i].m_CUnitRadius); // create a leaf bounding volume
            new_bb.m_TriangleIndex = i; // denote the triangle index to the leaf
            m_DataTriangles[i].m_BVolume = new_bb; // denote the leaf to the respective triangle
            m_BVTLeaves.Add(new_bb); // add the new bounding volume to the list of leaves
        }
        m_DataBVH = ConstructBVH(m_BVTLeaves); // construct BVH from bottom
        m_DataBVHArray = BoundingVolume.BuildBVHArray(m_DataBVH); // build BVH array for shader use
        m_DataPointData.Clear(); // clear the data layer crust information, if existing
        for (int i = 0; i < m_VerticesCount; i++) // initialize new data points
        {
            m_DataPointData.Add(new PointData()); // add new point data
        }

        m_PlanetManager.m_FileManager.ReadMesh(out m_RenderVertices, out m_RenderTriangles, out m_RenderVerticesNeighbours, out m_RenderTrianglesOfVertices, render_filename); // Read the render layer triangulation
        m_RenderVerticesCount = m_RenderVertices.Count; // set the render vertices count
        m_RenderTrianglesCount = m_RenderTriangles.Count; // set the render triangles count
        foreach (DRTriangle it in m_RenderTriangles) // ensure clockwise orientation of all triangles
        {
            it.EnsureClockwiseOrientation(); // switch two points if the triangle is not clockwise
        }
        m_RenderPointData.Clear();
        for (int i = 0; i < m_RenderVertices.Count; i++)
        {
            m_RenderPointData.Add(new PointData()); // clear the render layer crust information, if existing
        }

    }

    /// <summary>
    /// Create typical fractal elevation map on the data layer. Does not use tectonic layer.
    /// Older code - does not use persistent buffers, but it is fast enough so that it does not matter now.
    /// </summary>
    public void GenerateFractalTerrain ()
    {

        ComputeShader work_shader = m_PlanetManager.m_Shaders.m_FractalTerrainShader; // standard shader assignment

        int kernelHandle = work_shader.FindKernel("CSFractalTerrain"); // standard shader kernel assignment

        Vector3[] vertices_input = m_DataVertices.ToArray(); // data layer vertex locations
        Vector3[] random_input = new Vector3[m_PlanetManager.m_Settings.FractalTerrainIterations]; // a set of random vectors normal to plane divisions the fractal algorithm uses
        float[] elevations_output = new float[m_DataVertices.Count]; // array for shader elevations output

        for (int i = 0; i < m_PlanetManager.m_Settings.FractalTerrainIterations; i++) // at first, calculate the random normal vectors
        {
            Vector3 cand_input;
            do
            {
                cand_input = new Vector3(m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f));
            } while (cand_input.magnitude == 0); // ensure the random vector has a non-zero length
            random_input[i] = cand_input.normalized;
        }

        ComputeBuffer vertices_input_buffer = new ComputeBuffer(vertices_input.Length, 12, ComputeBufferType.Default); // create the compute buffers
        ComputeBuffer random_input_buffer = new ComputeBuffer(random_input.Length, 12, ComputeBufferType.Default);
        ComputeBuffer elevations_output_buffer = new ComputeBuffer(elevations_output.Length, 4, ComputeBufferType.Default);
        vertices_input_buffer.SetData(vertices_input); // fill the compute buffers data
        random_input_buffer.SetData(random_input);
        elevations_output_buffer.SetData(elevations_output);

        work_shader.SetBuffer(kernelHandle, "vertices_input", vertices_input_buffer); // set the compute buffers
        work_shader.SetBuffer(kernelHandle, "random_input", random_input_buffer);
        work_shader.SetBuffer(kernelHandle, "elevations_output", elevations_output_buffer);
        work_shader.SetInt("fractal_iterations", m_PlanetManager.m_Settings.FractalTerrainIterations); // set constant values
        work_shader.SetInt("vertices_number", m_VerticesCount);
        work_shader.SetFloat("elevation_step", m_PlanetManager.m_Settings.FractalTerrainElevationStep);
        work_shader.Dispatch(kernelHandle, m_DataVertices.Count / 64 + (m_DataVertices.Count % 64 != 0 ? 1 : 0), 1, 1); // dispatch threads for all vertices

        vertices_input_buffer.Release(); // release the temporary buffers
        random_input_buffer.Release();
        elevations_output_buffer.GetData(elevations_output);

        for (int i = 0; i < m_VerticesCount; i++) // assign computed elevations
        {
            m_DataPointData[i].elevation = elevations_output[i];
        }
        elevations_output_buffer.Release(); // release the output
        InitializeCBuffers(); // initialize persistent buffers, currently for rendering
    }

    /// <summary>
    /// Create a random crust by partitioning data layer triangulation. Basic principle is creating Voronoi map.
    /// </summary>
    public void InitializeRandomCrust()
    {
        List<Vector3> centroids = new List<Vector3>(); // centroids of the Voronoi cells
        List<Plate> plates = new List<Plate>(); // initialize new plate list
        List<float> plate_elevations = new List<float>(); // initial elevations of plate vertices - set to either ocean or continental
        for (int i = 0; i < m_PlanetManager.m_Settings.PlateInitNumberOfCentroids; i++) // create random plate instances with Voronoi centroids
        {
            Vector3 added_centroid = new Vector3(m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f)).normalized; // centroids are generated randomly
            centroids.Add(added_centroid); // set the centroid vector
            Plate new_plate = new Plate(this); // create a new plate instance
            new_plate.m_RotationAxis = new Vector3(m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f)).normalized; // randomized rotation axis
            new_plate.m_PlateAngularSpeed = m_Random.Range(0.0f, m_PlanetManager.m_Settings.MaximumPlateSpeed); // angular speed of the plate
            new_plate.m_Centroid = added_centroid; // assign centroid to the new plate
            plates.Add(new_plate); // add new plate to the list
            plate_elevations.Add(m_Random.Range(0.0f, 1.0f) < m_PlanetManager.m_Settings.InitialContinentalProbability ? m_PlanetManager.m_Settings.InitialContinentalAltitude : m_PlanetManager.m_Settings.InitialOceanicDepth); // randomly assign continental or oceanic plates, according to settings probability
        }
        m_CrustVertices = new List<Vector3>(); // vertices to be filled to the crust layer
        m_CrustPointData = new List<PointData>(); // crust layer data to be initialized
        for (int i = 0; i < m_DataVertices.Count; i++) // find the nearest plate centroid for all vertices
        {
            float mindist = Mathf.Infinity;
            int plate_index = 0;
            for (int j = 0; j < centroids.Count; j++)
            {
                float dist = UnitSphereDistance(m_DataVertices[i], centroids[j]); // iterate over centroids and keep minimal distance and plate index
                if (dist < mindist)
                {
                    mindist = dist;
                    plate_index = j;
                }
            }
            m_DataPointData[i].thickness = m_PlanetManager.m_Settings.NewCrustThickness; // initialize crust data point
            m_DataPointData[i].plate = plate_index; // assign the nearest plate centroid 
            m_DataPointData[i].orogeny = OroType.UNKNOWN; // orogeny is not differentiated
            m_DataPointData[i].age = 0; // new crust
        }

        for (int i = 0; i < m_PlanetManager.m_Settings.VoronoiBorderNoiseIterations; i++) // warp the cell borders to get a more natural look
        {
            WarpCrustBordersGlobal();
        }

        for (int i = 0; i < m_DataVertices.Count; i++) // translate all data layer vertices into crust layer, add corresponding vertices to plates
        {
            m_DataPointData[i].elevation = plate_elevations[m_DataPointData[i].plate];
            plates[m_DataPointData[i].plate].m_PlateVertices.Add(i);
            m_CrustVertices.Add(m_DataVertices[i]);
            m_CrustPointData.Add(new PointData(m_DataPointData[i]));
        }
        m_CrustTriangles = new List<DRTriangle>();
        for (int i = 0; i < m_DataTriangles.Count; i++) // translate all data layer triangles into crust layer, add corresponding triangles to plates
        {
            if ((m_DataPointData[m_DataTriangles[i].m_A].plate == m_DataPointData[m_DataTriangles[i].m_B].plate) && (m_DataPointData[m_DataTriangles[i].m_B].plate == m_DataPointData[m_DataTriangles[i].m_C].plate)) // if the triangle only has vertices of one type (qquivalence is a transitive relation)
            {
                plates[m_DataPointData[m_DataTriangles[i].m_A].plate].m_PlateTriangles.Add(i);
            }
            m_CrustTriangles.Add(new DRTriangle(m_DataTriangles[i], m_CrustVertices));
        }
        foreach (Plate it in plates) // build bounding volume hiearchies for all plates
        {
            List<BoundingVolume> bvt_leaves = new List<BoundingVolume>();
            int plate_tricount = it.m_PlateTriangles.Count;
            for (int i = 0; i < plate_tricount; i++)
            {
                int tri_index = it.m_PlateTriangles[i];
                BoundingVolume new_bb = new BoundingVolume(m_CrustTriangles[tri_index].m_CCenter, m_CrustTriangles[tri_index].m_CUnitRadius); // create a leaf bounding volume
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

        m_TectonicPlates = plates; // assign ready plates to the planet
        m_TectonicPlatesCount = plates.Count; // assign plate count

        m_PlatesOverlap = CalculatePlatesVP(); // calculate plates overlap matrix
        DetermineBorderTriangles(); // find all border triangles of plates

        // set buffer that need to be updated
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

    /// <summary>
    /// Find all border triangles of plates.
    /// </summary>
    public void DetermineBorderTriangles ()
    {
        bool is_border; // border flag
        foreach (Plate it in m_TectonicPlates) // for all plates, consider their triangles not border unless proven otherwise
        {
            int tri_count = it.m_PlateTriangles.Count;
            for (int i = 0; i < tri_count; i++) // perform the check for all plate triangles
            {
                is_border = false; // triangle is border if it neighbours a triangle with different vertex plates
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
                    it.m_BorderTriangles.Add(it.m_PlateTriangles[i]); // finally, add triangle to border triangles
                }
            }
        }
    }

    /// <summary>
    /// Calculate matrix of rows overlapping columns (1 if they do, -1 if they go under).
    /// </summary>
    /// <returns>matrix represented as a 2D array</returns>
    public int[,] CalculatePlatesVP ()
    {
        int[,] retVal = new int[m_TectonicPlatesCount, m_TectonicPlatesCount]; // initialize the array
        float[] plate_scores = new float[m_TectonicPlatesCount]; // assign each plate a score according to vertex elevation sum adjusted for continental elevation cases
        int[] plate_ranks = new int[m_TectonicPlatesCount]; // plate rank ordering the plates from highest to lowest score
        List<int> ranked = new List<int>(); // list of plates already assigned a rank
        for (int i = 0; i < m_TectonicPlatesCount; i++) // calculate scores by summing elevation with weight -1 in case of ocean crust elevation and 100 in case of continental crust elevation (abs value)
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
                if (!ranked.Contains(j)) // check if the plate has not been ranked yet
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
            plate_ranks[best_in_round] = i; // assign rank
            ranked.Add(best_in_round); // assign as ranked
        }

        for (int i = 0; i < m_TectonicPlatesCount; i++) // fill the rank comparisons as overlaps into a triangle, same plates have always zero
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


        for (int j = 0; j < m_TectonicPlatesCount; j++) // fill the rest to form an anti-symmetric matrix
        {
            for (int i = 0; i < j; i++)
            {
                retVal[i, j] = -retVal[j, i];
            }
        }
        return retVal;
    }

    /// <summary>
    /// Increment plate transforms along.
    /// </summary>
    public void MovePlates ()
    {
        for (int i = 0; i < m_TectonicPlatesCount; i++) // rotate transforms of all plates
        {
            m_TectonicPlates[i].m_Transform = Quaternion.AngleAxis(m_PlanetManager.m_Settings.TectonicIterationStepTime * m_TectonicPlates[i].m_PlateAngularSpeed * 180.0f / Mathf.PI, m_TectonicPlates[i].m_RotationAxis) * m_TectonicPlates[i].m_Transform;
        }
        m_CBufferUpdatesNeeded["plate_transforms"] = true; // set the transform buffers to be updated
        m_CBufferUpdatesNeeded["plate_transforms_predictive"] = true;
    }

    /// <summary>
    /// Construct a bounding volume hiearchy binary tree.
    /// </summary>
    /// <param name="volume_list">a list of elementary bounding volumes</param>
    /// <returns>root of the BVH tree</returns>
    public BoundingVolume ConstructBVH(List<BoundingVolume> volume_list)
    {
        List<int> initial_order_indices = BoundingVolume.MCodeRadixSort(volume_list); // sort the volume list into equirectangular Z-order curve

        List<BoundingVolume> bvlist_in = new List<BoundingVolume>(); // processed level of BVs
        List<BoundingVolume> bvlist_out = new List<BoundingVolume>(); // next (higher) level of BVs
        int list_size = volume_list.Count;

        for (int i = 0; i < list_size; i++) // set the leaves as the first level, order by Morton code
        {
            bvlist_in.Add(volume_list[initial_order_indices[i]]);
        }
        while (list_size > 1) // as long as there are BVs to merge
        {


            int[] nearest_neighbours = new int[list_size]; // array of nearest neighbour indices for all BVs - not neccessarily mutual as they are only evaluated at an array subrange

            int kernelHandle = m_PlanetManager.m_Shaders.m_BVHNearestNeighbourShader.FindKernel("CSBVHNN"); // assign the NN searching shader kernel

            Vector3[] cluster_positions = new Vector3[list_size]; // fill the circumcenters for NN look-up
            for (int i = 0; i < list_size; i++)
            {
                cluster_positions[i] = bvlist_in[i].m_Circumcenter;
            }

            ComputeBuffer cluster_positions_buffer = new ComputeBuffer(list_size, 12, ComputeBufferType.Default); // create corresponding buffers
            ComputeBuffer nearest_neighbours_buffer = new ComputeBuffer(list_size, 4, ComputeBufferType.Default);

            cluster_positions_buffer.SetData(cluster_positions); // set the circumcenters

            m_PlanetManager.m_Shaders.m_BVHNearestNeighbourShader.SetBuffer(kernelHandle, "cluster_positions", cluster_positions_buffer); // set the buffers
            m_PlanetManager.m_Shaders.m_BVHNearestNeighbourShader.SetBuffer(kernelHandle, "nearest_neighbours", nearest_neighbours_buffer);

            m_PlanetManager.m_Shaders.m_BVHNearestNeighbourShader.SetInt("array_size", list_size); // set the constants
            m_PlanetManager.m_Shaders.m_BVHNearestNeighbourShader.SetInt("BVH_radius", m_PlanetManager.m_Settings.BVHConstructionRadius); // array subrange radius
            m_PlanetManager.m_Shaders.m_BVHNearestNeighbourShader.Dispatch(kernelHandle, (list_size/64) + 1, 1, 1); // dispatch in batches of 64 for the whole array

            cluster_positions_buffer.Release(); // release input buffer
            nearest_neighbours_buffer.GetData(nearest_neighbours); // copy the NN data

            nearest_neighbours_buffer.Release(); // release output buffer


            for (int i = 0; i < list_size; i++)
            {
                if ((nearest_neighbours[i] < 0) || (nearest_neighbours[i] >= list_size)) // array index out of bounds
                {
                    nearest_neighbours[i] = (i == 0 ? 1 : 0);
                }

            }

            for (int i = 0; i < list_size; i++)
            {
                if (nearest_neighbours[i] < 0) // report anomalous NN index
                {
                    Debug.Log(i + " -> " + nearest_neighbours[i]);
                }
                if (nearest_neighbours[nearest_neighbours[i]] == i)  // if the NN indices are a match within array
                {
                    if (i < nearest_neighbours[i]) // only merge once, at the lower index within array
                    {
                        bvlist_out.Add(BoundingVolume.MergeBV(bvlist_in[i], bvlist_in[nearest_neighbours[i]])); // merge the corresponding BVs
                    }
                }
                else
                {
                    bvlist_out.Add(bvlist_in[i]); // BVs with non-corresponding indices are sent to the higher level to look for NN again
                }
            }
            bvlist_in = bvlist_out; // make output the proccessed BVs for the next level
            list_size = bvlist_in.Count(); // recalculate the list size
            bvlist_out = new List<BoundingVolume>(); // initialize new output list
        }
        return bvlist_in[0]; // return the first (and only) left BV, which is the root
    }

    /// <summary>
    /// Delete plates that no longer have vertices.
    /// </summary>
    public void CleanUpPlates()
    {
        bool overlap_matrix_recalculation_need = false; // flag the need for matrix recalculation if a deletion occurs - default value is false
        int n_iterations = m_TectonicPlatesCount; // look through all plates
        for (int i = n_iterations - 1; i >= 0; i--) // count down from the highest plate index, it makes shifting easier
        {
            if (m_TectonicPlates[i].m_PlateVertices.Count < 1) // if a plate has no vertices
            {
                overlap_matrix_recalculation_need = true; // flag the recalculation
                if (m_TectonicPlates[i].m_BorderTriangles.Count > 0) // if the plate has some border triangles, vertices were removed incorrectly
                {
                    Debug.Log("Error: empty plate with non-empty border triangle set!");
                }
                for (int j = 0; j < m_CrustVertices.Count; j++) // reduce
                {
                    if (m_CrustPointData[j].plate >= i) { // decrease all plate indices above active index by 1
                        if (m_CrustPointData[j].plate == i) // if a vertex has the active plate index, vertices were removed incorrectly
                        {
                            Debug.Log("Error: crust vertex registered to an empty plate!");
                        }
                        m_CrustPointData[j].plate--;
                    }
                }
                m_TectonicPlates.RemoveAt(i); // remove the plate object itself
                m_TectonicPlatesCount--; // decrease the number of plates
            }
        }
        if (overlap_matrix_recalculation_need) // if a recalculation is needed, do it and flag an update of crust point data
        {
            m_PlatesOverlap = CalculatePlatesVP();
            m_CBufferUpdatesNeeded["crust_vertex_data"] = true;
        }
    }

    /// <summary>
    /// Mirror the data layer in crust - reverting transforms to identity.
    /// </summary>
    /// <param name="clean_empty_plates">flag the need to remove empty plates - counterproductive when e. g. simply attaching terranes during continental collision</param>
    public void ResampleCrust(bool clean_empty_plates = true)
    {
        Vector3[] centroids = new Vector3[m_TectonicPlatesCount]; // array for centroids recalculation
        foreach (Plate it in m_TectonicPlates) // clear all member lists of all plates
        {
            it.m_BorderTriangles.Clear();
            it.m_PlateTriangles.Clear();
            it.m_PlateVertices.Clear();
        }
        for (int i = 0; i < m_DataVertices.Count; i++) // simply copy the data points, as they are identical barring the transforms - assign points to their respective plates
        {
            m_CrustVertices[i] = m_DataVertices[i];
            m_TectonicPlates[m_DataPointData[i].plate].m_PlateVertices.Add(i);
            m_CrustPointData[i] = new PointData(m_DataPointData[i]);
            centroids[m_DataPointData[i].plate] += m_DataVertices[i]; // incremental centroid calculation
        }
        for (int i = 0; i < m_TectonicPlatesCount; i++) // normalize all centroids, if possible - in case of zero length, assign random unit vector
        {
            m_TectonicPlates[i].m_Centroid = (centroids[i].magnitude == 0.0f ? new Vector3(m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f)).normalized : centroids[i].normalized);
        }
        for (int i = 0; i < m_DataTriangles.Count; i++) // determine plate triangles by their vertices' plate data
        {
            if ((m_DataPointData[m_DataTriangles[i].m_A].plate == m_DataPointData[m_DataTriangles[i].m_B].plate) && (m_DataPointData[m_DataTriangles[i].m_B].plate == m_DataPointData[m_DataTriangles[i].m_C].plate)) // if the triangle only has vertices of one type (qquivalence is a transitive relation)
            {
                m_TectonicPlates[m_DataPointData[m_DataTriangles[i].m_A].plate].m_PlateTriangles.Add(i);
            }
        }
        foreach (Plate it in m_TectonicPlates) // for all plates revert transforms and reconstruct VBH
        {
            it.m_Transform = Quaternion.identity;
            List<BoundingVolume> bvt_leaves = new List<BoundingVolume>();
            int plate_tricount = it.m_PlateTriangles.Count;
            for (int i = 0; i < plate_tricount; i++)
            {
                int tri_index = it.m_PlateTriangles[i];
                BoundingVolume new_bb = new BoundingVolume(m_CrustTriangles[tri_index].m_CCenter, m_CrustTriangles[tri_index].m_CUnitRadius); // create a leaf bounding volume
                new_bb.m_TriangleIndex = tri_index; // denote the triangle index to the leaf
                m_CrustTriangles[tri_index].m_BVolume = new_bb; // denote the leaf to the respective triangle
                bvt_leaves.Add(new_bb); // add the new bounding volume to the list of leaves
            }
            if (bvt_leaves.Count > 0) // construct BVH only for non-empty sets
            {
                it.m_BVHPlate = ConstructBVH(bvt_leaves);
                it.m_BVHArray = BoundingVolume.BuildBVHArray(it.m_BVHPlate);
            }
        }
        if (clean_empty_plates) // clean up empty plates, if flagged
        {
            CleanUpPlates();
        }
        DetermineBorderTriangles(); // determine noew border triangles
        m_CBufferUpdatesNeeded["plate_transforms"] = true; // flag needed buffer updates
        m_CBufferUpdatesNeeded["crust_vertex_data"] = true;
        m_CBufferUpdatesNeeded["crust_BVH"] = true;
        m_CBufferUpdatesNeeded["crust_BVH_sps"] = true;
        m_CBufferUpdatesNeeded["crust_border_triangles"] = true;
        m_CBufferUpdatesNeeded["crust_border_triangles_sps"] = true;
        m_TectonicStepsTakenWithoutResample = 0; // revert the tectonic steps without resampling back to 0
    }

    /// <summary>
    /// Perform the tectonic simulation step. Driven by PlanetManager settings.
    /// </summary>
    public void TectonicStep()
    {
        ComputeShader work_shader = m_PlanetManager.m_Shaders.m_PlateInteractionsShader; // shader assignment
        if (m_PlanetManager.m_ContinentalCollisions) // if continental collisions are allowed
        {
            UpdateCBBuffers(); // update buffers
            int continentalContactsKernelHandle = work_shader.FindKernel("CSContinentalContacts"); // shader kernel assignment - look for contacts of continental triangles from different plates

            int n_total_triangles = m_CBuffers["crust_triangles"].count; // set up needed input and output
            int[] continental_triangle_contacts_table_output = new int[m_TectonicPlatesCount * n_total_triangles]; // table showing with which plates the triangles collide, if so - 1D array with matrix C-like pointer arithmetic
            int[] continental_triangle_contacts_output = new int[n_total_triangles]; // collision flags for all triangles

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

            work_shader.Dispatch(continentalContactsKernelHandle, n_total_triangles / 64 + (n_total_triangles % 64 != 0 ? 1 : 0), 1, 1); // batches of 64

            continental_triangle_contacts_table_buffer.GetData(continental_triangle_contacts_table_output);
            continental_triangle_contacts_buffer.GetData(continental_triangle_contacts_output);

            int[] continental_vertex_collisions = new int[m_VerticesCount]; // corresponding vertex collision data
            int[] continental_vertex_collisions_table = new int[m_VerticesCount * m_TectonicPlatesCount];
            bool collision_occured = false; // global collision flag
            int n_triangles = m_CrustTriangles.Count;
            for (int i = 0; i < n_triangles; i++)
            {
                if (continental_triangle_contacts_output[i] != 0) // if a triangle collided
                {
                    collision_occured = true;
                    continental_vertex_collisions[m_CrustTriangles[i].m_A] = 1; // flag the vertex collisions
                    continental_vertex_collisions[m_CrustTriangles[i].m_B] = 1;
                    continental_vertex_collisions[m_CrustTriangles[i].m_C] = 1;
                    for (int j = 0; j < m_TectonicPlatesCount; j++)
                    {
                        if (continental_triangle_contacts_table_output[j * n_triangles + i] != 0) // fill the plate specific collisions
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
                Debug.Log("Continental collision detected :<"); // log the collision
                List<CollidingTerrane> c_terranes = new List<CollidingTerrane>(); // set of colliding terranes

                int[] continental_vertex_collisions_terranes = new int[m_CrustVertices.Count]; // array of colliding terranes indices
                int[] continental_vertex_collisions_plates = new int[m_CrustVertices.Count]; // with which plate the vertex collide

                for (int i = 0; i < m_CrustVertices.Count; i++) // initialize at out of bounds indices -1
                {
                    continental_vertex_collisions_plates[i] = -1;
                }

                int terrane_count_index = 0; // number of indexing within arrays

                for (int i = 0; i < m_CrustVertices.Count; i++) // build colliding terranes
                {
                    if (continental_vertex_collisions[i] != 0) // if a vertex is flagged as colliding
                    {
                        for (int j = 0; j < m_TectonicPlatesCount; j++) //
                        {
                            if (continental_vertex_collisions_table[j * m_CrustVertices.Count + i] != 0) // if found flagged vertex, assume it has not been yet assigned to a colliding terrane and build one
                            {
                                terrane_count_index++; // new terrane id
                                int colliding_plate = m_CrustPointData[i].plate; // read the plate the terrane should belong to
                                int collided_plate = j; // to which plate should the terrane be attached
                                CollidingTerrane new_c_terrane = new CollidingTerrane();
                                Queue<int> to_search = new Queue<int>(); // terrane construction BFS queue
                                to_search.Enqueue(i); // first vertex
                                continental_vertex_collisions_terranes[i] = terrane_count_index; // assign the vertex to new terrane
                                continental_vertex_collisions[i] = 0; // deflag the vertex, will not be used again
                                continental_vertex_collisions_plates[i] = collided_plate; // to which plate the vertex should be attached
                                int active_vertex_index; // index of the neighbour-expanded vertex
                                while (to_search.Count > 0) // while there is something left to expand
                                {
                                    active_vertex_index = to_search.Dequeue(); // take first to expand
                                    new_c_terrane.m_Vertices.Add(active_vertex_index); // add vertex to terrane
                                    foreach (int it in m_DataVerticesNeighbours[active_vertex_index]) // for all neighbours of the expanded vertex
                                    {
                                        if ((continental_vertex_collisions_terranes[it] == 0) && (m_CrustPointData[it].elevation >= 0) && (m_CrustPointData[it].plate == colliding_plate)) // if it is continental, not yet assigned a terrane and of the same plate as the original vertex
                                        {
                                            to_search.Enqueue(it); // add for expansion
                                            continental_vertex_collisions_terranes[it] = terrane_count_index; // assign terrane
                                            continental_vertex_collisions[it] = 0; // deflag expanded vertex
                                            continental_vertex_collisions_plates[it] = collided_plate; // assign to collide with a plate
                                        }
                                    }
                                }
                                new_c_terrane.colliding_plate = colliding_plate; // fill the terrane data
                                new_c_terrane.collided_plate = collided_plate;
                                new_c_terrane.index = terrane_count_index;
                                c_terranes.Add(new_c_terrane); // add to terrane list
                                break; // looking only for first plate that fits, skipping the rest
                            }
                        }
                    }
                }

                // collision uplift computation
                List<int> terrane_colliding_plates = new List<int>(); // needed input data
                List<int> terrane_collided_plates = new List<int>();
                List<int> terrane_vertices = new List<int>(); // array of terrane vertices, delimited by the sps counterpart (see sps buffers for details)
                List<int> terrane_vertices_sps = new List<int>();
                terrane_vertices_sps.Add(0); // initialize first delimiter

                foreach (CollidingTerrane it in c_terranes) // construct the input
                {
                    terrane_colliding_plates.Add(it.colliding_plate);
                    terrane_collided_plates.Add(it.collided_plate);
                    foreach (int it2 in it.m_Vertices)
                    {
                        terrane_vertices.Add(it2);
                    }
                    terrane_vertices_sps.Add(terrane_vertices.Count);
                }

                ComputeBuffer terrane_colliding_plates_buffer = new ComputeBuffer(terrane_colliding_plates.Count, 4); // create corresponding buffers
                ComputeBuffer terrane_collided_plates_buffer = new ComputeBuffer(terrane_collided_plates.Count, 4);
                ComputeBuffer terrane_vertices_buffer = new ComputeBuffer(terrane_vertices.Count, 4);
                ComputeBuffer terrane_vertices_sps_buffer = new ComputeBuffer(terrane_vertices_sps.Count, 4);

                terrane_colliding_plates_buffer.SetData(terrane_colliding_plates.ToArray()); // set the data to buffers
                terrane_collided_plates_buffer.SetData(terrane_collided_plates.ToArray());
                terrane_vertices_buffer.SetData(terrane_vertices.ToArray());
                terrane_vertices_sps_buffer.SetData(terrane_vertices_sps.ToArray());

                int continentalCollisionUpliftKernelHandle = work_shader.FindKernel("CSContinentalCollisionUplift"); // assign the kernel

                float[] uplift_output = new float[m_VerticesCount]; // create the output uplift contributions
                ComputeBuffer uplift_buffer = new ComputeBuffer(m_VerticesCount, 4, ComputeBufferType.Default);
                uplift_buffer.SetData(uplift_output);

                work_shader.SetInt("n_crust_vertices", m_CrustVertices.Count); // set the kernel data
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

                work_shader.Dispatch(continentalCollisionUpliftKernelHandle, m_VerticesCount / 64 + (m_VerticesCount % 64 != 0 ? 1 : 0), 1, 1); // dispatch over all vertices at batches of 64


                uplift_buffer.GetData(uplift_output); // get the uplift data
                float el_old, el_new; // test height overflow
                for (int i = 0; i < m_VerticesCount; i++)
                {
                    el_old = m_CrustPointData[i].elevation;
                    el_new = Mathf.Min(el_old + uplift_output[i], m_PlanetManager.m_Settings.HighestContinentalAltitude); // clamp at max height
                    m_CrustPointData[i].elevation = el_new;
                    if ((el_old < 0) && (el_new >= 0)) // if the vertex rose from the ocean, set the Himalayan orogeny
                    {
                        m_CrustPointData[i].orogeny = OroType.HIMALAYAN;
                    }
                }

                terrane_colliding_plates_buffer.Release(); // release buffers
                terrane_collided_plates_buffer.Release();
                terrane_vertices_buffer.Release();
                terrane_vertices_sps_buffer.Release();
                uplift_buffer.Release();
                m_CBufferUpdatesNeeded["crust_vertex_data"] = true; // update elevation buffer

                CrustToData(); // interpolate to data
                ResampleCrust(false); // resample for easier vertex assignment
                foreach (CollidingTerrane it in c_terranes) // for all terranes, switch plate indices of their plates to their new plates
                {
                    foreach (int it2 in m_TectonicPlates[it.colliding_plate].m_PlateVertices)
                    {
                        m_DataPointData[it2].plate = it.collided_plate;
                    }
                }
                ResampleCrust(); // resample again, now with empty plate deletion

            }
            continental_triangle_contacts_table_buffer.Release(); // release buffers
            continental_triangle_contacts_buffer.Release();
        }
        if (m_PlanetManager.m_StepMovePlates) // increment transforms of all plates
        {
            MovePlates();
        }
        UpdateCBBuffers();

        int plateContactsKernelHandle = work_shader.FindKernel("CSTrianglePlateContacts"); // find border triangle contacts kernel

        int n_total_border_triangles = m_CBuffers["crust_border_triangles"].count;
        CS_PlateContact[] contact_points_output = new CS_PlateContact[m_TectonicPlatesCount * n_total_border_triangles]; // matrix of flags - border triangle collisions with other plates

        ComputeBuffer contact_points_buffer = new ComputeBuffer(contact_points_output.Length, 28, ComputeBufferType.Default);
        work_shader.SetInt("n_crust_triangles", m_CrustTriangles.Count);
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

        work_shader.Dispatch(plateContactsKernelHandle, n_total_border_triangles / 64 + (n_total_border_triangles % 64 != 0 ? 1 : 0), 1, 1); // dispatch over all border triangles

        contact_points_buffer.GetData(contact_points_output); // get output contact data

        if (m_PlanetManager.m_StepSubductionUplift) // if subduction uplift is allowed
        {
            
            int subductionKernelHandle = work_shader.FindKernel("CSSubductionUplift"); // calculate uplift contributions from subduction
            
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

            work_shader.Dispatch(subductionKernelHandle, m_VerticesCount / 64 + (m_VerticesCount % 64 != 0 ? 1 : 0), 1, 1); // dispatch over all vertices


            uplift_buffer.GetData(uplift_output);

            float el_old, el_new; // similar to uplift in continental collision
            for (int i = 0; i < m_VerticesCount; i++)
            {
                el_old = m_CrustPointData[i].elevation;
                el_new = Mathf.Min(el_old + uplift_output[i] * m_PlanetManager.m_Settings.TectonicIterationStepTime, m_PlanetManager.m_Settings.HighestContinentalAltitude);
                m_CrustPointData[i].elevation = el_new;
                if ((el_old < 0) && (el_new >= 0))
                {
                    m_CrustPointData[i].orogeny = OroType.ANDEAN; // vertices rising above ocean level because of subduction gain Andean orogeny flag
                }
            }
            uplift_buffer.Release();
            m_CBufferUpdatesNeeded["crust_vertex_data"] = true;
        }
        if (m_PlanetManager.m_StepErosionDamping) // if continental erosion and ocean damping are allowed
        {
            UpdateCBBuffers();
            int erosionDampingSedimentKernelHandle = work_shader.FindKernel("CSErosionDampingSediments"); // calculate uplift contributions (decrease) by erosion, damping and sediment accretion
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
            work_shader.Dispatch(erosionDampingSedimentKernelHandle, m_VerticesCount / 64 + (m_VerticesCount % 64 != 0 ? 1 : 0), 1, 1); // dispatch over all vertices

            erosion_damping_buffer.GetData(erosion_damping_output);
            sediment_buffer.GetData(sediment_output);
            for (int i = 0; i < m_VerticesCount; i++) // change elevations - add sediment accretion only if it is allowed
            {
                m_CrustPointData[i].elevation = Mathf.Min(m_CrustPointData[i].elevation + (erosion_damping_output[i] + (m_PlanetManager.m_SedimentAccretion ? sediment_output[i] : 0.0f)) * m_PlanetManager.m_Settings.TectonicIterationStepTime, m_PlanetManager.m_Settings.HighestContinentalAltitude);
            }
            erosion_damping_buffer.Release();
            sediment_buffer.Release();
            m_CBufferUpdatesNeeded["crust_vertex_data"] = true;
        }

        if (m_PlanetManager.m_StepSlabPull) // if subduction front slab pull is allowed
        {
            UpdateCBBuffers();
            int slabContributionsKernelHandle = work_shader.FindKernel("CSPlateVerticesSlabContributions"); // calculate rotation axes changes because of slab pull
            work_shader.SetInt("n_crust_vertices", m_VerticesCount);


            int[] pull_contributions_output = new int[m_VerticesCount]; // minor vertex contributions are summed up for individual plates

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
            work_shader.Dispatch(slabContributionsKernelHandle, m_VerticesCount / 64 + (m_VerticesCount % 64 != 0 ? 1 : 0), 1, 1); // dispatch over all vertices

            pull_contributions_buffer.GetData(pull_contributions_output);
            Vector3[] axis_corrections = new Vector3[m_TectonicPlatesCount]; // contributions are assigned to individual plates
            for (int i = 0; i < m_VerticesCount; i++)
            {
                if (pull_contributions_output[i] == 1)
                {
                    Vector3 correction = Vector3.Cross(m_TectonicPlates[m_CrustPointData[i].plate].m_Centroid, m_CrustVertices[i]); // contribution calculation
                    if (correction.magnitude > 0)
                    {
                        axis_corrections[m_CrustPointData[i].plate] += correction.normalized; // all contributions are weighed equally
                    }
                }
            }
            for (int i = 0; i < m_TectonicPlatesCount; i++) // axes changes are calculated as added perturbations weighed by a constant and then normalized
            {
                Vector3 new_axis = m_TectonicPlates[i].m_RotationAxis + m_PlanetManager.m_Settings.SlabPullPerturbation * axis_corrections[i] * m_PlanetManager.m_Settings.TectonicIterationStepTime;
                m_TectonicPlates[i].m_RotationAxis = (new_axis.magnitude > 0 ? new_axis.normalized : new Vector3(m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f)).normalized);
            }
            pull_contributions_buffer.Release();
            m_CBufferUpdatesNeeded["plate_motion_axes"] = true;
        }

        if (m_PlanetManager.m_PlateRifting) // if plate rifting is allowed
        {
            float initial_average_vertex_area = (float)m_CrustVertices.Count / m_PlanetManager.m_Settings.PlateInitNumberOfCentroids; // probability is derived from current area
            float adjusted_rift_frequency;
            int plate_count = m_TectonicPlates.Count;
            int ocean_count, continental_count;
            float ratio_weight;
            bool rift_occured = false;

            int max_vertices_plate = -1; // largest plate index
            int max_vertices_n = 0; // largest plate number of vertices
            for (int i = 0; i < plate_count; i++) // more vertices -> larger, i. e. look for a plate with the largest number of vertices
            {
                if (m_TectonicPlates[i].m_PlateVertices.Count > max_vertices_n) // compare and set as largest, if greater
                {
                    max_vertices_plate = i;
                    max_vertices_n = m_TectonicPlates[i].m_PlateVertices.Count;
                }
            }

            if (m_TectonicPlates[max_vertices_plate].m_PlateVertices.Count >= 2) // rifting a plate implies at lwast two vertices - consistency check
            {
                ocean_count = 0; // how many ocean vertices
                continental_count = 0; // how many continental vertices
                for (int j = 0; j < m_TectonicPlates[max_vertices_plate].m_PlateVertices.Count; j++) // count different vertices types
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
                ratio_weight = (float)continental_count / (continental_count + ocean_count) * 0.9f + 0.1f; // continental ratio weight function - linear between 0.1f and 1.0f
                adjusted_rift_frequency = m_PlanetManager.m_Settings.PlateRiftsPerTectonicIterationStep * ratio_weight * (m_TectonicPlates[max_vertices_plate].m_PlateVertices.Count) / initial_average_vertex_area; // calculate probability the plate rift spontaneously
                if (m_Random.Random() < adjusted_rift_frequency * Mathf.Exp(-adjusted_rift_frequency)) // Poisson distribution
                {
                    Debug.Log("Rift occured at plate " + max_vertices_plate); // log the rift
                    PlateRift(max_vertices_plate); // call the rift function
                    rift_occured = true;
                }
            }

            if (rift_occured) // resample after rift and recalculate overlap matrix
            {
                ResampleCrust();
                CalculatePlatesVP();
            }
        }

        for (int i = 0; i < m_CrustVertices.Count; i++) // age the crust
        {
            m_CrustPointData[i].age += m_PlanetManager.m_Settings.TectonicIterationStepTime;
        }

        contact_points_buffer.Release(); // release the buffer used for subduction
        m_TotalTectonicStepsTaken++; // increment step numbers
        m_TectonicStepsTakenWithoutResample++;
    }

    /// <summary>
    /// Check the bounding volume hiearchy for inconsistencies and log the tree depth (for pruning efficiency)
    /// </summary>
    public void BVHDiagnostics ()
    {
        Debug.Log("---------Data BVH Diagnostics---------");
        int max_depth = 0; // maximum depth reached
        Stack<BoundingVolume> searchstack = new Stack<BoundingVolume>(); // stack configuration - DFS
        searchstack.Push(m_DataBVH); // push the tree root
        BoundingVolume cand; // BV reference to be evaluated
        max_depth = searchstack.Count;
        while (searchstack.Count > 0) // while stack is not empty
        {
            cand = searchstack.Peek(); // look at the stack top
            if (cand.m_Children.Count > 0) // if the stack top has children, push the first (left) child
            {
                searchstack.Push(cand.m_Children[0]);
            } else // if it is a leaf, remove it and look for the next BV to expand
            {
                cand = searchstack.Pop(); // remove leaf from stack
                if (cand == searchstack.Peek().m_Children[0]) // if it is the left child of the current stack top, push its second (right) child - this ensures both children are looked at consecutively
                {
                    searchstack.Push(searchstack.Peek().m_Children[1]); // throws an exception if there is a BV with only a single child
                } else // if it is the right child, find next BV to expand by removing stack top until there is nothing left or it has a right child that has not been expanded yet
                {
                    do
                    {
                        cand = searchstack.Pop();
                    } while ((searchstack.Count > 0) && (cand != searchstack.Peek().m_Children[0])); // do it while stack is not empty and current BV is the left child of the next stack top
                    if (searchstack.Count > 0)
                    {
                        searchstack.Push(searchstack.Peek().m_Children[1]); // stack top has only right child that has not been expanded yet
                    }
                    else // if stack is empty, break and end -> tree has been searched
                    {
                        break;
                    }
                }
            }
            max_depth = Mathf.Max(max_depth, searchstack.Count); // while pushing children, compare the stack size to current maximum depth
        }
        Debug.Log("BVH binary tree depth is " + max_depth); // log maximum depth
        if (m_TectonicPlatesCount > 0) // if tectonics are present
        {
            Debug.Log("---------Crust BVH Diagnostics---------"); // repeat the same btu for each plate individually on crust layer BVH

            for (int i = 0; i < m_TectonicPlatesCount; i++)
            {
                searchstack.Clear(); // clear before proceeding
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

    /// <summary>
    /// Check mesh vertex lengths for non-unit lengths and elevation values for infinities.
    /// </summary>
    public void MeshAndElevationValueDiagnostics()
    {
        float magnitude_tolerance = 0.0001f; // maximum tolerance for vertex length
        Debug.Log("Checking mesh health...");
        bool healthy = true;
        if (m_TectonicPlatesCount > 0) // check crust vertices for anomalous Vector3 magnitudes and infinities
        {
            Debug.Log("Tectonic plates present, checking crust...");
            int n_vertices = m_CrustVertices.Count;
            for (int i = 0; i < n_vertices; i++)
            {
                if (Mathf.Abs(1 - m_CrustVertices[i].magnitude) > magnitude_tolerance)
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
        for (int i = 0; i < m_DataVertices.Count; i++)  // check data vertices for anomalous Vector3 magnitudes and infinities
        {
            if (Mathf.Abs(1 - m_DataVertices[i].magnitude) > magnitude_tolerance)
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
        for (int i = 0; i < m_RenderVertices.Count; i++) // check render vertices for anomalous Vector3 magnitudes and infinities
        {
            if (Mathf.Abs(1 - m_DataVertices[i].magnitude) > magnitude_tolerance)
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

    /// <summary>
    /// Smooth elevation values in the data layer.
    /// </summary>
    public void SmoothElevation ()
    {
        if (m_PlanetManager.m_PropagateCrust) // first interpolate the crust layer
        {
            CrustToData();
        }
        int n_vertices = m_DataVertices.Count;
        float [] el_values = new float[n_vertices];
        float nsw = m_PlanetManager.m_Settings.NeighbourSmoothWeight; // neighbour influence is weighed by this constant
        for (int i = 0; i < n_vertices; i++)
        {
            el_values[i] += m_DataPointData[i].elevation;
            foreach (int it in m_DataVerticesNeighbours[i]) // change current value by weighed value of all neighbours
            {
                el_values[i] += m_DataPointData[it].elevation * nsw;
            }
            el_values[i] /= nsw * m_DataVerticesNeighbours[i].Count + 1; // normalize the result
        }
        for (int i = 0; i < n_vertices; i++)
        {
            m_DataPointData[i].elevation = el_values[i]; // set the new values
        }
        m_CBufferUpdatesNeeded["data_vertex_data"] = true; // flag buffer updates
        if (m_TectonicPlates.Count > 0) // resample crust to reflect changes in crust layer
        {
            ResampleCrust();
        }
    }

    /// <summary>
    /// Smooth elevation values weighed by vertex elevation graph Laplacian
    /// </summary>
    public void LaplacianSmoothElevation()
    {
        if (m_PlanetManager.m_PropagateCrust) // similar to SmoothElevation()
        {
            CrustToData();
        }
        int n_vertices = m_DataVertices.Count;
        float[] el_values = new float[n_vertices];
        float nsw; // weight is computed
        for (int i = 0; i < n_vertices; i++)
        {
            nsw = 0;
            foreach (int it in m_DataVerticesNeighbours[i]) // compute vertex laplacian
            {
                nsw += m_DataPointData[it].elevation - m_DataPointData[i].elevation;
            }
            nsw = Mathf.Abs(nsw)/((m_PlanetManager.m_Settings.HighestContinentalAltitude - m_PlanetManager.m_Settings.OceanicTrenchElevation) * m_DataVerticesNeighbours[i].Count); // normalize the Laplacian for maximum elevation difference and neighbour count
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

    /// <summary>
    /// Assign crust thickness values.
    /// </summary>
    public void CalculateThickness()
    {
        for (int i = 0; i < m_CrustVertices.Count; i++) // thickness is simply a thickness of new crust values plus corresponding elevation values
        {
            m_CrustPointData[i].thickness = m_PlanetManager.m_Settings.NewCrustThickness + m_CrustPointData[i].elevation;
        }
        m_CBufferUpdatesNeeded["crust_vertex_data"] = true;
    }

    /// <summary>
    /// Divide given plate into two.
    /// </summary>
    /// <param name="rifted_plate">index of the rifted plate</param>
    public void PlateRift(int rifted_plate)
    {
        if (m_TectonicPlates[rifted_plate].m_PlateVertices.Count < 2) // only plates with at least two vertices should be rifted
        {
            return;
        }
        CrustToData(); // interpolate crust layer into data layer
        List<Vector3> centroids = new List<Vector3>(); // vertices are assigned around these new centroids
        int new_plate_index = m_TectonicPlates.Count; // one new plate has the old index, another (new) the first available index
        int centroid1_index = m_Random.IRandom(0, m_TectonicPlates[rifted_plate].m_PlateVertices.Count); // take random plate vertex index as the first centroid
        int centroid2_index;
        do { // until another plate vertex is picked as the second centroid, choose randomly
            centroid2_index = m_Random.IRandom(0, m_TectonicPlates[rifted_plate].m_PlateVertices.Count);
        } while (centroid2_index == centroid1_index);
        Vector3 centroid1 = m_DataVertices[centroid1_index]; // assign centroid positions
        Vector3 centroid2 = m_DataVertices[centroid2_index];
        Vector3 adjusted_centroid1 = Vector3.zero;
        Vector3 adjusted_centroid2 = Vector3.zero;
        float dist1, dist2; // check minimal distances
        for (int i = 0; i < m_TectonicPlates[rifted_plate].m_PlateVertices.Count; i++) // all plate vertices remain in the old plate unless they have a shorter distance to the second centroid
        {
            dist1 = UnitSphereDistance(m_DataVertices[m_TectonicPlates[rifted_plate].m_PlateVertices[i]], centroid1);
            dist2 = UnitSphereDistance(m_DataVertices[m_TectonicPlates[rifted_plate].m_PlateVertices[i]], centroid2);
            if (dist2 < dist1)
            {
                m_DataPointData[m_TectonicPlates[rifted_plate].m_PlateVertices[i]].plate = new_plate_index;
            }
        }

        for (int i = 0; i < m_PlanetManager.m_Settings.VoronoiBorderNoiseIterations; i++) // warp Voronoi cell borders
        {
            WarpCrustBordersTwoPlates(rifted_plate, new_plate_index);
        }


        for (int i = 0; i < m_TectonicPlates[rifted_plate].m_PlateVertices.Count; i++) // calculate new plate centroids from their vertices
        {
            if (m_DataPointData[m_TectonicPlates[rifted_plate].m_PlateVertices[i]].plate == rifted_plate) {
                adjusted_centroid1 += m_DataVertices[m_TectonicPlates[rifted_plate].m_PlateVertices[i]];
            } else
            {
                adjusted_centroid2 += m_DataVertices[m_TectonicPlates[rifted_plate].m_PlateVertices[i]];
            }
        }

        Plate new_plate = new Plate(this); // create new plate

        adjusted_centroid1 = adjusted_centroid1.magnitude > 0 ? adjusted_centroid1.normalized : new Vector3(m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f)).normalized; // normalize new centroids
        adjusted_centroid2 = adjusted_centroid2.magnitude > 0 ? adjusted_centroid2.normalized : new Vector3(m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f)).normalized;
        m_TectonicPlates[rifted_plate].m_Centroid = adjusted_centroid1;
        new_plate.m_Centroid = adjusted_centroid2;

        m_TectonicPlates[rifted_plate].m_PlateAngularSpeed = m_Random.Range(0.0f, m_PlanetManager.m_Settings.MaximumPlateSpeed); // set random plate speeds
        new_plate.m_PlateAngularSpeed = m_Random.Range(0.0f, m_PlanetManager.m_Settings.MaximumPlateSpeed);

        Vector3 new_axis1, new_axis2; // designate new rotation axes
        new_axis1 = Vector3.Cross(centroid1, centroid2); // axes should reflect opposite directions along the centroid line
        new_axis1 = new_axis1.magnitude > 0 ? new_axis1.normalized : new Vector3(m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f)).normalized; // normalize first axis
        new_axis2 = -new_axis1; // second axis is simply the opposite of the first one
        m_TectonicPlates[rifted_plate].m_RotationAxis = new_axis1; // assign axes
        new_plate.m_RotationAxis = new_axis2;
        m_TectonicPlates.Add(new_plate); // add new plate
        m_TectonicPlatesCount++; // increment the number of plates
    }

    /// <summary>
    /// Force a rift of the largest plate.
    /// </summary>
    public void ForcedPlateRift()
    {
        int plate_count = m_TectonicPlates.Count;

        int max_vertices_plate = -1;
        int max_vertices_n = 0;
        for (int i = 0; i < plate_count; i++) // find the largest
        {
            if (m_TectonicPlates[i].m_PlateVertices.Count > max_vertices_n)
            {
                max_vertices_plate = i;
                max_vertices_n = m_TectonicPlates[i].m_PlateVertices.Count;
            }
        }

        if (m_TectonicPlates[max_vertices_plate].m_PlateVertices.Count >= 2) // test for vertex count and call the rift
        {
            Debug.Log("Rifting plate " + max_vertices_plate);
            PlateRift(max_vertices_plate);
            ResampleCrust(); // resample afterwards
            CalculatePlatesVP(); // recalculate overlap matrix
            m_CBufferUpdatesNeeded["plate_motion_axes"] = true; // flag buffer updates
            m_CBufferUpdatesNeeded["plate_motion_angular_speeds"] = true;
            m_CBufferUpdatesNeeded["crust_vertex_data"] = true;
        }
        else {
            Debug.LogError("WTF plate, no rift because reasons."); // plate with less than two vertices
        }
    }

    /// <summary>
    /// Create a vector noise for each data triangle.
    /// </summary>
    public void CreateVectorNoise ()
    {
        Vector3 noise_vec;
        for (int i = 0; i < m_DataTriangles.Count; i++) // assign normalized random vector to each triangle, project to a tangent plane and normalize
        {
            do
            {
                noise_vec = new Vector3(m_Random.Range(0.0f, 1.0f), m_Random.Range(0.0f, 1.0f), m_Random.Range(0.0f, 1.0f));
                noise_vec = noise_vec - (Vector3.Dot(noise_vec, m_DataTriangles[i].m_BCenter)) * m_DataTriangles[i].m_BCenter;
            } while (noise_vec.magnitude == 0.0f);
            noise_vec = noise_vec.normalized;
            m_VectorNoise.Add(noise_vec);
        }
        for (int i = 0; i < m_PlanetManager.m_Settings.VectorNoiseAveragingIterations; i++) // average the noise over neighbours a number of times
        {
            List<Vector3> work_noise = new List<Vector3>(m_VectorNoise);
            for (int j = 0; j < m_DataTriangles.Count; j++)
            {
                foreach (int it in m_DataTriangles[j].m_Neighbours)
                {
                    Vector3 contrib = m_VectorNoise[it];
                    work_noise[j] += contrib - (Vector3.Dot(contrib, m_DataTriangles[j].m_BCenter)) * m_DataTriangles[j].m_BCenter; // contribution is the neighbour's noise vector projected into tangent plane

                }

            }
            m_VectorNoise = work_noise; // reassign after each iteration, so that results are consistent

        }

    }

    /// <summary>
    /// Warp borders of all plates to create a more natural look.
    /// </summary>
    public void WarpCrustBordersGlobal ()
    {
        int[] vertex_plates = new int[m_DataVertices.Count]; // array of plate indices for convenience
        for (int i = 0; i < m_DataVertices.Count; i++) // assign the indices
        {
            vertex_plates[i] = m_DataPointData[i].plate;
        }

        for (int i = 0; i < m_DataTriangles.Count; i++) // try every triangle
        {
            int p1, p2, p3;
            Vector3 v1, v2, v3;
            float maxdot = Mathf.NegativeInfinity; // test for dot product of vector noise and shift in the direction of the maximal
            float mindot = Mathf.Infinity;
            int maxdotind = -1;
            int mindotind = -1;
            p1 = m_DataPointData[m_DataTriangles[i].m_A].plate; // test if the triangle is border
            p2 = m_DataPointData[m_DataTriangles[i].m_B].plate;
            p3 = m_DataPointData[m_DataTriangles[i].m_C].plate;
            bool candidate = ((p1 == p2) && (p1 != p3)) || ((p2 == p3) && (p2 != p1)) || ((p3 == p1) && (p3 != p2));
            if (!candidate) // non-border triangles are skipped
            {
                continue;
            } else
            {
                if (m_Random.Range(0.0f, 1.0f) < m_VectorNoise[i].magnitude) // only change if a random number is less than the noise magnitude
                { 
                    v1 = m_DataVertices[m_DataTriangles[i].m_A] - m_DataTriangles[i].m_BCenter;
                    v2 = m_DataVertices[m_DataTriangles[i].m_B] - m_DataTriangles[i].m_BCenter;
                    v3 = m_DataVertices[m_DataTriangles[i].m_C] - m_DataTriangles[i].m_BCenter;
                    if (Vector3.Dot(m_VectorNoise[i], v1) > maxdot) // compare all three dot products
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
                    vertex_plates[maxdotind] = m_DataPointData[mindotind].plate; // assign the plate of the lowest dot as the plate of the highest dot
                }
            }
        }
        for (int i = 0; i < m_DataVertices.Count; i++) // assign the plates back to data layer
        {
            m_DataPointData[i].plate = vertex_plates[i];
        }
    }

    /// <summary>
    /// Warp the border between two plates
    /// </summary>
    /// <param name="a">first plate index</param>
    /// <param name="b">second plate index</param>
    public void WarpCrustBordersTwoPlates(int a, int b)
    {
        int[] vertex_plates = new int[m_DataVertices.Count]; // plate reassignment array
        for (int i = 0; i < m_DataVertices.Count; i++) // fill the current values
        {
            vertex_plates[i] = m_DataPointData[i].plate;
        }

        HashSet<int> allowed = new HashSet<int> { a, b }; // border triangle vertex plate indices are tested against an allowed set from the parameters
        HashSet<int> present = new HashSet<int>(); // every triangle fills plate indices here
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
            present.Add(p1); // fill the present set
            present.Add(p2);
            present.Add(p3);

            if (!present.IsSubsetOf(allowed)) // if the present indices are not a subset of allowed, triangle does not belong to either of the two plates
            {
                continue;
            }

            bool candidate = ((p1 == p2) && (p1 != p3)) || ((p2 == p3) && (p2 != p1)) || ((p3 == p1) && (p3 != p2)); // if triangle is border
            if (!candidate)
            {
                continue;
            }
            else
            {
                v1 = m_DataVertices[m_DataTriangles[i].m_A] - m_DataTriangles[i].m_BCenter; // similar to WarpCrustBorderGlobal
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
        for (int i = 0; i < m_DataVertices.Count; i++)
        {
            m_DataPointData[i].plate = vertex_plates[i];
        }
    }
}


