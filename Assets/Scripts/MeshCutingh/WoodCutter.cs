using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WoodCutter : MonoBehaviour
{
    [SerializeField] private LayerMask woodLayer;
    [SerializeField] private float raycastDistance = 2f;

    private void DrawCutLine(Vector3 contactPoint, Collider collider) 
    {
        
    }

    void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, raycastDistance, woodLayer)) {
            DrawCutLine(hit.point, hit.collider);
        }
    }
}
