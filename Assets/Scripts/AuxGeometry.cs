using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main triangle representation. Keeps indices to vertices in the respective planet layer, centroid and circumcenter, unit circumradius, reference to the vertex locations, neighbouring triangle indices and, if applicable, corresponding bounding volume reference.
/// </summary>
public class DRTriangle
{
    public int m_A, m_B, m_C; // vertex indices, relative to the reference
    public float m_CUnitRadius; // unit sphere circumradius

    public Vector3 m_BCenter; // triangle centroid, normalize to unit sphere
    public Vector3 m_CCenter; // triangle circumcenter, normalize to unit sphere

    public List<Vector3> m_VertexReference; // vertex position list reference
    public List<int> m_Neighbours; // neighbour triangle indices - should always be three of them

    public BoundingVolume m_BVolume = null; // bounding volume reference

    /// <summary>
    /// Constructor using vertex indices, together with a vertex location reference.
    /// </summary>
    /// <param name="a">first vertex index</param>
    /// <param name="b">second vertex index</param>
    /// <param name="c">third vertex index</param>
    /// <param name="reference">reference list of vertex locations</param>
    public DRTriangle(int a, int b, int c, List<Vector3> reference)
    {
        m_A = a;
        m_B = b;
        m_C = c;
        m_VertexReference = reference;
        m_Neighbours = new List<int>(); // neighbours must be added later
        UpdateData(); // construct circumcircle, circumradius and centroid
    }

    /// <summary>
    /// Copy constructor, requires vertex location reference.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="reference"></param>
    public DRTriangle(DRTriangle source, List<Vector3> reference)
    {
        m_A = source.m_A;
        m_B = source.m_B;
        m_C = source.m_C;
        m_VertexReference = reference;
        m_Neighbours = new List<int>();
        foreach (int it in source.m_Neighbours) // copy source neighbours
        {
            m_Neighbours.Add(it);
        }
        UpdateData();
    }

    /// <summary>
    /// Distance on a unit sphere, corrected for rounding error at Acos (cases of Cos > 1).
    /// </summary>
    /// <param name="a">first vertex position</param>
    /// <param name="b">second vertex position</param>
    /// <returns>float distance on a unit sphere</returns>
    static float UnitSphereDistance(Vector3 a, Vector3 b)
    {
        return Mathf.Acos(Mathf.Min(Vector3.Dot(a, b), 1.0f));
    }

    /// <summary>
    /// Recalculate centroid, normalized to unit sphere.
    /// </summary>
    void UpdateBCenter()
    {
        m_BCenter = (m_VertexReference[m_A] + m_VertexReference[m_B] + m_VertexReference[m_C]).normalized;
    }

    /// <summary>
    /// Recalculate circumradius and circumcenter, normalized to unit sphere.
    /// </summary>
    void UpdateCCenter()
    {
        Vector3 center_ray = Vector3.Cross(m_VertexReference[m_B] - m_VertexReference[m_A], m_VertexReference[m_C] - m_VertexReference[m_A]);
        center_ray = (Vector3.Dot(center_ray, m_VertexReference[m_A]) < 0 ? -center_ray: center_ray);
        m_CCenter = center_ray.normalized;
        m_CUnitRadius = UnitSphereDistance(m_VertexReference[m_A], m_CCenter);

    }

    /// <summary>
    /// Recalculate all internal induced variables.
    /// </summary>
    public void UpdateData()
    {
        UpdateBCenter();
        UpdateCCenter();
    }

    /// <summary>
    /// Check whether a point is inside a spherical triangle on a unit sphere.
    /// </summary>
    /// <param name="refugee">a point on a sphere, expected to be normalized to 1</param>
    /// <returns>true if the point is inside the triangle (edges included), false otherwise</returns>
    public bool Contains(Vector3 refugee)
    {

        Vector3 x = Vector3.Cross(m_VertexReference[m_A], m_VertexReference[m_B]); // cross product defines a normal vector to a plane the edge is part of (constant is equal to 0)
        Vector3 y = Vector3.Cross(m_VertexReference[m_B], m_VertexReference[m_C]);
        Vector3 z = Vector3.Cross(m_VertexReference[m_C], m_VertexReference[m_A]);
        double xR, yR, zR;
        xR = x.x * refugee.x + x.y * refugee.y + x.z * refugee.z; // substitute the point into the halfspace inequality
        yR = y.x * refugee.x + y.y * refugee.y + y.z * refugee.z;
        zR = z.x * refugee.x + z.y * refugee.y + z.z * refugee.z;
        if ((xR * yR >= 0) && (yR * zR >= 0) && (zR * xR >= 0) && (Vector3.Dot(m_BCenter, refugee) > 0)) // if the inequalities hold (expected clockwise triangle orientation) - dot product check is because the three vertices actually define two triangles
            return true;
        else
            return false;
    }

