using UnityEngine;

public readonly struct Triangle
{
    public readonly Vector3 PosA;
    public readonly Vector3 PosB;
    public readonly Vector3 PosC;

    public readonly Vector3 NormalA;
    public readonly Vector3 NormalB;
    public readonly Vector3 NormalC;

    public readonly Vector3 TangentA;
    public readonly Vector3 TangentB;
    public readonly Vector3 TangentC;

    public readonly Vector2 UVA;
    public readonly Vector2 UVB;
    public readonly Vector2 UVC;

    public Triangle(
        Vector3 posA, Vector3 posB, Vector3 posC, 
        Vector3 normalA, Vector3 normalB, Vector3 normalC,
        Vector3 tangentA, Vector3 tangentB, Vector3 tangentC,
        Vector2 uvA, Vector2 uvB, Vector2 uvC
        )
    {
        this.PosA = posA;
        this.PosB = posB;
        this.PosC = posC;
        this.NormalA = normalA;
        this.NormalB = normalB;
        this.NormalC = normalC;
        this.TangentA = tangentA;
        this.TangentB = tangentB;
        this.TangentC = tangentC;
        this.UVA = uvA;
        this.UVB = uvB;
        this.UVC = uvC;
    }
}