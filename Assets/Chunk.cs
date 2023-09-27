using UnityEngine;

public class Chunk : MonoBehaviour
{
  private Transform _transform;
  public Vector3 offset;
  public Vector3 size;
  public byte x;
  public byte y;

  private Color _color;
  private void OnValidate()
  {
    _transform = transform;
    _color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
  }

  private void OnDrawGizmos()
  {
    //Gizmos.color = _color;
    //Gizmos.DrawWireCube(_transform.position + offset, size);
  }
}