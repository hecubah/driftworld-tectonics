using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum OroType {UNKNOWN, ANDEAN, HIMALAYAN};

public class PointData // object containint additional vertex data
{
    public float elevation = 0; // elevation parameter
    public float thickness = 0; // thickness parameter
    public int plate = -1; // which plate the vertex belongs to - -1 means no plate
    public OroType orogeny = OroType.UNKNOWN;
    public float age = 0;


    public PointData() // default constructor with blank data - zero plate index means something only if the list of plates is not empty
    {
    }

    public PointData(PointData source)
    {
        elevation = source.elevation;
        thickness = source.thickness;
        plate = source.plate;
        orogeny = source.orogeny;
        age = source.age;
    }
}

public class Plate // maximal tectonic unit, the parameters drive certain decisions
{
    public List<int> m_PlateVertices; // all vertices belonging to the plate
    public List<int> m_PlateTriangles; // all triangles belonging to the plate
    public List<int> m_BorderTriangles; // border triangles of the plate
    public Vector3 m_RotationAxis; // axis around which the plate drifts
    public float m_PlateAngularSpeed; // angular speed of the plate drift
    public BoundingVolume m_BVHPlate; // bounding volume hiearchy of the plate
    public Quaternion m_Transform; // Relative transform caused by the motion of the plate
    public Vector3 m_Centroid; // centroid of the plate because it is called centroid not a cup of coffee
    public List<BoundingVolumeStruct> m_BVHArray; // List representing the BVH for array buffer feeding of the compute buffers
    
    public Plate (TectonicPlanet planet) // new plate with zeroed parameters
    {
        //m_Planet = planet;
        m_PlateVertices = new List<int>();
        m_PlateTriangles = new List<int>();
        m_BorderTriangles = new List<int>();
        m_BVHPlate = null;
        m_Transform = Quaternion.identity;
        m_Centroid = Vector3.zero;
        m_BVHArray = new List<BoundingVolumeStruct>();
    }

}

public class CollidingTerraine
{
    public int colliding_plate = -1;
    public int collided_plate = -1;
    public int index = 0;
    public List<int> m_Vertices = new List<int>();

}

