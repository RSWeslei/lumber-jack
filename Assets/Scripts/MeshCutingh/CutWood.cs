using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(MeshFilter))]
public class CutWood : MonoBehaviour
{   
    [SerializeField] private GameObject vertexPreviewDebug;
    private MeshFilter meshFilter;
    Mesh mesh;
    private void Awake() {
        meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.mesh;
    }

    private IEnumerator LogVertexWithDelay(Vector3[] vertex, float delay) {
        for (int i = 0; i < vertex.Length; i++) {
            LogVertex(vertex[i], i);
            yield return new WaitForSeconds(delay);
        }
    }

    private void LogVertex(Vector3 point, int i) {
        // instanciate a debug object at the point of gameobject using local position of this gameobject
        GameObject debugObject = Instantiate(vertexPreviewDebug, transform);
        debugObject.GetComponentInChildren<TextMeshPro>().text = i + " " + point.ToString();
        debugObject.transform.rotation = Quaternion.identity;
        debugObject.transform.localPosition = point;
    }

    public void CutQuads (Vector3[] vertices) {
        Vector3[] cutPoints = vertices;

        // log vertices
        foreach (Vector3 vertex in vertices) {
            LogVertex(vertex, 0);
        }

        int[] leftTriangles = new int[cutPoints.Length * 3];
        int[] rightTriangles = new int[cutPoints.Length * 3];
 
        Vector3[] leftVertices = new Vector3[vertices.Length + cutPoints.Length];
        Vector3[] rightVertices = new Vector3[vertices.Length + cutPoints.Length];

        // leftVertices[0] = vertices[0];
        // leftVertices[1] = cutPointA;
        // leftVertices[2] = vertices[2];
        // leftVertices[3] = cutPointB;

        // rightVertices[0] = cutPointA;
        // rightVertices[1] = vertices[1];
        // rightVertices[2] = cutPointB;
        // rightVertices[3] = vertices[3];

        // leftTriangles[0] = 0;
        // leftTriangles[1] = 2;
        // leftTriangles[2] = 1;

        // leftTriangles[3] = 1;
        // leftTriangles[4] = 2;
        // leftTriangles[5] = 3;

        // rightTriangles[0] = 0;
        // rightTriangles[1] = 2;
        // rightTriangles[2] = 1;

        // rightTriangles[3] = 1;
        // rightTriangles[4] = 2;
        // rightTriangles[5] = 3;

        // do the same from above but with a multiples quads
        for (int i = 0; i < vertices.Length; i += 4) {
            // get the 4 vertices of the quad
            Vector3[] quadVertices = new Vector3[4] {vertices[i], vertices[i + 1], vertices[i + 2], vertices[i + 3]};

            // get the 2 cut points
            Vector3[] cutPointsQuad = new Vector3[2] {cutPoints[i / 4], cutPoints[i / 4 + 1]};

            // get the 2 triangles of the quad
            int[] quadTriangles = new int[6] {i, i + 2, i + 1, i + 1, i + 2, i + 3};

            // get the 2 triangles of the left side of the quad
            int[] leftTrianglesQuad = new int[6] {i, i + 2, i + 1, i + 1, i + 2, i + 3};

            // get the 2 triangles of the right side of the quad
            int[] rightTrianglesQuad = new int[6] {i, i + 2, i + 1, i + 1, i + 2, i + 3};

            // get the 2 vertices of the left side of the quad
            Vector3[] leftVerticesQuad = new Vector3[4] {quadVertices[0], cutPointsQuad[0], quadVertices[2], cutPointsQuad[1]};

            // get the 2 vertices of the right side of the quad
            Vector3[] rightVerticesQuad = new Vector3[4] {cutPointsQuad[0], quadVertices[1], cutPointsQuad[1], quadVertices[3]};

            // add the 2 triangles of the left side of the quad to the leftTriangles array
            leftTriangles[i] = leftTrianglesQuad[0];
            leftTriangles[i + 1] = leftTrianglesQuad[1];
            leftTriangles[i + 2] = leftTrianglesQuad[2];
            leftTriangles[i + 3] = leftTrianglesQuad[3];
            leftTriangles[i + 4] = leftTrianglesQuad[4];
            leftTriangles[i + 5] = leftTrianglesQuad[5];

            // add the 2 triangles of the right side of the quad to the right
            rightTriangles[i] = rightTrianglesQuad[0];
            rightTriangles[i + 1] = rightTrianglesQuad[1];
            rightTriangles[i + 2] = rightTrianglesQuad[2];
            rightTriangles[i + 3] = rightTrianglesQuad[3];
            rightTriangles[i + 4] = rightTrianglesQuad[4];
            rightTriangles[i + 5] = rightTrianglesQuad[5];

            // add the 2 vertices of the left side of the quad to the leftVertices array
            leftVertices[i] = leftVerticesQuad[0];
            leftVertices[i + 1] = leftVerticesQuad[1];
            leftVertices[i + 2] = leftVerticesQuad[2];
            leftVertices[i + 3] = leftVerticesQuad[3];

            // add the 2 vertices of the right side of the quad to the rightVertices array
            rightVertices[i] = rightVerticesQuad[0];
            rightVertices[i + 1] = rightVerticesQuad[1];
            rightVertices[i + 2] = rightVerticesQuad[2];
            rightVertices[i + 3] = rightVerticesQuad[3];
        }
        

        Mesh leftMesh = new Mesh {
            vertices = leftVertices,
            triangles = leftTriangles
        };

        Mesh rightMesh = new Mesh {
            vertices = rightVertices,
            triangles = rightTriangles
        };

        leftMesh.RecalculateNormals();
        rightMesh.RecalculateNormals();

        GameObject left = new GameObject("Left");
        left.AddComponent<MeshFilter>().mesh = leftMesh;
        left.AddComponent<MeshRenderer>().material = GetComponent<MeshRenderer>().material;
        left.AddComponent<MeshCollider>().sharedMesh = leftMesh;
        left.transform.position = transform.position;
        left.transform.rotation = transform.rotation;

        GameObject right = new GameObject("Right");
        right.AddComponent<MeshFilter>().mesh = rightMesh;
        right.AddComponent<MeshRenderer>().material = GetComponent<MeshRenderer>().material;
        right.AddComponent<MeshCollider>().sharedMesh = rightMesh;
        right.transform.position = transform.position;
        right.transform.rotation = transform.rotation;

        Destroy(gameObject);


       
    }

    private void Start() {
        CutQuads(mesh.vertices);
    }
}
