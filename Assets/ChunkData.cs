using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class ChunkData
{
  public Vector3 position;
  public Vector3 size;

  public Vector2[] faces;
  public Vector2[] vertices; 
  public int chunkSceneIndex;
  public byte x;
  public byte y;

  public bool isLoaded;
}