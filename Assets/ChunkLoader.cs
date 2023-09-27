using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChunkLoader : MonoBehaviour
{
  [SerializeField] private ChunkData _currentChunk;

  [SerializeField] private ChunkData[][] scenes = new ChunkData[][]
  {
    new ChunkData[5],
    new ChunkData[5],
    new ChunkData[5],
    new ChunkData[5],
    new ChunkData[5],
  };

  private List<AsyncOperation> _asyncOperations;

  private void Awake()
  {
    var sceneCount = SceneManager.sceneCountInBuildSettings;
    var scenesList = new List<ChunkData>();

    _asyncOperations = new List<AsyncOperation>();
    for (int i = 1; i < sceneCount; i++)
    {
      Debug.Log(i);
      var scene = SceneManager.GetSceneByBuildIndex(i);

      SceneManager.LoadScene(i, LoadSceneMode.Additive);
    }
  }

  private void Start()
  {
    for (int i = 1; i < SceneManager.sceneCount; i++)
    {
      var scene = SceneManager.GetSceneAt(i);
      Debug.Log($"Found scene {scene.name}, {scene.rootCount}");
      var chunk = scene.GetRootGameObjects()[0].GetComponent<Chunk>();
      if (chunk == null) return;

      Debug.Log("Found chunk " + $"{chunk.x}, {chunk.y}");
      var position = new Vector2(chunk.transform.position.x, chunk.transform.position.z);
      scenes[chunk.x][chunk.y] = new ChunkData
      {
        position = new Vector3(chunk.transform.position.x, 0, chunk.transform.position.z),
        faces = new []
        {
          position - new Vector2(0, chunk.size.z / 2),
          position - new Vector2(chunk.size.x / 2, 0),
          position + new Vector2(0, chunk.size.z / 2),
          position + new Vector2(chunk.size.x / 2, 0),
        },
        vertices = new []
        {
          // Bottom Left
          position - new Vector2(chunk.size.x / 2, chunk.size.z / 2),
          // Top Left
          position - new Vector2(chunk.size.x / 2, -chunk.size.z / 2),
          // Top Right
          position + new Vector2(chunk.size.x / 2, -chunk.size.z / 2),
          // Bottom Right
          position + new Vector2(chunk.size.x / 2, chunk.size.z / 2),
        },
        size = chunk.size,
        chunkSceneIndex = scene.buildIndex,
        x = chunk.x,
        y = chunk.y,
      };
      var boxCollider = new GameObject($"Chunk - {scene.name}").AddComponent<BoxCollider>();
      boxCollider.center = chunk.transform.position + chunk.offset;
      boxCollider.size = chunk.size;
      SceneManager.UnloadSceneAsync(scene.buildIndex, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
    }

    LoadClosestChunk(transform.position);
  }

  [SerializeField] private float closestDistance;
  [SerializeField] private byte closestX;
  [SerializeField] private byte closestY;
  [SerializeField] private float distance = 15;
  [SerializeField] private Vector3 chunkPos;
  [SerializeField] private Vector3 closestPos;

  public void LoadChunks(Vector2 position)
  {
    closestX = _currentChunk.x;
    closestY = _currentChunk.y;
    closestDistance = Vector2.Distance(position, _currentChunk.position);

    ChunkData chunk;
    if (_currentChunk.x > 0) // (-1, 0)
    {
      chunk = scenes[_currentChunk.x - 1][_currentChunk.y];
      if (chunk != null) HandleChunk(position, chunk, chunk.faces[3]);
    }

    // (-1, 1)
    if (_currentChunk.x > 0 && _currentChunk.y < scenes[_currentChunk.x].Length - 1)
    {
      chunk = scenes[_currentChunk.x - 1][_currentChunk.y + 1];
      if (chunk != null) HandleChunk(position, chunk, chunk.vertices[2]);
    }

    if (_currentChunk.y > 0) // (0, -1)
    {
      chunk = scenes[_currentChunk.x][_currentChunk.y - 1];
      if (chunk != null) HandleChunk(position, chunk, chunk.faces[2]);
    }

    if (_currentChunk.x > 0 && _currentChunk.y > 0) // (-1, -1)
    {
      chunk = scenes[_currentChunk.x - 1][_currentChunk.y - 1];
      if (chunk != null) HandleChunk(position, chunk, chunk.vertices[3]);
    }

    // (1, -1)
    if (_currentChunk.x < scenes.Length - 1 && _currentChunk.y > 0)
    {
      chunk = scenes[_currentChunk.x + 1][_currentChunk.y - 1];
      if (chunk != null) HandleChunk(position, chunk, chunk.vertices[1]);
    }

    if (_currentChunk.x < scenes.Length - 1) // (1, 0)
    {
      chunk = scenes[_currentChunk.x + 1][_currentChunk.y];
      if (chunk != null) HandleChunk(position, chunk, chunk.faces[1]);
    }

    if (_currentChunk.y < scenes[_currentChunk.x].Length - 1) // (0, 1)
    {
      chunk = scenes[_currentChunk.x][_currentChunk.y + 1];
      if (chunk != null) HandleChunk(position, chunk, chunk.faces[0]);
    }

    if (_currentChunk.x < scenes.Length - 1 && _currentChunk.y < scenes[_currentChunk.x].Length - 1) // (1, 1)
    {
      chunk = scenes[_currentChunk.x + 1][_currentChunk.y + 1];
      if (chunk != null) HandleChunk(position, chunk, chunk.vertices[0]);
    }

    if (_currentChunk.x == closestX && _currentChunk.y == closestY) return;
    if (Vector3.Distance(position, _currentChunk.position) > distance) UnloadChunk(_currentChunk);
    chunkPos = closestPos;
    _currentChunk = scenes[closestX][closestY];
  }

  private void HandleChunk(Vector3 position, ChunkData chunkData, Vector3 chunkPos)
  {
    if (chunkData != null)
    {
      var nextXYDistance = Vector3.Distance(position, chunkData.position);
      if (nextXYDistance < distance)
      {
        LoadChunk(chunkData);
        if (nextXYDistance < closestDistance)
        {
          closestDistance = nextXYDistance;
          closestX = chunkData.x;
          closestY = chunkData.y;
          closestPos = chunkPos;
        }
      }
      else
        UnloadChunk(chunkData);
    }
  }

  private void LoadClosestChunk(Vector3 position)
  {
    closestX = 0;
    closestY = 0;
    closestDistance = float.MaxValue;
    foreach (var x in scenes)
    {
      foreach (var y in x)
      {
        if (y == null) continue;
        var nextXDistance = Vector3.Distance(position, y.position);
        if (nextXDistance < closestDistance)
        {
          closestDistance = nextXDistance;
          closestX = y.x;
          closestY = y.y;
          closestPos = y.position;
        }
      }
    }

    Debug.Log($"Loading closest chunk: {closestX}, {closestY} - {closestDistance}");
    LoadChunk(scenes[closestX][closestY]);
    chunkPos = closestPos;
    _currentChunk = scenes[closestX][closestY];
  }

  private void LoadChunk(ChunkData chunkData)
  {
    if (chunkData.isLoaded) return;
    SceneManager.LoadSceneAsync(chunkData.chunkSceneIndex, LoadSceneMode.Additive);
    chunkData.isLoaded = true;
  }

  private void UnloadChunk(ChunkData chunkData)
  {
    if (!chunkData.isLoaded) return;
    //SceneManager.GetSceneAt(chunkData.chunkSceneIndex).GetRootGameObjects()[0].gameObject.SetActive(false);
    SceneManager.UnloadSceneAsync(chunkData.chunkSceneIndex, UnloadSceneOptions.None);
    chunkData.isLoaded = false;
  }


  private void OnDrawGizmos()
  {
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, distance);
    foreach (var x in scenes)
    {
      foreach (var y in x)
      {
        if (y != null)
        {
          if (!y.isLoaded)
          {
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(y.position, new Vector3(y.size.x, 0.01f, y.size.z));
          }

          if (!y.isLoaded) GUI.contentColor = Color.white;
          else GUI.contentColor = Color.green;
          Handles.Label(y.position, $"({y.x}, {y.y})");

          foreach (var vert in y.faces)
          {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(new Vector3(vert.x, 0, vert.y), new Vector3(0.1f, 0.1f,0.1f));
          }
          
          
          foreach (var vert in y.vertices)
          {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(new Vector3(vert.x, 0, vert.y), 0.1f);
          }
        }
      }
    }
  }

  public void FixedUpdate()
  {
    LoadChunks(transform.position);
  }
}