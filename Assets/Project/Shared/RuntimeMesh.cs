using UnityEngine;

namespace LumberJack.Shared
{
    /// <summary>
    /// Dono de uma malha criada em runtime: põe no MeshFilter e destrói a malha junto com o objeto.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class RuntimeMesh : MonoBehaviour
    {
        private Mesh _mesh;
        private bool _hasMesh;

        private void OnDestroy()
        {
            DestroyMesh();
        }

        public void Assign(Mesh mesh)
        {
            DestroyMesh();
            _mesh = mesh;
            _hasMesh = true;
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        private void DestroyMesh()
        {
            if (!_hasMesh) return;

            Destroy(_mesh);
            _hasMesh = false;
        }
    }
}
