using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DRVector
{
}

public class DRTriangle
{
    public int m_A, m_B, m_C;
    public float m_CUnitRadius;

    public Vector3 m_BCenter;
    public Vector3 m_CCenter;

    public List<Vector3> m_VertexReference;
    public List<DRTriangle> m_TriangleReference;
    public List<int> m_Neighbours;
    public HashSet<int> m_VerticesSet;

    public BoundingVolume m_BVolume = null;

    public DRTriangle(int a, int b, int c, List<Vector3> reference)
    {
        m_A = a;
        m_B = b;
        m_C = c;
        m_VertexReference = reference;
        m_Neighbours = new List<int>();
        m_VerticesSet = new HashSet<int> {a, b, c};
        UpdateData();
    }

    public DRTriangle(DRTriangle source, List<Vector3> reference)
    {
        m_A = source.m_A;
        m_B = source.m_B;
        m_C = source.m_C;
        m_VertexReference = reference;
        m_Neighbours = new List<int>();
        m_VerticesSet = new HashSet<int> { m_A, m_B, m_C };
        UpdateData();
    }

    static float UnitSphereDistance(Vector3 a, Vector3 b)
    {
        return Mathf.Acos(Vector3.Dot(a, b));
    }

    void UpdateBCenter()
    {
        m_BCenter = (m_VertexReference[m_A] + m_VertexReference[m_B] + m_VertexReference[m_C]).normalized;
    }

    void UpdateCCenter()
    {
        Vector3 center_ray = Vector3.Cross(m_VertexReference[m_B] - m_VertexReference[m_A], m_VertexReference[m_C] - m_VertexReference[m_A]);
        center_ray = (Vector3.Dot(center_ray, m_VertexReference[m_A]) < 0 ? -center_ray: center_ray);
        m_CCenter = center_ray.normalized;
        m_CUnitRadius = UnitSphereDistance(m_VertexReference[m_A], m_CCenter);

    }

    public void UpdateData()
    {
        UpdateBCenter();
        UpdateCCenter();
    }

    public bool Contains(Vector3 refugee)
    {

        Vector3 x = Vector3.Cross(m_VertexReference[m_A], m_VertexReference[m_B]);
        Vector3 y = Vector3.Cross(m_VertexReference[m_B], m_VertexReference[m_C]);
        Vector3 z = Vector3.Cross(m_VertexReference[m_C], m_VertexReference[m_A]);
        double xR, yR, zR;
        xR = x.x * refugee.x + x.y * refugee.y + x.z * refugee.z;
        yR = y.x * refugee.x + y.y * refugee.y + y.z * refugee.z;
        zR = z.x * refugee.x + z.y * refugee.y + z.z * refugee.z;
        if ((xR * yR >= 0) && (yR * zR >= 0) && (zR * xR >= 0) && (Vector3.Dot(m_BCenter, refugee) > 0))
            return true;
        else
            return false;


    }

    public bool IsClockwise()
    {
        return (!(Vector3.Dot(Vector3.Cross(m_VertexReference[m_C] - m_VertexReference[m_A], m_VertexReference[m_A] - m_VertexReference[m_B]), m_BCenter) < 0));
    }

    public void EnsureClockwiseOrientation()
    {
        if (Vector3.Dot(Vector3.Cross(m_VertexReference[m_C]- m_VertexReference[m_A], m_VertexReference[m_A] - m_VertexReference[m_B]),m_BCenter) < 0)
        {
            int x = m_B;
            m_B = m_C;
            m_C = x;
        }
    }

    static public bool Collision (DRTriangle a, DRTriangle b)
    {
        if ((a.Contains(b.m_VertexReference[b.m_A])) || (a.Contains(b.m_VertexReference[b.m_B])) || (a.Contains(b.m_VertexReference[b.m_C])) || (b.Contains(a.m_VertexReference[a.m_A])) || (b.Contains(a.m_VertexReference[a.m_B])) || (b.Contains(a.m_VertexReference[a.m_C])))
        {
            return true;
        } else
        {
            Vector3 a1, a2, b1, b2;
            a1 = a.m_VertexReference[a.m_A];
            a2 = a.m_VertexReference[a.m_B];
            b1 = b.m_VertexReference[b.m_A];
            b2 = b.m_VertexReference[b.m_B];
            if (SidesIntersect(a1, a2, b1, b2))
            {
                return true;
            }
            a1 = a.m_VertexReference[a.m_A];
            a2 = a.m_VertexReference[a.m_B];
            b1 = b.m_VertexReference[b.m_B];
            b2 = b.m_VertexReference[b.m_C];
            if (SidesIntersect(a1, a2, b1, b2))
            {
                return true;
            }
            a1 = a.m_VertexReference[a.m_A];
            a2 = a.m_VertexReference[a.m_B];
            b1 = b.m_VertexReference[b.m_C];
            b2 = b.m_VertexReference[b.m_A];
            if (SidesIntersect(a1, a2, b1, b2))
            {
                return true;
            }
            a1 = a.m_VertexReference[a.m_B];
            a2 = a.m_VertexReference[a.m_C];
            b1 = b.m_VertexReference[b.m_B];
            b2 = b.m_VertexReference[b.m_C];
            if (SidesIntersect(a1, a2, b1, b2))
            {
                return true;
            }
            a1 = a.m_VertexReference[a.m_B];
            a2 = a.m_VertexReference[a.m_C];
            b1 = b.m_VertexReference[b.m_C];
            b2 = b.m_VertexReference[b.m_A];
            if (SidesIntersect(a1, a2, b1, b2))
            {
                return true;
            }
            a1 = a.m_VertexReference[a.m_C];
            a2 = a.m_VertexReference[a.m_A];
            b1 = b.m_VertexReference[b.m_C];
            b2 = b.m_VertexReference[b.m_A];
            if (SidesIntersect(a1, a2, b1, b2))
            {
                return true;
            }
            return false;
        }
    }

