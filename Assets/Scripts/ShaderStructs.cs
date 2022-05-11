using UnityEngine;
// This is a unit with definitions for simple objects used in the project shaders.


/// <summary>
/// Basic triangle data, shader version. Does not have vertex reference, must be fed to the shader along with an array of CS_Triangle structs.
/// </summary>
public struct CS_Triangle // size of 40 B
{
    public int A; // first vertex index
    public int B; // first vertex index
    public int C; // first vertex index
    public int neigh_1;  // first triangle neighbour index
    public int neigh_2;  // first triangle neighbour index
    public int neigh_3;  // first triangle neighbour index
    public Vector3 circumcenter; // triangle circumcenter without transform
    public float circumradius; // triangle circumradius
    public CS_Triangle(int A_p, int B_p, int C_p, int n1_p, int n2_p, int n3_p, Vector3 cc_p, float cr_p) // complete constructor
    {
        A = A_p;
        B = B_p;
        C = C_p;
        neigh_1 = n1_p;
        neigh_2 = n2_p;
        neigh_3 = n3_p;
        circumcenter = cc_p;
        circumradius = cr_p;
    }
}

/// <summary>
/// Basic vertex PointData representation, shader version. Must be accompanied by an array of vertex positions.
/// </summary>
public struct CS_VertexData // size of 16 B
{
    public float elevation; // elevation values
    public int plate; // plate the vertex belongs to
    public int orogeny; // OrogenyType
    public float age; // crust age


    public CS_VertexData(PointData source) // complete constructor
    {
        elevation = source.elevation;
        plate = source.plate;
        orogeny = (int)source.orogeny;
        age = source.age;
    }
}

/// <summary>
/// An array of these objects denotes crust border triangles that intersect with another plate.
/// </summary>
public struct CS_PlateContact // size of 28 B
{
    public int contact_occured; // bool substitute for checking whether the respective border triangle intersects another plate
    public Vector3 contact_point; // centroid of the contacting border triangle, normalized to unit sphere
    public float elevation; // average elevation of the intersected triangle vertices
    public int contacting_plate; // plate index the contacting triangles belongs to
    public int contacted_plate; // contacted plate index
    public CS_PlateContact(int contact_occured_p, Vector3 contact_point_p, float elevation_p, int contacting_plate_p, int contacted_plate_p) // complete constructor
    {
        contact_occured = contact_occured_p;
        contact_point = contact_point_p;
        elevation = elevation_p;
        contacting_plate = contacting_plate_p;
        contacted_plate = contacted_plate_p;
    }
}

/// <summary>
/// A bounding volume data object, shader version. It holds basic binary tree element data. Indexing is within the same array. Default constructor only.
/// </summary>
public struct BoundingVolumeStruct // size of 32 B
{
    public int n_children; // how many children a node has - zero denotes a leaf
    public int left_child; // left bounding volume child
    public int right_child; // right bounding volume child
    public int triangle_index; // if the node is a leaf, index of the respective elementary triangle
    public Vector3 circumcenter; // circumcenter of the bounding volume
    public float circumradius; // circumradius of the bounding volume
}