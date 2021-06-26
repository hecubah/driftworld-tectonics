using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class TopoMeshInterpreter // class with a single job to read and interpret generated mesh binary files
{
    // a single binary file must be formatted in the following manner:
    // first 4 byte int is the number of vertices -> V_n
    // V_n groups of 3*8 byte values (double) follow with the specific XYZ coordinates - the interpreter switches Y for Z because of Unity's coordinate system
    // 4 byte int for the number of triangles -> T_n
    // T_n 3*4 byte values (int) with the vertex indices for each triangle - matched to the read array of vertices
    // V_n groups of vertex neighbours, each starts with 4 byte int for the number of neighbours -> Vng_n, then Vng_n 4 byte int values for neighbours indices in the vertex array
    // T_n groups of triangle neighbours, each has three 4 byte int values as neighbours indices in the triangle array (assume correct spherical topology)
    // NO CHECKS!
    static public void ReadMesh(out List<Vector3> vertices_p, out List<DRTriangle> triangles_p, out List<List<int>> vertices_neighbours_p, out List<List<int>> triangles_of_vertices_p, string filename) 
    {
        List<Vector3> vertices = new List<Vector3>();
        List<DRTriangle> triangles = new List<DRTriangle>();
        List<List<int>> vertices_neighbours = new List<List<int>>();
        List<List<int>> triangles_of_vertices = new List<List<int>>();
        List<int> vertex_neighbours;
        string file_path = Application.dataPath + @"\Data/" + filename;
        if (!File.Exists(file_path))
        {
            Debug.LogError("file " + file_path + " does not exist");
        } else
        {
            FileStream fs = new FileStream(file_path, FileMode.Open);
            byte[] int_read = new byte[4];
            int n_vertices, n_triangles, n_vertex_neighbours, vertex_neighbour_index;
            fs.Read(int_read, 0, 4);
            n_vertices = BitConverter.ToInt32(int_read, 0);
            byte[] vectorx_read = new byte[8];
            byte[] vectory_read = new byte[8];
            byte[] vectorz_read = new byte[8];
            for (int i = 0; i < n_vertices; i++)
            {
                fs.Read(vectorx_read, 0, 8);
                fs.Read(vectory_read, 0, 8);
                fs.Read(vectorz_read, 0, 8);
                Vector3 new_vector = Vector3.zero;
                new_vector.x = (float)BitConverter.ToDouble(vectorx_read, 0);
                new_vector.z = (float)BitConverter.ToDouble(vectory_read, 0);
                new_vector.y = (float)BitConverter.ToDouble(vectorz_read, 0);
                vertices.Add(new_vector.normalized);
                triangles_of_vertices.Add(new List<int>());
            }

            fs.Read(int_read, 0, 4);
            n_triangles = BitConverter.ToInt32(int_read, 0);
            byte[] trianglea_read = new byte[4];
            byte[] triangleb_read = new byte[4];
            byte[] trianglec_read = new byte[4];
            int a, b, c;
            for (int i = 0; i < n_triangles; i++)
            {
                fs.Read(trianglea_read, 0, 4);
                fs.Read(triangleb_read, 0, 4);
                fs.Read(trianglec_read, 0, 4);
                a = BitConverter.ToInt32(trianglea_read, 0);
                b = BitConverter.ToInt32(triangleb_read, 0);
                c = BitConverter.ToInt32(trianglec_read, 0);
                DRTriangle new_triangle = new DRTriangle(a, b, c, vertices);
                triangles.Add(new_triangle);
            }
            byte[] n_vertex_neighbours_read = new byte[4];
            byte[] neighbour_read = new byte[4];
            for (int i = 0; i < n_vertices; i++)
            {
                vertex_neighbours = new List<int>();

                fs.Read(n_vertex_neighbours_read, 0, 4);
                n_vertex_neighbours = BitConverter.ToInt32(n_vertex_neighbours_read, 0);
                vertex_neighbours.Add(n_vertex_neighbours);
                for (int j = 0; j < n_vertex_neighbours; j++)
                {
                    fs.Read(neighbour_read, 0, 4);
                    vertex_neighbour_index = BitConverter.ToInt32(neighbour_read, 0);
                    vertex_neighbours.Add(vertex_neighbour_index);
                }
                vertices_neighbours.Add(vertex_neighbours);
            }
            for (int i = 0; i < n_triangles; i++)
            {
                fs.Read(trianglea_read, 0, 4);
                fs.Read(triangleb_read, 0, 4);
                fs.Read(trianglec_read, 0, 4);
                a = BitConverter.ToInt32(trianglea_read, 0);
                b = BitConverter.ToInt32(triangleb_read, 0);
                c = BitConverter.ToInt32(trianglec_read, 0);
                triangles[i].m_Neighbours.Add(a);
                triangles[i].m_Neighbours.Add(b);
                triangles[i].m_Neighbours.Add(c);
                triangles_of_vertices[triangles[i].m_A].Add(i);
                triangles_of_vertices[triangles[i].m_B].Add(i);
                triangles_of_vertices[triangles[i].m_C].Add(i);
            }


            fs.Close();
        }
        vertices_p = vertices;
        triangles_p = triangles;
        vertices_neighbours_p = vertices_neighbours;
        triangles_of_vertices_p = triangles_of_vertices;
    }
}