    /// <summary>
    /// For every triangle, ensure the vertices are ordered clockwise. Only clockwise oriented triangles are properly rendered (normal pointing outside the sphere).
    /// </summary>
    public void EnsureClockwiseOrientation()
    {
        if (Vector3.Dot(Vector3.Cross(m_VertexReference[m_C]- m_VertexReference[m_A], m_VertexReference[m_A] - m_VertexReference[m_B]),m_BCenter) < 0) // if not clockwise, switch second and third index
        {
            int x = m_B;
            m_B = m_C;
            m_C = x;
        }
    }
}

/// <summary>
/// Encompassing bounding volume, realized as a circle on the unit sphere surface. Bounding volume hiearchy is implemented as a binary tree.
/// </summary>
public class BoundingVolume
{
    public List<BoundingVolume> m_Children; // subvolumes, if any
    public int m_TriangleIndex; // leaf bounding volume triangle
    public Vector3 m_Circumcenter; // base circumcenter of the volume
    public float m_Circumradius; // volume cirmuradius

    public BoundingVolume (Vector3 circumcenter, float circumradius)
    {
        m_Circumcenter = circumcenter;
        m_Circumradius = circumradius;
        m_Children = new List<BoundingVolume>(); // children are added while constructing the tree
        m_TriangleIndex = -1; // a leaf needs to be assigned an existing non-negative triangle index
    }

    /// <summary>
    /// Calculate an unsigned integer encoding the circumcenter of the volume. Quantized values are the azimuthal angle and polar angle (azimuthal zero is in the direction of the x axis).
    /// </summary>
    /// <returns>quantized point coordinates</returns>
    public uint MortonCode()
    {
        float atanval = Mathf.Atan2(m_Circumcenter.z, m_Circumcenter.x); // azimuthal angle
        uint phi_norm = (uint)((atanval >= 0 ? atanval : atanval + 2 * Mathf.PI) / (2 * Mathf.PI) * 65535); // normalize to max_uint16 - 16 bit values of both angles are interlaced into a single 32 bit uint
        uint theta_norm = (uint)(Mathf.Acos(m_Circumcenter.y) / Mathf.PI * 65535); // normalize to max_uint16
        uint code = 0; // value initialization
        for (int i = 0; i < 16; i++) // for all bits in normalized int phi coordinate
        {
            code += ((phi_norm >> i) % 2) << (2 * i); // fill the second bits in couples
        }
        for (int i = 0; i < 16; i++) // for all bits in normalized int theta coordinate
        {
            code += ((theta_norm >> i) % 2) << (2 * i + 1); // fill the first bits in couples
        }
        return code;
    }

    /// <summary>
    /// Sorts given bounding volume set by the corresponding morton codes of its elements. Uses binary bucket sort, used for leveled construction of a bounding volume hiearchy from the bottom up (starting with leaves).
    /// </summary>
    /// <param name="volume_list">a list of bounding volumes</param>
    /// <returns>list of sorted indices relating to the given list</returns>
    public static List<int> MCodeRadixSort(List<BoundingVolume> volume_list)
    {
        int list_count = volume_list.Count; // number of bounding volume objects
        List<uint> morton_code_list = new List<uint>(); // corresponding Morton codes list
        Queue<int> wip_list = new Queue<int>(); // sorted sequence queue
        for (int i = 0; i < list_count; i++) // for all bounding volume objects
        {
            morton_code_list.Add(volume_list[i].MortonCode()); // add the corresponding Morton code to the list
            wip_list.Enqueue(i); // enqueue the identity index
        }
        for (int i = 0; i < 32; i++) // for all bits in the Morton code, starting with the least significant bit
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
        List<int> retVal = new List<int>(); // new return value list of indices
        for (int i = 0; i < list_count; i++) // for all items in the final sequence
        {
            retVal.Add(wip_list.Dequeue()); // add the final index to the list
        }
        return retVal;
    }

