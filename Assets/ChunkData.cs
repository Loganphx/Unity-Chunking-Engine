using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class ChunkData
{
  public bool isValid;
  
  public Vector2 position;
  public Vector3 size;

  [SerializeField] 
  public Vector2[] faces;
  public Vector2[] vertices; 
  public int chunkSceneIndex;
  public byte x;
  public byte y;

  public bool isLoaded;
}