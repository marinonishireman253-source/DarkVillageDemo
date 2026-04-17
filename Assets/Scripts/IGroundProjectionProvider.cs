using UnityEngine;

public interface IGroundProjectionProvider
{
    Vector3 GetGroundProjection(Vector3 worldPosition);
}
