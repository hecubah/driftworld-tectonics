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

    public List<Vector3> m_CrustVertices;
    public List<DRTriangle> m_CrustTriangles;
    public List<List<int>> m_CrustVerticesNeighbours;
    public List<List<int>> m_CrustTrianglesOfVertices;
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

    public List<int> m_LookupStartTriangles;

    public List<Vector3> m_RenderVertices;
    public List<DRTriangle> m_RenderTriangles;
    public List<List<int>> m_RenderVerticesNeighbours;
    public List<List<int>> m_RenderTrianglesOfVertices;
    public List<PointData> m_RenderPointData;

    public int m_RenderVerticesCount;
    public int m_RenderTrianglesCount;

    public int m_TectonicPlatesCount;
    public List<Plate> m_TectonicPlates;

    public int[,] m_PlatesOverlap;

    public TectonicPlanet(float radius)
    {
        m_PlanetManager = (PlanetManager)GameObject.Find("Planet").GetComponent(typeof(PlanetManager));

        m_Radius = radius;

        m_Random = m_PlanetManager.m_Random;

        m_CrustVertices = new List<Vector3>();
        m_CrustTriangles = new List<DRTriangle>();
        m_CrustVerticesNeighbours = new List<List<int>>();
        m_CrustTrianglesOfVertices = new List<List<int>>();
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

        m_RenderVerticesCount = 0;
        m_RenderTrianglesCount = 0;

        m_TectonicPlates = new List<Plate>();
        m_PlatesOverlap = null;
    }

    public static float UnitSphereDistance(Vector3 a, Vector3 b)
    {
        return Mathf.Acos(Vector3.Dot(a, b));
    }

    public static float SphereDistance(Vector3 a, Vector3 b, float radius)
    {
        return radius * UnitSphereDistance(a, b);
    }

    public int SearchDataTrianglesForPointBruteForce(Vector3 needle)
    {
        float mindist = Mathf.Infinity;
        float dist;
        int current_searched_triangle = 0; // hopefully dummy variable definition
        for (int i = 0; i < m_TrianglesCount; i++)
        {
            dist = UnitSphereDistance(needle, m_DataTriangles[i].m_BCenter);
            if (dist < mindist)
            {
                mindist = dist;
                current_searched_triangle = i;
            }
        }
        if (!m_DataTriangles[current_searched_triangle].Contains(needle))
        {
            for (int i = 0; i < m_DataTriangles[current_searched_triangle].m_Neighbours.Count; i++)
            {
                if (m_DataTriangles[i].Contains(needle))
                {
                    current_searched_triangle = i;
                    break;
                }

            }
        }
        return current_searched_triangle;

    }

    public int SearchDataTrianglesForPoint(Vector3 needle)
    {
        bool closest;
        float mindist = Mathf.Infinity;
        float dist;
        int current_searched_triangle = 0; // hopefully dummy variable definition
        int aux_triangle_index;
        for (int i = 0; i < m_LookupStartTriangles.Count; i++)
        {
            dist = UnitSphereDistance(needle, m_DataTriangles[m_LookupStartTriangles[i]].m_BCenter);
            if (dist < mindist)
            {
                mindist = dist;
                current_searched_triangle = m_LookupStartTriangles[i];
            }
        }
        do
        {
            aux_triangle_index = current_searched_triangle;
            closest = true;
            for (int i = 0; i < m_DataTriangles[current_searched_triangle].m_Neighbours.Count; i++)
            {
                dist = UnitSphereDistance(needle, m_DataTriangles[m_DataTriangles[current_searched_triangle].m_Neighbours[i]].m_BCenter);
                if (dist < mindist)
                {
                    mindist = dist;
                    aux_triangle_index = m_DataTriangles[current_searched_triangle].m_Neighbours[i];
                    closest = false;
                }
            }
            current_searched_triangle = aux_triangle_index;
        } while (!closest);
        if (!m_DataTriangles[current_searched_triangle].Contains(needle))
        {
            for (int i = 0; i < m_DataTriangles[current_searched_triangle].m_Neighbours.Count; i++)
            {
                if (m_DataTriangles[m_DataTriangles[current_searched_triangle].m_Neighbours[i]].Contains(needle))
                {
                    current_searched_triangle = m_DataTriangles[current_searched_triangle].m_Neighbours[i];
                    break;
                }

            }
        }
        return current_searched_triangle;
    }

    public List<int> SearchDataForPoint(Vector3 needle)
    {
        List<int> retVal = m_DataBVH.SearchForPoint(needle, m_DataTriangles);
        return retVal;
    }

    public void CrustToData() // WIP
    {
        ComputeShader work_shader = m_PlanetManager.m_CrustToDataBaseShader;

        int kernelHandle = work_shader.FindKernel("CSCrustToDataBase");

        Vector3[] triangle_points = new Vector3[3 * m_TrianglesCount];
        float[] point_values = new float[3 * m_TrianglesCount];
        for (int i = 0; i < m_TrianglesCount; i++)
        {
            triangle_points[3 * i + 0] = m_CrustVertices[m_CrustTriangles[i].m_A];
            triangle_points[3 * i + 1] = m_CrustVertices[m_CrustTriangles[i].m_B];
            triangle_points[3 * i + 2] = m_CrustVertices[m_CrustTriangles[i].m_C];
            point_values[3 * i + 0] = m_CrustPointData[m_CrustTriangles[i].m_A].elevation;
            point_values[3 * i + 1] = m_CrustPointData[m_CrustTriangles[i].m_B].elevation;
            point_values[3 * i + 2] = m_CrustPointData[m_CrustTriangles[i].m_C].elevation;
        }

        int[] overlap_matrix = new int[m_TectonicPlatesCount * m_TectonicPlatesCount];
        int[] BVH_array_sizes = new int[m_TectonicPlatesCount];
        List<BoundingVolumeStruct> BVArray_pass = new List<BoundingVolumeStruct>();
        Vector4[] plate_transforms = new Vector4[m_TectonicPlatesCount];

        for (int i = 0; i < m_TectonicPlatesCount; i++)
        {
            for (int j = 0; j < m_TectonicPlatesCount; j++)
            {
                overlap_matrix[i * m_TectonicPlatesCount + j] = m_PlatesOverlap[i, j];
            }
            BVH_array_sizes[i] = m_TectonicPlates[i].m_BVHArray.Count;
            BVArray_pass.AddRange(m_TectonicPlates[i].m_BVHArray);
            Vector4 added_transform = new Vector4();
            added_transform.x = m_TectonicPlates[i].m_Transform.x;
            added_transform.y = m_TectonicPlates[i].m_Transform.y;
            added_transform.z = m_TectonicPlates[i].m_Transform.z;
            added_transform.w = m_TectonicPlates[i].m_Transform.w;
            plate_transforms[i] = added_transform;

        }
        BoundingVolumeStruct[] BVArray_finished = BVArray_pass.ToArray();

        Vector3[] data_vertices = m_DataVertices.ToArray();

        ComputeBuffer triangle_points_buffer = new ComputeBuffer(triangle_points.Length, 12, ComputeBufferType.Default);
        ComputeBuffer point_values_buffer = new ComputeBuffer(point_values.Length, 4, ComputeBufferType.Default);
        ComputeBuffer overlap_matrix_buffer = new ComputeBuffer(overlap_matrix.Length, 4, ComputeBufferType.Default);
        ComputeBuffer BVH_array_sizes_buffer = new ComputeBuffer(BVH_array_sizes.Length, 4, ComputeBufferType.Default);
        ComputeBuffer BVArray_finished_buffer = new ComputeBuffer(BVArray_finished.Length, 32, ComputeBufferType.Default);
        ComputeBuffer plate_transforms_buffer = new ComputeBuffer(plate_transforms.Length, 16, ComputeBufferType.Default);

        ComputeBuffer data_vertices_buffer = new ComputeBuffer(m_VerticesCount, 12, ComputeBufferType.Default);
        ComputeBuffer elevations_out_buffer = new ComputeBuffer(m_VerticesCount, 4, ComputeBufferType.Default);
        ComputeBuffer plates_out_buffer = new ComputeBuffer(m_VerticesCount, 4, ComputeBufferType.Default);


        triangle_points_buffer.SetData(triangle_points);
        point_values_buffer.SetData(point_values);

        overlap_matrix_buffer.SetData(overlap_matrix);
        BVH_array_sizes_buffer.SetData(BVH_array_sizes);
        BVArray_finished_buffer.SetData(BVArray_finished);
        plate_transforms_buffer.SetData(plate_transforms);

        data_vertices_buffer.SetData(data_vertices);

        work_shader.SetBuffer(kernelHandle, "triangle_points", triangle_points_buffer);
        work_shader.SetBuffer(kernelHandle, "point_values", point_values_buffer);
        work_shader.SetInt("n_plates", m_TectonicPlatesCount);

        work_shader.SetBuffer(kernelHandle, "overlap_matrix", overlap_matrix_buffer);
        work_shader.SetBuffer(kernelHandle, "BVH_array_sizes", BVH_array_sizes_buffer);
        work_shader.SetBuffer(kernelHandle, "BVH_array", BVArray_finished_buffer);
        work_shader.SetBuffer(kernelHandle, "plate_transforms", plate_transforms_buffer);

        work_shader.SetBuffer(kernelHandle, "data_vertices", data_vertices_buffer);
        work_shader.SetBuffer(kernelHandle, "elevations_out", elevations_out_buffer);
        work_shader.SetBuffer(kernelHandle, "plates_out", plates_out_buffer);
        work_shader.SetFloat("ocean_base_floor", APR.OceanBaseFloor);
        work_shader.SetInt("n_data_vertices", m_VerticesCount);


        work_shader.Dispatch(kernelHandle, m_VerticesCount/64 + (m_VerticesCount%64 != 0 ? 1 : 0), 1, 1);

        triangle_points_buffer.Release();
        point_values_buffer.Release();

        overlap_matrix_buffer.Release();
        BVH_array_sizes_buffer.Release();
        BVArray_finished_buffer.Release();
        plate_transforms_buffer.Release();

        data_vertices_buffer.Release();

        float[] elevations_out = new float[m_VerticesCount];
        elevations_out_buffer.GetData(elevations_out);

        for (int i = 0; i < m_VerticesCount; i++)
        {
            m_DataPointData[i].elevation = elevations_out[i];
        }

        elevations_out_buffer.Release();
        plates_out_buffer.Release();

        /*
        for (int i = 0; i < m_VerticesCount; i++)
        {
            bool found = false;
            PointData interpolated_data = new PointData();
            for (int j = 0; j < m_TectonicPlatesCount; j++)
            {
                for (int k = 0; k < m_TectonicPlates[j].m_PlateTriangles.Count; k++)
                {
                    if (m_DataTriangles[m_TectonicPlates[j].m_PlateTriangles[k]].Contains(m_DataVertices[i]))
                    {
                        if (found)
                        {

                        } else
                        {
                            //float highest_elevation =;
                        }
                    }
                }
            }

            if (found)
            {

            } else
            {

            }
        }
        */
    }

    public void DataToRender(bool propagate_crust)
    {
        if (propagate_crust)
        {
            CrustToData();
        }

        ComputeShader work_shader = m_PlanetManager.m_DataToRenderShader;

        int kernelHandle = work_shader.FindKernel("CSDataToRender");

        Vector3[] triangle_points = new Vector3[3 * m_TrianglesCount];

        int[] triangle_neighbours = new int[3 * m_RenderTrianglesCount];
        for (int i = 0; i < m_RenderTrianglesCount; i++)
        {
            triangle_neighbours[3 * i + 0] = m_RenderTriangles[i].m_Neighbours[0];
            triangle_neighbours[3 * i + 1] = m_RenderTriangles[i].m_Neighbours[1];
            triangle_neighbours[3 * i + 2] = m_RenderTriangles[i].m_Neighbours[2];
        }


        float[] point_values = new float[3 * m_TrianglesCount];
        for (int i = 0; i < m_TrianglesCount; i++)
        {
            triangle_points[3 * i + 0] = m_DataVertices[m_DataTriangles[i].m_A];
            triangle_points[3 * i + 1] = m_DataVertices[m_DataTriangles[i].m_B];
            triangle_points[3 * i + 2] = m_DataVertices[m_DataTriangles[i].m_C];
            point_values[3 * i + 0] = m_DataPointData[m_DataTriangles[i].m_A].elevation;
            point_values[3 * i + 1] = m_DataPointData[m_DataTriangles[i].m_B].elevation;
            point_values[3 * i + 2] = m_DataPointData[m_DataTriangles[i].m_C].elevation;
        }

        BoundingVolumeStruct[] BVArray = m_DataBVHArray.ToArray();

        Vector3[] render_vertices = m_RenderVertices.ToArray();

        ComputeBuffer triangle_points_buffer = new ComputeBuffer(triangle_points.Length, 12, ComputeBufferType.Default);
        ComputeBuffer triangle_neighbours_buffer = new ComputeBuffer(triangle_neighbours.Length, 4, ComputeBufferType.Default);
        ComputeBuffer point_values_buffer = new ComputeBuffer(point_values.Length, 4, ComputeBufferType.Default);
        ComputeBuffer BVArray_buffer = new ComputeBuffer(BVArray.Length, 32, ComputeBufferType.Default);

        ComputeBuffer render_vertices_buffer = new ComputeBuffer(m_VerticesCount, 12, ComputeBufferType.Default);
        ComputeBuffer elevations_out_buffer = new ComputeBuffer(m_VerticesCount, 4, ComputeBufferType.Default);


        triangle_points_buffer.SetData(triangle_points);
        triangle_neighbours_buffer.SetData(triangle_neighbours);
        point_values_buffer.SetData(point_values);

        BVArray_buffer.SetData(BVArray);

        render_vertices_buffer.SetData(render_vertices);

        work_shader.SetBuffer(kernelHandle, "triangle_points", triangle_points_buffer);
        work_shader.SetBuffer(kernelHandle, "triangle_neighbours", triangle_neighbours_buffer);
        work_shader.SetBuffer(kernelHandle, "point_values", point_values_buffer);

        work_shader.SetBuffer(kernelHandle, "BVH_array", BVArray_buffer);

        work_shader.SetBuffer(kernelHandle, "render_vertices", render_vertices_buffer);
        work_shader.SetBuffer(kernelHandle, "elevations_out", elevations_out_buffer);
        work_shader.SetInt("n_render_vertices", m_RenderVerticesCount);


        work_shader.Dispatch(kernelHandle, m_RenderVerticesCount / 64 + (m_RenderVerticesCount % 64 != 0 ? 1 : 0), 1, 1);

        triangle_points_buffer.Release();
        triangle_neighbours_buffer.Release();
        point_values_buffer.Release();

        BVArray_buffer.Release();

        render_vertices_buffer.Release();

        float[] elevations_out = new float[m_RenderVerticesCount];
        elevations_out_buffer.GetData(elevations_out);

        for (int i = 0; i < m_RenderVerticesCount; i++)
        {
            m_RenderPointData[i].elevation = elevations_out[i];
        }

        elevations_out_buffer.Release();


        /*
        for (int i = 0; i < m_RenderVerticesCount; i++)
        {
            m_RenderPointData[i] = InterpolatePointFromData(m_RenderVertices[i]);
        }
        */

    }

    public void CrustMesh(out Vector3[] vertices_array, out int[] triangles_array)
    {
        vertices_array = new Vector3[m_VerticesCount];
        float elevation;
        for (int i = 0; i < m_VerticesCount; i++)
        {
            elevation = (m_CrustPointData[i].elevation > 0 ? m_CrustPointData[i].elevation : 0);
            vertices_array[i] = (m_Radius + elevation) * (m_TectonicPlates[m_CrustPointData[i].plate].m_Transform * m_CrustVertices[i]);
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
            vertices_array[i] = (m_Radius + elevation) * vertices_array[i];
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
            vertices_array[i] = (m_Radius + elevation) * vertices_array[i];
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

    public PointData InterpolatePointFromData(Vector3 point)
    {
        PointData retVal = new PointData();
        int which_triangle = SearchDataTrianglesForPoint(point);
        
        float d00, d01, d11, d20, d21, den;
        float u, v, w;
        Vector3 a, b, c, v0, v1, v2;
        int ai, bi, ci, max_weight;
        a = m_DataVertices[m_DataTriangles[which_triangle].m_A];
        b = m_DataVertices[m_DataTriangles[which_triangle].m_B];
        c = m_DataVertices[m_DataTriangles[which_triangle].m_C];
        ai = m_DataTriangles[which_triangle].m_A;
        bi = m_DataTriangles[which_triangle].m_B;
        ci = m_DataTriangles[which_triangle].m_C;
        v0 = b - a;
        v1 = c - a;
        v2 = point - a;
        d00 = Vector3.Dot(v0, v0);
        d01 = Vector3.Dot(v0, v1);
        d11 = Vector3.Dot(v1, v1);
        d20 = Vector3.Dot(v2, v0);
        d21 = Vector3.Dot(v2, v1);
        den = d00 * d11 - d01 * d01;
        v = (d11 * d20 - d01 * d21) / den;
        max_weight = 2;
        w = (d00 * d21 - d01 * d20) / den;
        if (w > max_weight)
            max_weight = 3;
        u = 1.0f - v - w;
        if (u > max_weight)
            max_weight = 1;

        retVal.elevation = m_DataPointData[ai].elevation * u + m_DataPointData[bi].elevation * v + m_DataPointData[ci].elevation * w;
        
        switch (max_weight)
        {
            case 1:
                retVal.plate = m_DataPointData[ai].plate;
                break;
            case 2:
                retVal.plate = m_DataPointData[bi].plate;
                break;
            case 3:
                retVal.plate = m_DataPointData[ci].plate;
                break;
            default:
                break;
        }
        
        return retVal;
    }

    public PointData InterpolatePointFromTriangle(Vector3 point, DRTriangle triangle)
    {
        if (!triangle.Contains(point))
        {
            return null;
        }
        PointData retVal = new PointData();

        float d00, d01, d11, d20, d21, den;
        float u, v, w;
        Vector3 a, b, c, v0, v1, v2;
        int ai, bi, ci, max_weight;
        a = m_DataVertices[triangle.m_A];
        b = m_DataVertices[triangle.m_B];
        c = m_DataVertices[triangle.m_C];
        ai = triangle.m_A;
        bi = triangle.m_B;
        ci = triangle.m_C;
        v0 = b - a;
        v1 = c - a;
        v2 = point - a;
        d00 = Vector3.Dot(v0, v0);
        d01 = Vector3.Dot(v0, v1);
        d11 = Vector3.Dot(v1, v1);
        d20 = Vector3.Dot(v2, v0);
        d21 = Vector3.Dot(v2, v1);
        den = d00 * d11 - d01 * d01;
        v = (d11 * d20 - d01 * d21) / den;
        max_weight = 2;
        w = (d00 * d21 - d01 * d20) / den;
        if (w > max_weight)
            max_weight = 3;
        u = 1.0f - v - w;
        if (u > max_weight)
            max_weight = 1;

        retVal.elevation = m_DataPointData[ai].elevation * u + m_DataPointData[bi].elevation * v + m_DataPointData[ci].elevation * w;

        switch (max_weight)
        {
            case 1:
                retVal.plate = m_DataPointData[ai].plate;
                break;
            case 2:
                retVal.plate = m_DataPointData[bi].plate;
                break;
            case 3:
                retVal.plate = m_DataPointData[ci].plate;
                break;
            default:
                break;
        }

        return retVal;

    }

    public void MarkupTerrain ()
    {
        bool mark;
        Vector3 pointNorth, pointSouthHemi;
        pointNorth = new Vector3(0,1,0);
        pointSouthHemi = new Vector3(1,-1,-1).normalized;
        for (int i = 0; i < m_VerticesCount; i++)
        {
            Vector3 point = m_DataVertices[i];
            mark = UnitSphereDistance(point, pointNorth) < Mathf.PI / 6;
            mark = mark || ((point.y > -0.1f) && (point.y < 0.1f));
            mark = mark || (UnitSphereDistance(point, pointSouthHemi) < Math.PI / 12);
            if (mark)
                m_DataPointData[i].elevation = APR.MarkupElevation*m_Radius;
            else
                m_DataPointData[i].elevation = 0.0f;
        }
    }

    public void GenerateFractalTerrain ()
    {
        
        int kernelHandle = m_PlanetManager.m_FractalTerrainCShader.FindKernel("CSFractalTerrain");

        Vector3[] vertices_input = new Vector3[m_VerticesCount];
        Vector3[] random_input = new Vector3[64*APR.FractalTerrainIterations];
        float[] elevations_output = new float[m_VerticesCount];

        for (int i = 0; i < m_VerticesCount; i++)
        {
            vertices_input[i] = m_DataVertices[i];
        }

        for (int i = 0; i < 64*APR.FractalTerrainIterations; i++)
        {
            random_input[i] = new Vector3(m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f));
        }

        ComputeBuffer vertices_input_buffer = new ComputeBuffer(vertices_input.Length, 12, ComputeBufferType.Default);
        ComputeBuffer random_input_buffer = new ComputeBuffer(random_input.Length, 12, ComputeBufferType.Default);
        ComputeBuffer elevations_output_buffer = new ComputeBuffer(elevations_output.Length, 4, ComputeBufferType.Default);
        vertices_input_buffer.SetData(vertices_input);
        random_input_buffer.SetData(random_input);

        m_PlanetManager.m_FractalTerrainCShader.SetBuffer(kernelHandle, "vertices_input", vertices_input_buffer);
        m_PlanetManager.m_FractalTerrainCShader.SetBuffer(kernelHandle, "random_input", random_input_buffer);
        m_PlanetManager.m_FractalTerrainCShader.SetBuffer(kernelHandle, "elevations_output", elevations_output_buffer);
        m_PlanetManager.m_FractalTerrainCShader.SetInt("vertices_number", m_VerticesCount);
        m_PlanetManager.m_FractalTerrainCShader.SetFloat("elevation_step", APR.FractalTerrainElevationStep);
        m_PlanetManager.m_FractalTerrainCShader.Dispatch(kernelHandle, APR.FractalTerrainIterations, 1, 1);

        vertices_input_buffer.Release();
        random_input_buffer.Release();
        elevations_output_buffer.GetData(elevations_output);

        for (int i = 0; i < m_VerticesCount; i++)
        {
            m_DataPointData[i].elevation = elevations_output[i];
        }
        elevations_output_buffer.Release();   
    }

    // Create new crust as random centroid set Voronoi map
    public void InitializeRandomCrust()
    {
        List<Vector3> centroids = new List<Vector3>(); // vertices are assigned around these centroids
        List<Plate> plates = new List<Plate>(); // formal partition objects
        for (int i = 0; i < APR.PlateInitNumberOfCentroids; i++) // for each centroid
        {
            centroids.Add(new Vector3(m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f)).normalized); // set the centroid vector
            Plate new_plate = new Plate(this); // create a new plate
            new_plate.m_RotationAxis = new Vector3(m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f), m_Random.Range(-1.0f, 1.0f)).normalized; // randomized rotation axis
            new_plate.m_PlateAngularSpeed = m_Random.Range(0.0f, APR.MaxPlateAngularSpeed); // angular speed of the plate
            if (m_Random.Range(0f, 1f) < APR.PlateInitLandRatio)
            {
                new_plate.m_InitElevation = APR.PlateInitLandElevation; // plate is continental
            } else
            {
                new_plate.m_InitElevation = APR.PlateInitSeaElevation; // plate is oceanic
            }
            plates.Add(new_plate); // add new plate to the list
        }
        m_CrustVertices = new List<Vector3>();
        m_CrustPointData = new List<PointData>();
        for (int i = 0; i < m_DataVertices.Count; i++) // for all vertices on the global mesh
        {
            float mindist = Mathf.Infinity;
            int plate_index = 0;
            for (int j = 0; j < centroids.Count; j++)
            {
                float dist = TectonicPlanet.UnitSphereDistance(m_DataVertices[i], centroids[j]);
                if (dist < mindist)
                {
                    mindist = dist;
                    plate_index = j;
                }
            }
            float el, thick;
            el = plates[plate_index].m_InitElevation;
            thick = m_Random.Range(APR.CrustThicknessMin, APR.CrustThicknessMax);
            plates[plate_index].m_Mass += thick;
            plates[plate_index].m_Type += el;
            m_DataPointData[i].elevation = el;
            m_DataPointData[i].thickness = thick;
            m_DataPointData[i].plate = plate_index;
            plates[plate_index].m_PlateVertices.Add(i);
            m_CrustVertices.Add(m_DataVertices[i]);
            m_CrustPointData.Add(new PointData(m_DataPointData[i]));
        }
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
            int plate_tricount = it.m_PlateTriangles.Count();
            for (int i = 0; i < plate_tricount; i++) // for all triangles in data
            {
                int tri_index = it.m_PlateTriangles[i];
                BoundingVolume new_bb = new BoundingVolume(m_CrustTriangles[tri_index].m_CCenter, m_CrustTriangles[tri_index].m_CUnitRadius); // create a leaf bounding box
                new_bb.m_TriangleIndex = tri_index; // denote the triangle index to the leaf
                m_CrustTriangles[tri_index].m_BVolume = new_bb; // denote the leaf to the respective triangle
                bvt_leaves.Add(new_bb); // add the new bounding volume to the list of leaves
            }
            it.m_BVHPlate = ConstructBVH(bvt_leaves);
            it.m_BVHArray = BoundingVolume.BuildBVHArray(it.m_BVHPlate);
        }

        m_TectonicPlates = plates;
        m_TectonicPlatesCount = plates.Count;

        m_PlatesOverlap = CalculatePlatesVP();
        DetermineBorderTriangles();
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
    }

    public void MovePlates ()
    {
        for (int i = 0; i < m_TectonicPlatesCount; i++)
        {
            m_TectonicPlates[i].m_Transform *= Quaternion.AngleAxis(APR.TectonicIterationStepTime * m_TectonicPlates[i].m_PlateAngularSpeed * 180.0f / Mathf.PI, m_TectonicPlates[i].m_RotationAxis);
        }
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

            int kernelHandle = m_PlanetManager.m_BVHNearestNeighbourShader.FindKernel("CSBVHNN");

            Vector3[] cluster_positions = new Vector3[list_size];
            for (int i = 0; i < list_size; i++)
            {
                cluster_positions[i] = bvlist_in[i].m_Circumcenter;
            }

            ComputeBuffer cluster_positions_buffer = new ComputeBuffer(list_size, 12, ComputeBufferType.Default);
            ComputeBuffer nearest_neighbours_buffer = new ComputeBuffer(list_size, 4, ComputeBufferType.Default);

            cluster_positions_buffer.SetData(cluster_positions);

            m_PlanetManager.m_BVHNearestNeighbourShader.SetBuffer(kernelHandle, "cluster_positions", cluster_positions_buffer);
            m_PlanetManager.m_BVHNearestNeighbourShader.SetBuffer(kernelHandle, "nearest_neighbours", nearest_neighbours_buffer);

            m_PlanetManager.m_BVHNearestNeighbourShader.SetInt("array_size", list_size);
            m_PlanetManager.m_BVHNearestNeighbourShader.SetInt("BVH_radius", APR.BVHConstructionRadius);
            m_PlanetManager.m_BVHNearestNeighbourShader.Dispatch(kernelHandle, (list_size/64) + 1, 1, 1);

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

}


