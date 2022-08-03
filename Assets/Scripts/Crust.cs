using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orogeny type enum. Andean is for points rising above ocean level by subduction, Himalayan by continental collision.
/// </summary>
public enum OroType {UNKNOWN, ANDEAN, HIMALAYAN};

/// <summary>
/// Contains information about specific vertex.
/// </summary>
public class PointData
{
    public float elevation = 0; // vertex elevation relative to ocean level
    public float thickness = 0; // thickness parameter - calculated on demand by simply adding 
    public int plate = -1; // which plate the vertex belongs to - -1 means no plate
    public OroType orogeny = OroType.UNKNOWN; // vertex orogeny designation
    public float age = 0; // vertex crust age


    public PointData() // default constructor with blank data
    {
    }

    public PointData(PointData source) // copy constructor
    {
        elevation = source.elevation;
        thickness = source.thickness;
        plate = source.plate;
        orogeny = source.orogeny;
        age = source.age;
    }
}

/// <summary>
/// Widest tectonic element - all tectonic computations are done on a list of these objects.
/// </summary>
public class Plate
{
    public List<int> m_PlateVertices; // all vertices belonging to the plate
    public List<int> m_PlateTriangles; // all triangles belonging to the plate
    public List<int> m_BorderTriangles; // border triangles of the plate, i. e. triangles neighbouring triangles with vertices of non-uniform plate indices
    public Vector3 m_RotationAxis; // axis around which the plate drifts
    public float m_PlateAngularSpeed; // angular speed of the plate drift
    public BoundingVolume m_BVHPlate; // bounding volume hiearchy of the plate triangles
    public Quaternion m_Transform; // relative transform of the plate  - the whole plate moves as a rigid body, keeping a transform information relative to the initial state - most of the calculations apply the transform on demand without actually moving the vertex
    public Vector3 m_Centroid; // centroid of all of the vertices, normalized to unit sphere
    public List<BoundingVolumeStruct> m_BVHArray; // List representing the BVH for array buffer feeding of the compute buffers
    
    /// <summary>
    /// Fresh empty plate constructor.
    /// </summary>
    /// <param name="planet"></param>
    public Plate (TectonicPlanet planet)
    {
        m_PlateVertices = new List<int>();
        m_PlateTriangles = new List<int>();
        m_BorderTriangles = new List<int>();
        m_BVHPlate = null;
        m_Transform = Quaternion.identity;
        m_Centroid = Vector3.zero;
        m_BVHArray = new List<BoundingVolumeStruct>();
    }
}

/// <summary>
/// Set of conected continental vertices - individual terranes are attached to the collided plate.
/// </summary>
public class CollidingTerrane
{
    public int colliding_plate = -1; // index of the plate the terrane belongs to
    public int collided_plate = -1; // index of the plate the terrane is colliding with (and will be attached to)
    public int index = 0; // primary index identifier of the terrane
    public List<int> m_Vertices = new List<int>(); // set of connected continental vertices - this set has no continental neighbours (or at least it shouldn't have)
    public float mutual_speed = 0.0f; // mutual speed of the plates
}

