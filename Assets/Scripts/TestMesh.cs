using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class TestMesh : MonoBehaviour
{
    // Start is called before the first frame update
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    MeshCollider meshCollider;
    [SerializeField] GameObject vertexPreviewDebug;
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        CreateNewTriangle();
    }

    public void addVertex (Vector3 vertex) {
        Debug.Log(meshFilter.mesh.vertices.Length);
        // get the current mesh
        Mesh mesh = meshFilter.mesh;
        // get the current vertices
        Vector3[] vertices = mesh.vertices;
        // create a new array with the size of the current vertices + 1
        Vector3[] newVertices = new Vector3[vertices.Length + 1];
        // copy the current vertices to the new array
        vertices.CopyTo(newVertices, 0);
        // add the new vertex to the new array
        newVertices[newVertices.Length - 1] = vertex;
        // set the new vertices to the mesh
        mesh.vertices = newVertices;
        // recalculate the normals
        mesh.RecalculateNormals();
        // recalculate the bounds
        mesh.RecalculateBounds();
        // recalculate the tangents
        mesh.RecalculateTangents();
        // update the mesh
        meshFilter.mesh = mesh;
        // update the mesh collider
        meshCollider.sharedMesh = mesh;

        Debug.Log(meshFilter.mesh.vertices.Length);
    }

    public void CreateNewTriangle () 
    {
        Mesh mesh = new Mesh {
            name = "New Mesh"
        };
        Vector3[] originalTriangleVertices = new Vector3[3];
        originalTriangleVertices[0] = new Vector3(1f, 0, 0f);
        originalTriangleVertices[1] = new Vector3(0f, 0f, 1f);
        originalTriangleVertices[2] = new Vector3(0f, 0f, 0f);

        // First, we need to create a new array of vertices, which will be the new mesh vertices.
        Vector3[] newVertices = new Vector3[5];

        newVertices[3] = new Vector3(0.5f, 0f, 0f);
        newVertices[4] = new Vector3(0.5f, 0f, 0.5f);

        // Then, we need to create a new array of triangles, which will be the new mesh triangles.
        int[] newTriangles = new int[9];
        newTriangles[0] = 3;
        newTriangles[1] = 2;
        newTriangles[2] = 1;

        newTriangles[3] = 4;
        newTriangles[4] = 3;
        newTriangles[5] = 1;

        Vector3[] vertices = mesh.vertices;
        newTriangles[6] = 0;
        newTriangles[7] = 3;
        newTriangles[8] = 4;

        Mesh newMesh = new Mesh {
            vertices = newVertices,
            triangles = newTriangles
        };
        GameObject newWood = new GameObject("New Mesh");
        newWood.AddComponent<MeshFilter>();
        newWood.AddComponent<MeshRenderer>();
        newWood.AddComponent<MeshCollider>();
        newWood.GetComponent<MeshFilter>().mesh = mesh;



        mesh.vertices = newVertices;
        mesh.triangles = newTriangles;
        mesh.RecalculateNormals();
        
        meshFilter.mesh = mesh;
        meshRenderer.material.color = Color.red;
        meshCollider.sharedMesh = mesh;

        foreach (Vector3 vertex in meshFilter.mesh.vertices) {
            LogVertex(vertex);
            // Debug.Log("Position: " + vertex);
        }
    }

    private void LogVertex(Vector3 point) {
        // instanciate a debug object at the point of gameobject using local position of this gameobject
        GameObject debugObject = Instantiate(vertexPreviewDebug, transform);
        debugObject.transform.localPosition = point;
    }
}
