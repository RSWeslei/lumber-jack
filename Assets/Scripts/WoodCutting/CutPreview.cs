using System.Collections.Generic;
using UnityEngine;

// Desenha com um LineRenderer o contorno de onde o corte vai passar.
public class CutPreview
{
    private readonly LineRenderer _line;

    public Color ValidColor = new Color(0.2f, 1f, 0.3f);
    public Color InvalidColor = new Color(1f, 0.25f, 0.2f);

    public CutPreview(LineRenderer line)
    {
        _line = line;
        _line.useWorldSpace = true;
        _line.loop = true;
        _line.positionCount = 0;
    }

    /// <summary>
    /// Cria um LineRenderer filho de parent ja configurado para o preview.
    /// </summary>
    public static CutPreview Create(Transform parent, Material material, float width = 0.012f)
    {
        GameObject lineObject = new GameObject("Cut Preview");
        lineObject.transform.SetParent(parent, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = material;
        line.widthMultiplier = width;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        return new CutPreview(line);
    }

    public void Show(CuttableWood wood, Plane localPlane, bool valid)
    {
        List<Vector3> outline = MeshSlicer.GetCrossSection(wood.Mesh, localPlane);
        if (outline.Count < 3)
        {
            Hide();
            return;
        }

        // Empurra a linha um pouquinho para fora da madeira para nao sumir dentro da casca
        Vector3 center = Vector3.zero;
        foreach (Vector3 point in outline) center += point;
        center /= outline.Count;

        _line.positionCount = outline.Count;
        for (int i = 0; i < outline.Count; i++)
        {
            Vector3 local = center + (outline[i] - center) * 1.02f;
            _line.SetPosition(i, wood.transform.TransformPoint(local));
        }

        Color color = valid ? ValidColor : InvalidColor;
        _line.startColor = color;
        _line.endColor = color;
    }

    public void Hide()
    {
        _line.positionCount = 0;
    }
}
