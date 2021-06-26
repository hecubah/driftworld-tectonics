using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointData // object containint additional vertex data
{
    public float elevation = 0; // elevation parameter
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
    public List<int> m_BorderTriangles; // triangles bordering triangles that do not belong to the same plate - CHECK
    public List<int> m_TerrainAnchors; // absolutely no idea what that is
    public Vector3 m_RotationAxis; // axis around which the plate drifts
    public float m_PlateAngularSpeed; // angular speed of the plate drift
    public float m_Elevation; // mean elevation
    public Plate (TectonicPlanet planet) // new plate with zeroed parameters
    {
        m_Planet = planet;
        m_PlateVertices = new List<int>();
        m_PlateTriangles = new List<int>();
        m_BorderTriangles = new List<int>();
        m_TerrainAnchors = new List<int>();
    }
}
public class Crust
{

}
