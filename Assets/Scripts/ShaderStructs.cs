using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct CS_TriangleY // size of 72 B
{
    public Vector3 A;
    public Vector3 B;
    public Vector3 C;
    public float elevation_A;
    public float elevation_B;
    public float elevation_C;
    public int plate_A;
    public int plate_B;
    public int plate_C;
    public int neigh_1;
    public int neigh_2;
    public int neigh_3;
    public CS_TriangleY(Vector3 A_p, Vector3 B_p, Vector3 C_p, float eA_p, float eB_p, float eC_p, int pA_p, int pB_p, int pC_p, int n1_p, int n2_p, int n3_p)
    {
        A = A_p;
        B = B_p;
        C = C_p;
        elevation_A = eA_p;
        elevation_B = eB_p;
        elevation_C = eC_p;
        plate_A = pA_p;
        plate_B = pB_p;
        plate_C = pC_p;
        neigh_1 = n1_p;
        neigh_2 = n2_p;
        neigh_3 = n3_p;
    }
}

public struct CS_Triangle // size of 40 B
{
    public int A;
    public int B;
    public int C;
    public int neigh_1;
    public int neigh_2;
    public int neigh_3;
    public Vector3 circumcenter;
    public float circumradius;
    public CS_Triangle(int A_p, int B_p, int C_p, int n1_p, int n2_p, int n3_p, Vector3 cc_p, float cr_p)
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

public struct CS_VertexData // size of 16 B
{
    public float elevation;//
    public int plate;//
    public int orogeny;
    public float age;


    public CS_VertexData(PointData source)
    {
        elevation = source.elevation;
        plate = source.plate;
        orogeny = (int)source.orogeny;
        age = source.age;
    }
}

public struct CS_PlateContact // size of 28 B
{
    public int contact_occured;
    public Vector3 contact_point;
    public float elevation;
    public int contacting_plate;
    public int contacted_plate;
    public CS_PlateContact(int contact_occured_p, Vector3 contact_point_p, float elevation_p, int contacting_plate_p, int contacted_plate_p)
    {
        contact_occured = contact_occured_p;
        contact_point = contact_point_p;
        elevation = elevation_p;
        contacting_plate = contacting_plate_p;
        contacted_plate = contacted_plate_p;
    }
}

public struct BoundingVolumeStruct // size of 32 B
{
    public int n_children;
    public int left_child;
    public int right_child;
    public int triangle_index;
    public Vector3 circumcenter;
    public float circumradius;
}