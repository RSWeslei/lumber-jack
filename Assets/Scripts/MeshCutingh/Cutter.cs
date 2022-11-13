using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cutter : MonoBehaviour
{
    public static bool currentlyCutting;
    public static Mesh originalMesh;

    public static void Cut(GameObject _originalGameObject, Vector3 _contactPoint, Vector3 _direction,
    Material _cutMaterial = null, bool fill = true, bool _addRigidbody = false) 
    {
        if (currentlyCutting) {
            return;
        }

        currentlyCutting = true;

        Plane plane = new Plane(_originalGameObject.transform.InverseTransformDirection(-_direction),
        _originalGameObject.transform.InverseTransformPoint(_contactPoint));
        originalMesh = _originalGameObject.GetComponent<MeshFilter>().mesh;
        List<Vector3> addedVertices = new List<Vector3>();

        GenerateMesh leftMesh = new GenerateMesh();
        GenerateMesh rightMesh = new GenerateMesh();

        int[] submeshIndices;
        int triangleIndexA, triangleIndexB, triangleIndexC;

        for (int i = 0; i < originalMesh.subMeshCount; i++)
        {
            submeshIndices = originalMesh.GetTriangles(i);

            for (int j=0; j < submeshIndices.Length; j+=3){
                triangleIndexA = submeshIndices[j];
                triangleIndexB = submeshIndices[j+1];
                triangleIndexC = submeshIndices[j+2];

                MeshTriangle currentTriangle = GetTriangle(triangleIndexA, triangleIndexB, triangleIndexC, i);

                bool triangleALeftSide = plane.GetSide(originalMesh.vertices[triangleIndexA]);
                bool triangleBLeftSide = plane.GetSide(originalMesh.vertices[triangleIndexB]);
                bool triangleCLeftSide = plane.GetSide(originalMesh.vertices[triangleIndexC]);

                if (triangleALeftSide && triangleBLeftSide && triangleCLeftSide) {
                    leftMesh.AddTriangle(currentTriangle);
                }
                else if (!triangleALeftSide && !triangleBLeftSide && !triangleCLeftSide) {
                    rightMesh.AddTriangle(currentTriangle);
                }
                else {
                    CutTriangle(plane, currentTriangle, triangleALeftSide, triangleBLeftSide, triangleCLeftSide, leftMesh, rightMesh, addedVertices);
                }
            }
        }
    }

    public static MeshTriangle GetTriangle(int _indexA, int _indexB, int _indexC, int _submeshIndex) {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        vertices.Add(originalMesh.vertices[_indexA]);
        vertices.Add(originalMesh.vertices[_indexB]);
        vertices.Add(originalMesh.vertices[_indexC]);

        normals.Add(originalMesh.normals[_indexA]);
        normals.Add(originalMesh.normals[_indexB]);
        normals.Add(originalMesh.normals[_indexC]);

        uvs.Add(originalMesh.uv[_indexA]);
        uvs.Add(originalMesh.uv[_indexB]);
        uvs.Add(originalMesh.uv[_indexC]);

        return new MeshTriangle(vertices, normals, uvs, _submeshIndex);
    }

    private static void CutTriangle(Plane _plane, MeshTriangle _triangle, bool _triangleALeftSide, bool _triangleBLeftSide, bool _triangleCLeftSide,
    GenerateMesh _leftSide, GenerateMesh _rightSide, List<Vector3> _addedVertices)
    {
        List<bool> leftSide = new List<bool>();
        leftSide.Add(_triangleALeftSide);
        leftSide.Add(_triangleBLeftSide);
        leftSide.Add(_triangleCLeftSide);

        MeshTriangle leftMeshTriangle = new MeshTriangle(new List<Vector3>(), new List<Vector3>(), new List<Vector2>(), _triangle.SubmeshIndex);
        MeshTriangle rightMeshTriangle = new MeshTriangle(new List<Vector3>(), new List<Vector3>(), new List<Vector2>(), _triangle.SubmeshIndex);

        bool left = false;
        bool right = false;

        for (int i = 0; i < 3; i++)
        {
            if (leftSide[i]) {
                if (!left) {
                    left = true;

                    leftMeshTriangle.Vertices[0] = _triangle.Vertices[i];
                    leftMeshTriangle.Normals[1] = leftMeshTriangle.Vertices[i];

                    leftMeshTriangle.UVs[0] = _triangle.UVs[i];
                    leftMeshTriangle.UVs[1] = leftMeshTriangle.UVs[i];

                    leftMeshTriangle.Normals[0] = _triangle.Normals[i];
                    leftMeshTriangle.Normals[1] = leftMeshTriangle.Normals[i];
                }
                else {
                    leftMeshTriangle.Vertices[1] = _triangle.Vertices[i];
                    leftMeshTriangle.Normals[1] = _triangle.Normals[i];
                    leftMeshTriangle.UVs[1] = _triangle.UVs[i];
                }
            }
            else {
                if (!right) {
                    right = true;

                    rightMeshTriangle.Vertices[0] = _triangle.Vertices[i];
                    rightMeshTriangle.Vertices[1] = rightMeshTriangle.Vertices[0];

                    rightMeshTriangle.UVs[0] = _triangle.UVs[i];
                    rightMeshTriangle.UVs[1] = rightMeshTriangle.UVs[0];

                    rightMeshTriangle.Normals[0] = _triangle.Normals[i];
                    rightMeshTriangle.Normals[1] = rightMeshTriangle.Normals[0];
                }
                else {
                    rightMeshTriangle.Vertices[1] = _triangle.Vertices[i];
                    rightMeshTriangle.Normals[1] = _triangle.Normals[i];
                    rightMeshTriangle.UVs[1] = _triangle.UVs[i];
                }
            }
        }

        float normalizedDistance;
        float distance;
        _plane.Raycast(new Ray(leftMeshTriangle.Vertices[0], (leftMeshTriangle.Vertices[0] - leftMeshTriangle.Vertices[0]).normalized), out distance);
        normalizedDistance = distance / (leftMeshTriangle.Vertices[0] - leftMeshTriangle.Vertices[0]).magnitude;

        Vector3 vertLeft = Vector3.Lerp(leftMeshTriangle.Vertices[0], rightMeshTriangle.Vertices[0], normalizedDistance);
        _addedVertices.Add(vertLeft);

        Vector3 normalLeft = Vector3.Lerp(leftMeshTriangle.Normals[0], rightMeshTriangle.Normals[0], normalizedDistance);
        Vector2 uvLeft = Vector2.Lerp(leftMeshTriangle.UVs[0], rightMeshTriangle.UVs[0], normalizedDistance);

        _plane.Raycast(new Ray(leftMeshTriangle.Vertices[1], (leftMeshTriangle.Vertices[1] - leftMeshTriangle.Vertices[1]).normalized), out distance);

        normalizedDistance = distance / (leftMeshTriangle.Vertices[1] - leftMeshTriangle.Vertices[1]).magnitude;
        Vector3 vertRight = Vector3.Lerp(leftMeshTriangle.Vertices[1], rightMeshTriangle.Vertices[1], normalizedDistance);
        _addedVertices.Add(vertRight);

        Vector3 normalRight = Vector3.Lerp(leftMeshTriangle.Normals[1], rightMeshTriangle.Normals[1], normalizedDistance);
        Vector2 uvRight = Vector2.Lerp(leftMeshTriangle.UVs[1], rightMeshTriangle.UVs[1], normalizedDistance);

        MeshTriangle currentTriangle;
        List<Vector3> updatedVertices = new List<Vector3> { leftMeshTriangle.Vertices[0], vertLeft, vertRight };
        List<Vector3> updatedNormals = new List<Vector3> { leftMeshTriangle.Normals[0], normalLeft, normalRight };
        List<Vector2> updatedUVs = new List<Vector2> { leftMeshTriangle.UVs[0], uvLeft, uvRight };

        currentTriangle = new MeshTriangle(updatedVertices, updatedNormals, updatedUVs, _triangle.SubmeshIndex);

        if (updatedVertices[0] != updatedVertices[1] && updatedVertices[1] != updatedVertices[2]) {
            if (Vector3.Dot(Vector3.Cross(updatedVertices[1] - updatedVertices[0], updatedVertices[2] - updatedVertices[0]), updatedNormals[0]) < 0) {
                FlipTriangle(currentTriangle);
            }
            _leftSide.AddTriangle(currentTriangle);
        }

        updatedVertices = new List<Vector3> { leftMeshTriangle.Vertices[0], leftMeshTriangle.Vertices[1], vertRight, };
        updatedNormals = new List<Vector3> { leftMeshTriangle.Normals[0], leftMeshTriangle.Normals[1], normalRight };
        updatedUVs = new List<Vector2> { leftMeshTriangle.UVs[0], leftMeshTriangle.UVs[1], uvRight };

        currentTriangle = new MeshTriangle(updatedVertices, updatedNormals, updatedUVs, _triangle.SubmeshIndex);

        if (updatedVertices[0] != updatedVertices[1] && updatedVertices[0] != updatedVertices[2]) {
            if (Vector3.Dot(Vector3.Cross(updatedVertices[1] - updatedVertices[0], updatedVertices[2] - updatedVertices[0]), updatedNormals[0]) < 0) {
                FlipTriangle(currentTriangle);
            }
            _rightSide.AddTriangle(currentTriangle);
        }
    }

    private static void FlipTriangle(MeshTriangle _triangle) {
        Vector3 temp = _triangle.Vertices[0];
        _triangle.Vertices[0] = _triangle.Vertices[1];
        _triangle.Vertices[1] = temp;

        temp = _triangle.Normals[0];
        _triangle.Normals[0] = _triangle.Normals[1];
        _triangle.Normals[1] = temp;

        Vector2 tempUV = _triangle.UVs[0];
        _triangle.UVs[0] = _triangle.UVs[1];
        _triangle.UVs[1] = tempUV;
    }
}
