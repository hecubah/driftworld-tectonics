using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointData // object containint additional vertex data
{
    public float elevation = 0; // elevation parameter
    public float thickness = 0; // thickness parameter
    public int plate = 0; // which plate the vertex belongs to

    public PointData() // default constructor with blank data - zero plate index means something only if the list of plates is not empty
    {
        elevation = 0; // basic elevation is at sea level
        plate = 0; // default, only has meaning with a non-empty list of plates
    }

    public PointData(PointData source) // copy constructor - unused at the moment
    {
        elevation = source.elevation;
        plate = source.plate;
    }
}

public class Plate // maximal tectonic unit, the parameters drive certain decisions
{
    public TectonicPlanet m_Planet; // to which planet the plate belongs to
    public List<int> m_PlateVertices; // all vertices belonging to the plate
    public List<int> m_PlateTriangles; // all triangles belonging to the plate
    public List<int> m_BorderTriangles; // border triangles of the plate
    public Vector3 m_RotationAxis; // axis around which the plate drifts
    public float m_PlateAngularSpeed; // angular speed of the plate drift
    public float m_InitElevation; // mean elevation
    public BoundingVolume m_BVHPlate; // bounding volume hiearchy of the plate
    public float m_Mass; // total mass of the plate, sum of thickness values
    public float m_Type; // oceanic or continental plate, sum of elevation values
    public Quaternion m_Transform; // Relative transform caused by the motion of the plate
    public List<BoundingVolumeStruct> m_BVHArray; // List representing the BVH for array buffer feeding of the compute buffers
    
    public Plate (TectonicPlanet planet) // new plate with zeroed parameters
    {
        m_Planet = planet;
        m_PlateVertices = new List<int>();
        m_PlateTriangles = new List<int>();
        m_BorderTriangles = new List<int>();
        m_BVHPlate = null;
        m_Transform = Quaternion.identity;
        m_BVHArray = new List<BoundingVolumeStruct>();
        m_Type = 0.0f;
        m_Mass = 0.0f;
    }

    public void BuildBVHArray ()
    {
        m_BVHArray = new List<BoundingVolumeStruct>();
        if (m_BVHPlate != null)
        {
            Queue<BoundingVolume> queue_feed = new Queue<BoundingVolume>();
            int border_index = 0;
            queue_feed.Enqueue(m_BVHPlate);
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
                } else
                {
                    fill.n_children = 0;
                    fill.left_child = 0;
                    fill.right_child = 0;
                    fill.triangle_index = source.m_TriangleIndex;
                    fill.circumcenter = source.m_Circumcenter;
                    fill.circumradius = source.m_Circumradius;
                }
                m_BVHArray.Add(fill);
            }

        }

    }
}
public class Crust
{

}

public struct BoundingVolumeStruct
{
    public int n_children;
    public int left_child;
    public int right_child;
    public int triangle_index;
    public Vector3 circumcenter;
    public float circumradius;
}