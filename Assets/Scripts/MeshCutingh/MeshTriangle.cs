using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshTriangle 
{
    List<Vector3> vertices = new List<Vector3>();
    List<Vector3> normals = new List<Vector3>();
    List<Vector2> uvs = new List<Vector2>();
    int submeshIndex;

    public List<Vector3> Vertices { get { return vertices; } set { vertices = value; } }
    public List<Vector3> Normals { get { return normals; } set { normals = value; } }
    public List<Vector2> UVs { get { return uvs; } set { uvs = value; } }
    public int SubmeshIndex { get { return submeshIndex; } set { submeshIndex = value; } }

    public MeshTriangle (List<Vector3> _vertices, List<Vector3> _normals, List<Vector2> _uvs, int _submeshIndex) {
        Clear();

        vertices = _vertices;
        normals = _normals;
        uvs = _uvs;
        submeshIndex = _submeshIndex;
    }

    public void Clear () {
        vertices.Clear();
        normals.Clear();
        uvs.Clear();

        submeshIndex = 0;
    }
}