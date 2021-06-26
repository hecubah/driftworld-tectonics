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

public class BoundingBox
{
    public List<BoundingBox> m_Children;
    public List<DRTriangle> m_Trinagles;
    public Vector3 m_Circumcenter;
    public float m_Circumradius;

    public BoundingBox (Vector3 circumcenter, float circumradius)
    {
        m_Circumcenter = circumcenter;
        m_Circumradius = circumradius;
        m_Children = new List<BoundingBox>();
        m_Trinagles = new List<DRTriangle>();
    }
}