    static public bool SidesIntersect (Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2)
    {
        Vector3 intersection = Vector3.Cross(Vector3.Cross(a1, a2), Vector3.Cross(b1, b2));
        if (intersection.magnitude > 0)
        {
            intersection = intersection.normalized;
        }
        else
        {
            return true;
        }
        if (Vector3.Dot(a1, -intersection) > Vector3.Dot(a1, intersection))
        {
            intersection = -intersection;
        }
        if ((Vector3.Dot(a1, intersection) >= Vector3.Dot(a1, a2)) && (Vector3.Dot(a2, intersection) >= Vector3.Dot(a1, a2)) && (Vector3.Dot(b1, intersection) >= Vector3.Dot(b1, b2)) && (Vector3.Dot(b2, intersection) >= Vector3.Dot(b1, b2)))
        {
            return true;
        } else
        {
            return false;
        }

    }

}

public class BoundingVolume
{
    public List<BoundingVolume> m_Children; // all bounding box subboxes
    public int m_TriangleIndex; // leaf bounding box triangle
    public BoundingVolume m_Root = null; // bounding box tree root
    public BoundingVolume m_Parent = null; // higher bounding box tree node
    public Vector3 m_Circumcenter; // base circumcenter of the box
    public float m_Circumradius; // range of the box from its center
    public Quaternion m_GeneralTransform = Quaternion.identity; // root box transform to update on-the-fly

    public BoundingVolume (Vector3 circumcenter, float circumradius)
    {
        m_Circumcenter = circumcenter; // needs to be known before constructor call
        m_Circumradius = circumradius; // needs to be known before constructor call
        m_Children = new List<BoundingVolume>(); // children are added while constructing the tree or performing operations
        m_TriangleIndex = -1; // needs to be added when constructing the base of the tree
    }

    public uint MortonCode() // assign an unsigned integer to the circumcenter point on the sphere
    {
        float atanval = Mathf.Atan2(m_Circumcenter.z, m_Circumcenter.x);
        uint phi_norm = (uint)((atanval >= 0 ? atanval : atanval + 2 * Mathf.PI) / (2 * Mathf.PI) * 65535); // normalize to max_uint16
        uint theta_norm = (uint)(Mathf.Acos(m_Circumcenter.y) / Mathf.PI * 65535); // normalize to max_uint16
        uint code = 0; // fresh code
        for (int i = 0; i < 16; i++) // for all bits in normalized int phi coordinate
        {
            code += ((phi_norm >> i) % 2) << (2 * i); // fill the second bits in couples
        }
        for (int i = 0; i < 16; i++) // for all bits in normalized int theta coordinate
        {
            code += ((theta_norm >> i) % 2) << (2 * i + 1); // fill the first bits in couples
        }
        return code; // return ready code
    }

    public static List<int> MCodeRadixSort(List<BoundingVolume> volume_list) // compute Morton codes of a list of a single level of bounding volumes and return the sort index list
    {
        int list_count = volume_list.Count; // number of bounding volume objects
        List<uint> morton_code_list = new List<uint>(); // corresponding Morton codes list
        Queue<int> wip_list = new Queue<int>(); // sorted sequence queue
        for (int i = 0; i < list_count; i++) // for all bounding volume objects
        {
            morton_code_list.Add(volume_list[i].MortonCode()); // add the corresponding Morton code to the list
            wip_list.Enqueue(i); // enqueue the identity index
        }
        for (int i = 0; i < 32; i++) // for all bits in the Morton code
        {
            Queue<int> zeroes = new Queue<int>(); // bucket queue of zero bits
            Queue<int> ones = new Queue<int>(); // bucket queue of one bits
            for (int j = 0; j < list_count; j++) // for all items in the index queue
            {
                int candidate = wip_list.Dequeue(); // get next index
                if ((morton_code_list[candidate] >> i) % 2 == 0) // if the Morton code bit is zero
                {
                    zeroes.Enqueue(candidate); // add it to the zero bit bucket
                }
                else
                {
                    ones.Enqueue(candidate); // add it to the one bit bucket
                }
            }
            int zeroes_size = zeroes.Count; // number of Morton codes with zero respective bit
            int ones_size = ones.Count; // number of Morton codes with one respective bit
            for (int j = 0; j < zeroes_size; j++) // for all codes in the zero bucket
            {
                wip_list.Enqueue(zeroes.Dequeue()); // add the zero bucket to the new sequence
            }
            for (int j = 0; j < ones_size; j++) // for all codes in the one bucket
            {
                wip_list.Enqueue(ones.Dequeue()); // add the one bucket to the new sequence
            }
        }
        List<int> retVal = new List<int>(); // new return value list of indexes
        for (int i = 0; i < list_count; i++) // for all items in the final sequence
        {
            retVal.Add(wip_list.Dequeue()); // add the final index to the list
        }
        return retVal; // return the list of indexes
    }

    public static BoundingVolume ConstructBVH(List<BoundingVolume> volume_list)
    {
        List<int> order_list = BoundingVolume.MCodeRadixSort(volume_list);

        return null;
    }



}
