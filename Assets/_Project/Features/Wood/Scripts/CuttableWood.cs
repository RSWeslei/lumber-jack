using UnityEngine;

// Qualquer pedaco de madeira que pode ser cortado com o machado.
// Os pedacos gerados continuam no mesmo espaco local do tronco original,
// por isso os aneis da madeira ficam no lugar certo em todos os cortes.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CuttableWood : MonoBehaviour
{
    [Tooltip("Submesh/material usado na face de dentro (aneis da madeira)")]
    [SerializeField] private int innerSubmesh = 1;
    [Tooltip("Raio do tronco original, usado para mapear os aneis no corte")]
    [SerializeField] private float ringRadius = 0.3f;
    [Tooltip("Pedacos mais finos que isso nao podem ser cortados")]
    [SerializeField] private float minThickness = 0.08f;
    [SerializeField] private float density = 600f;
    [SerializeField] private float separationSpeed = 0.4f;

    private MeshFilter _meshFilter;
    private Rigidbody _rigidbody;

    public Mesh Mesh => _meshFilter.sharedMesh;

    // Eixo do tronco (comprimento) em coordenadas do mundo
    public Vector3 WorldAxis => transform.TransformDirection(Vector3.up);

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _rigidbody = GetComponent<Rigidbody>();
        UpdateMass();
    }

    /// <summary>
    /// Plano de corte (local) perpendicular ao comprimento da madeira no ponto mirado,
    /// inclinado em "tilt" graus no plano da tela de quem esta olhando.
    /// </summary>
    public Plane GetCutPlane(Vector3 worldPoint, Vector3 viewForward, Vector3 viewUp, float tilt)
    {
        Vector3 axis = WorldAxis;
        Vector3 tiltAxis = Vector3.ProjectOnPlane(viewForward, axis);
        if (tiltAxis.sqrMagnitude < 0.001f) tiltAxis = viewUp;
        Vector3 normal = Quaternion.AngleAxis(tilt, tiltAxis.normalized) * axis;
        return ToLocalPlane(worldPoint, normal);
    }

    /// <summary>
    /// Plano do mundo -> plano no espaco local deste pedaco.
    /// </summary>
    public Plane ToLocalPlane(Vector3 worldPoint, Vector3 worldNormal)
    {
        return new Plane(transform.InverseTransformDirection(worldNormal).normalized,
            transform.InverseTransformPoint(worldPoint));
    }

    /// <summary>
    /// Os dois pedacos ficariam grossos o suficiente com esse corte?
    /// </summary>
    public bool CanCut(Plane localPlane)
    {
        float min = float.MaxValue;
        float max = float.MinValue;
        foreach (Vector3 vertex in Mesh.vertices)
        {
            float distance = localPlane.GetDistanceToPoint(vertex);
            min = Mathf.Min(min, distance);
            max = Mathf.Max(max, distance);
        }
        float scale = transform.lossyScale.x;
        return -min * scale >= minThickness && max * scale >= minThickness;
    }

    public bool TryCut(Plane localPlane)
    {
        if (!CanCut(localPlane)) return false;
        if (!MeshSlicer.Slice(Mesh, localPlane, innerSubmesh, RingUV, out Mesh positive, out Mesh negative))
        {
            return false;
        }

        Vector3 worldNormal = transform.TransformDirection(localPlane.normal).normalized;
        CreatePiece(positive, worldNormal);
        CreatePiece(negative, -worldNormal);

        // Some na hora para os pedacos nao colidirem com o tronco antigo
        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }

    // Os aneis sao uma textura circular centrada no eixo do tronco (eixo Y local)
    private Vector2 RingUV(Vector3 localPoint)
    {
        return new Vector2(localPoint.x / (2f * ringRadius) + 0.5f, localPoint.z / (2f * ringRadius) + 0.5f);
    }

    private void CreatePiece(Mesh mesh, Vector3 pushDirection)
    {
        GameObject piece = new GameObject(name);
        piece.layer = gameObject.layer;
        piece.tag = gameObject.tag;
        piece.transform.SetParent(transform.parent, false);
        piece.transform.SetPositionAndRotation(transform.position, transform.rotation);
        piece.transform.localScale = transform.localScale;

        piece.AddComponent<MeshFilter>().sharedMesh = mesh;
        piece.AddComponent<MeshRenderer>().sharedMaterials = GetComponent<MeshRenderer>().sharedMaterials;

        MeshCollider meshCollider = piece.AddComponent<MeshCollider>();
        meshCollider.convex = true;
        meshCollider.sharedMesh = mesh;

        Rigidbody body = piece.AddComponent<Rigidbody>();
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        if (_rigidbody != null)
        {
            body.linearVelocity = _rigidbody.linearVelocity;
            body.angularVelocity = _rigidbody.angularVelocity;
        }

        // Copia as configuracoes deste componente para o pedaco novo
        CuttableWood wood = piece.AddComponent<CuttableWood>();
        wood.innerSubmesh = innerSubmesh;
        wood.ringRadius = ringRadius;
        wood.minThickness = minThickness;
        wood.density = density;
        wood.separationSpeed = separationSpeed;
        wood.UpdateMass();

        // Pedaco de madeira do jogo: herda o tipo de madeira e recalcula o preco pelo volume
        Wood gameWood = GetComponent<Wood>();
        if (gameWood != null) piece.AddComponent<Wood>().CopyFrom(gameWood);

        body.AddForce(pushDirection * separationSpeed, ForceMode.VelocityChange);
    }

    private void UpdateMass()
    {
        if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null || Mesh == null) return;

        Vector3 scale = transform.lossyScale;
        float volume = MeshSlicer.GetVolume(Mesh) * scale.x * scale.y * scale.z;
        _rigidbody.mass = Mathf.Max(0.5f, volume * density);
    }
}