    /// <summary>
    /// Merge two bounding volumes as efficiently as possible. Rounding errors notwithstanding, both bounding volumes circumcircles should just touch the merged circumcircle to prune BVH efficiently.
    /// </summary>
    /// <param name="a">first bounding volume</param>
    /// <param name="b">second bounding volume</param>
    /// <returns></returns>
    public static BoundingVolume MergeBV(BoundingVolume a, BoundingVolume b)
    {
        Vector3 c1 = a.m_Circumcenter;
        Vector3 c2 = b.m_Circumcenter;
        Vector3 c3; // variable for the merged cirumcenter
        float r1 = a.m_Circumradius;
        float r2 = b.m_Circumradius;
        float r3; // variable for the merged circumradius

        if (c1 == c2) // trivial - both centers are the same
        {
            c3 = c1; // either is good
            r3 = Mathf.Max(r1, r2); // consider the larger
        }
        else
        {
            Vector3 aux_basvec; // left-hand base vector of the plane in which c1 and c2 lie - base is {c1, aux_basvec}
            if (c1 == -c2) // both centers are opposite - since a cross product is used in other cases, account for the cases where cross product would be a zero vector
            {
                if (c1.x == 0f) // account for possible zero x coordinate of the first circumcenter vector while looking for a perpendicular vector
                {
                    aux_basvec = new Vector3(0f, c1.z, -c1.y).normalized; // find a perpendicular vector to both cirumcenters
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
                aux_basvec = Vector3.Cross(Vector3.Cross(c1, c2), c1).normalized; // find the base vector by using double cross product
            }

            bool circle_1_encompassed, circle_2_encompassed; // variables to deal with cases where the one of the merging circles contains the other one
            float cos_dist = Vector3.Dot(c1, c2); // distance cos between the circumcenters of the merging circles
            float distance = (cos_dist >= 1.0f ? 0.001f : Mathf.Acos(cos_dist)); // for volumes that seem too close, overestimate the distance - the dimension is angular measure respective to unit sphere
            circle_1_encompassed = -r1 > distance - r2; // second circle encompasses the first circle
            circle_2_encompassed = r1 > distance + r2; // first circle encompasses the second circle

            float delta_phi; // angle from c1 to the circumcenter of the merged volume circumcenter (towards aux_basvec)

            if (!circle_1_encompassed && !circle_2_encompassed) // no encompassing
            {
                delta_phi = (distance - r1 + r2) * 0.5f;
                r3 = (r1 + r2 + distance) * 0.5f;
            }
            else if (!circle_1_encompassed && circle_2_encompassed) // circle 2 encompassed
            {
                delta_phi = 0;
                r3 = r1;
            }
            else if (circle_1_encompassed && !circle_2_encompassed) // circle 1 encompassed - merged volume is simply circle 2
            {
                delta_phi = distance;
                r3 = r2;
            }
            else // OCD clause, should never happen
            {
                delta_phi = (distance + r1 - r2) * 0.5f;
                r3 = (r1 + r2 + distance) * 0.5f;
                Debug.LogError("Impossible merging, negative circumcenters distance!");
            }
            c3 = Mathf.Cos(delta_phi) * c1 + Mathf.Sin(delta_phi) * aux_basvec;
        }
        BoundingVolume retVal = new BoundingVolume(c3, r3); // create parent 
        retVal.m_Children.Add(a); // add first merged volume to the parent's children list
        retVal.m_Children.Add(b); // add second merged volume to the parent's children list
        if (float.IsNaN(retVal.m_Circumcenter.x)) // if the merging results in a NaN circumcenter x coordinate
        {
            Debug.LogError("Failed merging, repeating merge in verbose mode."); // message, declaring verbose repeating
            Debug.LogError("c1: (" + c1.x + "; " + c1.y + "; " + c1.z + "), radius " + r1);
            Debug.LogError("c2: (" + c2.x + "; " + c2.y + "; " + c2.z + "), radius " + r2);


            if (c1 == c2)
            {
                Debug.Log("Centers are the same.");
                c3 = c1;
                r3 = Mathf.Max(r1, r2);
            }
            else
            {
                Vector3 aux_basvec;
                if (c1 == -c2)
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

                bool circle_1_encompassed, circle_2_encompassed;
                float cos_dist = Vector3.Dot(c1, c2);
                float distance = (cos_dist >= 1.0f ? 0.001f : Mathf.Acos(cos_dist));
                Debug.Log("Calculated distance: " + distance);

                circle_1_encompassed = -r1 > distance - r2;
                circle_2_encompassed = r1 > distance + r2;

                float delta_phi;

                if (!circle_1_encompassed && !circle_2_encompassed)
                {
                    delta_phi = (distance - r1 + r2) * 0.5f;
                    r3 = (r1 + r2 + distance) * 0.5f;
                }
                else if (!circle_1_encompassed && circle_2_encompassed)
                {
                    delta_phi = 0;
                    r3 = r1;
                }
                else if (circle_1_encompassed && !circle_2_encompassed)
                {
                    delta_phi = distance;
                    r3 = r2;
                }
                else
                {
                    delta_phi = (distance + r1 - r2) * 0.5f;
                    r3 = (r1 + r2 + distance) * 0.5f;
                }
                c3 = Mathf.Cos(delta_phi) * c1 + Mathf.Sin(delta_phi) * aux_basvec;
            }
            BoundingVolume testVal = new BoundingVolume(c3, r3);
            testVal.m_Children.Add(a);
            testVal.m_Children.Add(b);
            Debug.LogError("Result: " + testVal.m_Circumcenter + " with radius " + r3);
        }
        return retVal;
    }

    /// <summary>
    /// Construct a bounding volume list for easier shader buffer updates.
    /// </summary>
    /// <param name="BV_root">root element of the BVH binary tree</param>
    /// <returns>shader-friendly list of bounding volumes, indexed internally</returns>
    public static List<BoundingVolumeStruct> BuildBVHArray(BoundingVolume BV_root)
    {
        List<BoundingVolumeStruct> retVal = new List<BoundingVolumeStruct>(); // list of shader-friendly objects
        if (BV_root != null) // BVH tree root existence check
        {
            Queue<BoundingVolume> queue_feed = new Queue<BoundingVolume>(); // queue buffer for the array construction
            int border_index = 0; // indexing within the constructed array
            queue_feed.Enqueue(BV_root);
            BoundingVolume source;
            BoundingVolumeStruct fill;
            while (queue_feed.Count > 0) // BFS search and fill
            {
                source = queue_feed.Dequeue(); // take next BV
                fill = new BoundingVolumeStruct(); // initialize new BVStruct
                if (source.m_Children.Count == 2) // if the node has children, fill the variables and add children to the end of the queue
                {
                    fill.n_children = 2; // node is not a leaf
                    fill.left_child = ++border_index; // increase inner index and assign to left child
                    fill.right_child = ++border_index; // increase inner index and assign to left child
                    queue_feed.Enqueue(source.m_Children[0]); // add left child to the queue
                    queue_feed.Enqueue(source.m_Children[1]); // add right child to the queue
                    fill.triangle_index = -1; // if the node is not a leaf, no triangle assigned - should not be used
                    fill.circumcenter = source.m_Circumcenter; // fill the circumcenter
                    fill.circumradius = source.m_Circumradius; // fill the circumradius
                }
                else // if the node has no children, fill the variables and assign the corresponding triangle
                {
                    fill.n_children = 0; // node is a leaf
                    fill.left_child = -1; // no left child, should not be used
                    fill.right_child = -1; // no right child, should not be used
                    fill.triangle_index = source.m_TriangleIndex; // triangle assignment
                    fill.circumcenter = source.m_Circumcenter; // fill the circumcenter
                    fill.circumradius = source.m_Circumradius; // fill the circumradius
                }
                retVal.Add(fill); // add BVStruct to the constructed array
            }

        }
        return retVal;
    }
}
