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

    public string Info()
    {
        string retVal = "";
        retVal += "Triangle " + ((uint)GetHashCode()).ToString() + " info:\n";
        retVal += "Vertex collection hash: " + ((uint)m_VertexReference.GetHashCode()).ToString() + "\n";
        //retVal += "Triangle collection hash: " + ((uint)m_TriangleReference.GetHashCode()).ToString() + "\n";
        retVal += "Vertex indices: " + m_A + ", " + m_B + ", " + m_C + "\n";
        retVal += "Vertex coordinates: (" + m_VertexReference[m_A].x + "; " + m_VertexReference[m_A].y + "; " + m_VertexReference[m_A].z + "), ";
        retVal += "(" + m_VertexReference[m_B].x + "; " + m_VertexReference[m_B].y + "; " + m_VertexReference[m_B].z + "), ";
        retVal += "(" + m_VertexReference[m_C].x + "; " + m_VertexReference[m_C].y + "; " + m_VertexReference[m_C].z + ")\n";
        retVal += "Triangle circumcenter: (" + m_CCenter.x + "; " + m_CCenter.y + "; " + m_CCenter.z + ")\n";
        retVal += "Triangle circumradius: " + m_CUnitRadius + "\n";
        retVal += "Triangle barycenter: (" + m_BCenter.x + "; " + m_BCenter.y + "; " + m_BCenter.z + ")\n";
        return retVal;
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


    public static BoundingVolume MergeBV(BoundingVolume a, BoundingVolume b)
    {
        Vector3 c1 = a.m_Circumcenter;
        Vector3 c2 = b.m_Circumcenter;
        Vector3 c3;
        float r1 = a.m_Circumradius;
        float r2 = b.m_Circumradius;
        float r3;

        if (c1 == c2) // trivial - both centers are the same
        {
            c3 = c1;
            r3 = Mathf.Max(r1, r2);
        }
        else
        {
            Vector3 aux_basvec;
            if (c1 == -c2) // both centers are opposite
            {
                if (c1.x == 0f)
                {
                    aux_basvec = new Vector3(0f, c1.z, -c1.y).normalized;
                }
                else if (c1.y == 0f)
                {
                    aux_basvec = new Vector3(c1.z, 0f, -c1.x).normalized;
                }
                else
                {
                    aux_basvec = new Vector3(c1.y, -c1.x, 0f).normalized;
                }
            }
            else
            {
                aux_basvec = Vector3.Cross(Vector3.Cross(c1, c2), c1).normalized;
            }

            bool invert_left_interval, invert_right_interval;
            float cos_dist = Vector3.Dot(c1, c2);
            float distance = (cos_dist >= 1.0f ? 0.001f : Mathf.Acos(cos_dist)); // precision upper limit
            invert_left_interval = (-r1 < distance - r2 ? false : true);
            invert_right_interval = (r1 < distance + r2 ? false : true);

            float delta_phi;

            if (!invert_left_interval && !invert_right_interval)
            {
                delta_phi = (distance - r1 + r2) / 2.0f;
                r3 = (r1 + r2 + distance) / 2.0f;
            }
            else if (!invert_left_interval && invert_right_interval)
            {
                delta_phi = 0;
                r3 = r1;
            }
            else if (invert_left_interval && !invert_right_interval)
            {
                delta_phi = distance;
                r3 = r2;
            }
            else
            {
                delta_phi = (distance + r1 - r2) / 2.0f;
                r3 = (r1 + r2 + distance) / 2.0f;
                /*
                Debug.LogError("Unrecognized circle merging");
                Debug.LogError("c1:");
                Debug.LogError("x:" + c1.x);
                Debug.LogError("y:" + c1.y);
                Debug.LogError("z:" + c1.z);
                Debug.LogError("radius: " + r1);
                Debug.LogError("c2:");
                Debug.LogError("x:" + c2.x);
                Debug.LogError("y:" + c2.y);
                Debug.LogError("z:" + c2.z);
                Debug.LogError("radius: " + r2);
                Debug.LogError("Result: " + (Mathf.Cos(delta_phi) * c1 + Mathf.Sin(delta_phi) * aux_basvec) + " with radius " + r3);
                */
            }
            c3 = Mathf.Cos(delta_phi) * c1 + Mathf.Sin(delta_phi) * aux_basvec;
        }
        BoundingVolume retVal = new BoundingVolume(c3, r3);
        retVal.m_Children.Add(a);
        retVal.m_Children.Add(b);
        a.m_Parent = retVal;
        b.m_Parent = retVal;

        if (float.IsNaN(retVal.m_Circumcenter.x))
        {
            Debug.LogError("Failed merging");
            Debug.LogError("c1: (" + c1.x + "; " + c1.y + "; " + c1.z + "), radius " + r1);
            Debug.LogError("c2: (" + c2.x + "; " + c2.y + "; " + c2.z + "), radius " + r2);


            if (c1 == c2) // trivial - both centers are the same
            {
                Debug.Log("Centers are the same.");
                c3 = c1;
                r3 = Mathf.Max(r1, r2);
            }
            else
            {
                Vector3 aux_basvec;
                if (c1 == -c2) // both centers are opposite
                {
                    Debug.Log("Centers are opposite.");
                    if (c1.x == 0f)
                    {
                        aux_basvec = new Vector3(0f, c1.z, -c1.y).normalized;
                    }
                    else if (c1.y == 0f)
                    {
                        aux_basvec = new Vector3(c1.z, 0f, -c1.x).normalized;
                    }
                    else
                    {
                        aux_basvec = new Vector3(c1.y, -c1.x, 0f).normalized;
                    }
                    Debug.Log("Aux base: (" + aux_basvec.x + "; " + aux_basvec.y + "; " + aux_basvec.z + ")");

                }
                else
                {
                    aux_basvec = Vector3.Cross(Vector3.Cross(c1, c2), c1).normalized;
                    Debug.Log("Aux base: (" + aux_basvec.x + "; " + aux_basvec.y + "; " + aux_basvec.z + ")");
                }

                bool invert_left_interval, invert_right_interval;
                float cos_dist = Vector3.Dot(c1, c2);
                float distance = (cos_dist >= 1.0f ? 0.001f : Mathf.Acos(cos_dist)); // precision upper limit
                Debug.Log("Calculated distance: " + distance);

                invert_left_interval = (-r1 < distance - r2 ? false : true);
                invert_right_interval = (r1 < distance + r2 ? false : true);

                float delta_phi;

                if (!invert_left_interval && !invert_right_interval)
                {
                    delta_phi = (distance - r1 + r2) / 2.0f;
                    r3 = (r1 + r2 + distance) / 2.0f;
                }
                else if (!invert_left_interval && invert_right_interval)
                {
                    delta_phi = 0;
                    r3 = r1;
                }
                else if (invert_left_interval && !invert_right_interval)
                {
                    delta_phi = distance;
                    r3 = r2;
                }
                else
                {
                    delta_phi = (distance + r1 - r2) / 2.0f;
                    r3 = (r1 + r2 + distance) / 2.0f;
                    /*
                    Debug.LogError("Unrecognized circle merging");
                    Debug.LogError("c1:");
                    Debug.LogError("x:" + c1.x);
                    Debug.LogError("y:" + c1.y);
                    Debug.LogError("z:" + c1.z);
                    Debug.LogError("radius: " + r1);
                    Debug.LogError("c2:");
                    Debug.LogError("x:" + c2.x);
                    Debug.LogError("y:" + c2.y);
                    Debug.LogError("z:" + c2.z);
                    Debug.LogError("radius: " + r2);
                    Debug.LogError("Result: " + (Mathf.Cos(delta_phi) * c1 + Mathf.Sin(delta_phi) * aux_basvec) + " with radius " + r3);
                    */
                }
                c3 = Mathf.Cos(delta_phi) * c1 + Mathf.Sin(delta_phi) * aux_basvec;
            }
            BoundingVolume testVal = new BoundingVolume(c3, r3);
            testVal.m_Children.Add(a);
            testVal.m_Children.Add(b);
            a.m_Parent = testVal;
            b.m_Parent = testVal;



            Debug.LogError("Result: " + testVal.m_Circumcenter + " with radius " + r3);

        }
        /*
        Debug.LogError("Unrecognized circle merging");
        Debug.LogError("c1:");
        Debug.LogError("x:" + c1.x);
        Debug.LogError("y:" + c1.y);
        Debug.LogError("z:" + c1.z);
        Debug.LogError("radius: " + r1);
        Debug.LogError("c2:");
        Debug.LogError("x:" + c2.x);
        Debug.LogError("y:" + c2.y);
        Debug.LogError("z:" + c2.z);
        Debug.LogError("radius: " + r2);
        Debug.LogError("Result: " + (Mathf.Cos(delta_phi) * c1 + Mathf.Sin(delta_phi) * aux_basvec) + " with radius " + r3);
        */




        return retVal;
    }

    public List<int> SearchForPoint(Vector3 needle, List<DRTriangle> hay)
    {
        List<int> retVal = new List<int>();
        List<BoundingVolume> list_in = new List<BoundingVolume>();
        List<BoundingVolume> list_out;
        list_in.Add(this);
        int depth_searched = 0;
        while (list_in.Count > 0)
        {
            depth_searched++;
            list_out = new List<BoundingVolume>();
            foreach (BoundingVolume it in list_in)
            {
                if (it.m_Children.Count > 0)
                {
                    foreach (BoundingVolume it2 in it.m_Children)
                    {
                        float dot_prod = Vector3.Dot(needle, it2.m_Circumcenter);
                        float multiplier = (it2.m_Circumradius < 0.005 ? 1.1f : 1.01f);
                        if (Mathf.Acos(dot_prod > 1 ? 1.0f : dot_prod) <= it2.m_Circumradius* multiplier)
                        {
                            list_out.Add(it2);
                        }
                    }
                }
                else
                {
                    if (hay[it.m_TriangleIndex].Contains(needle))
                    {
                        retVal.Add(it.m_TriangleIndex);
                    }
                }
            }
            list_in = list_out;
        }
        return retVal;

    }



}
