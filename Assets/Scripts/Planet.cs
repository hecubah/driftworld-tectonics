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

    public void RecalculateLookups()
    {
        m_LookupStartTriangles.Clear();
        m_LookupStartTriangles.Add(SearchDataTrianglesForPointBruteForce(Vector3.up));
        m_LookupStartTriangles.Add(SearchDataTrianglesForPoint(Vector3.forward));
        m_LookupStartTriangles.Add(SearchDataTrianglesForPoint(Vector3.left));
        m_LookupStartTriangles.Add(SearchDataTrianglesForPoint(Vector3.back));
        m_LookupStartTriangles.Add(SearchDataTrianglesForPoint(Vector3.right));
        m_LookupStartTriangles.Add(SearchDataTrianglesForPoint(Vector3.down));
    }

    public void CrustToData() // WIP
    {
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
    }

    public void DataToRender(bool propagate_crust)
    {
        if (propagate_crust)
        {
            CrustToData();
        }
        for (int i = 0; i < m_RenderVerticesCount; i++)
        {
            m_RenderPointData[i] = InterpolatePointFromData(m_RenderVertices[i]);
        }

    }

    public void CrustMesh(out Vector3[] vertices_array, out int[] triangles_array)
    {
        vertices_array = m_CrustVertices.ToArray();
        float elevation;
        for (int i = 0; i < m_VerticesCount; i++)
        {
            if (m_CrustPointData[i].elevation > 0)
            {
                elevation = m_CrustPointData[i].elevation;
            }
            else
            {
                elevation = 0;
            }
            vertices_array[i] = (m_Radius + elevation) * vertices_array[i];
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
            if (m_DataPointData[i].elevation > 0)
            {
                elevation = m_DataPointData[i].elevation;
            }
            else
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
            if (m_RenderPointData[i].elevation > 0)
            {
                elevation = m_RenderPointData[i].elevation;
            }
            else
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
            new_plate.m_Elevation = APR.PlateInitElevation; // initial elevation of all vertices in the plate
            plates.Add(new_plate); // add new plate to the list
        }
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
            m_DataPointData[i].elevation = plates[plate_index].m_Elevation;
            m_DataPointData[i].plate = plate_index;
            plates[plate_index].m_PlateVertices.Add(i);
        }
        bool borderTriangle = false;
        for (int i = 0; i < m_DataTriangles.Count; i++)
        {
            if ((m_DataPointData[m_DataTriangles[i].m_A].plate == m_DataPointData[m_DataTriangles[i].m_B].plate) && (m_DataPointData[m_DataTriangles[i].m_B].plate == m_DataPointData[m_DataTriangles[i].m_C].plate))
            {
                foreach (int it in m_DataTriangles[i].m_Neighbours)
                {
                    if ((m_DataPointData[m_DataTriangles[it].m_A].plate != m_DataPointData[m_DataTriangles[it].m_B].plate) || (m_DataPointData[m_DataTriangles[it].m_B].plate != m_DataPointData[m_DataTriangles[it].m_C].plate))
                    {
                        borderTriangle = true;
                        break;
                    }
                }
                plates[m_DataPointData[m_DataTriangles[i].m_A].plate].m_PlateTriangles.Add(i);
            }
            if (borderTriangle)
            {
                plates[m_DataPointData[m_DataTriangles[i].m_A].plate].m_BorderTriangles.Add(i);
            }
        }
        foreach (Plate it in plates) {
            it.m_TerrainAnchors.Add(it.m_BorderTriangles[0]);
        }
        m_TectonicPlates = plates;
        m_TectonicPlatesCount = plates.Count;

        ConstructBoundingBoxHiearchy();
    }

    public void ConstructBoundingBoxHiearchy ()
    {

    }

    public void MovePlates ()
    {
        Vector3 move;
        for (int i = 0; i < m_VerticesCount; i++)
        {
            move = APR.TectonicIterationStepTime*m_TectonicPlates[m_CrustPointData[i].plate].m_PlateAngularSpeed * (Vector3.Cross(m_TectonicPlates[m_CrustPointData[i].plate].m_RotationAxis, m_CrustVertices[i]));
            m_CrustVertices[i] = (m_CrustVertices[i] + move).normalized;
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


