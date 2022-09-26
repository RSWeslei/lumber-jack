using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cutter : MonoBehaviour
{
    Transform inicialPos;
    Transform finalPos;
    [SerializeField] LayerMask layerMask;
    float moveSpeed = 0.1f;
    [SerializeField] TestMesh testMesh;

    void Start()
    {
        inicialPos = transform.GetChild(0);
        finalPos = transform.GetChild(1);
    }

    void Update()
    {
        // raycast between the two points and draw a line
        // move the gameobject to z axis
        transform.Translate(Vector3.forward * Time.deltaTime * moveSpeed);

        RaycastHit hit;
        if (Physics.Raycast(inicialPos.position, finalPos.position - inicialPos.position, out hit, Mathf.Infinity, layerMask))
        {
            Debug.DrawLine(inicialPos.position, hit.point, Color.red);
            //log local position of the hit point
            testMesh.addVertex(hit.transform.InverseTransformPoint(hit.point));
        }
        else
        {
            Debug.DrawLine(inicialPos.position, finalPos.position, Color.green);
        }
    }
}
