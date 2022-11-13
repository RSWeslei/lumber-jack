using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateMesh : MonoBehaviour
{
    List<Vector3> vertices = new List<Vector3>();
    List<Vector3> normals = new List<Vector3>();
    List<Vector2> uvs = new List<Vector2>();
    List<List<int>> submeshIndices = new List<List<int>>();

    public List<Vector3> Vertices { get { return vertices; } set {vertices = value;} }
    public List<Vector3> Normals { get { return normals; } set { normals = value; } }
    public List<Vector2> UVs { get { return uvs; } set { uvs = value; } }
    public List<List<int>> SubmeshIndices { get { return submeshIndices; } set { submeshIndices = value; } }

    public void AddTriangle (MeshTriangle _triangle) {
        int currentVerticeCount = vertices.Count;

        vertices.AddRange(_triangle.Vertices);
        normals.AddRange(_triangle.Normals);
        uvs.AddRange(_triangle.UVs);

        if (submeshIndices.Count < _triangle.SubmeshIndex + 1) {
            for (int i = submeshIndices.Count; i < _triangle.SubmeshIndex + 1; i++) {
                submeshIndices.Add(new List<int>());
            }
        }

        for (int i = 0; i < 3; i++) {
            submeshIndices[_triangle.SubmeshIndex].Add(currentVerticeCount + i);
        }
    }
}
