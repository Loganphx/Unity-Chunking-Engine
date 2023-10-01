using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

[Serializable]
public class ChunkLoader : MonoBehaviour
{
  [SerializeField] private ChunkData currentChunk;

  [SerializeField] private ChunkData[][] scenes = new ChunkData[][]
  {
    new ChunkData[6],
    new ChunkData[6],
    new ChunkData[6],
    new ChunkData[6],
    new ChunkData[6],
    new ChunkData[6],
  };

  private void Awake()
  {
    _transform = transform;
    scenes = new ChunkData[][]
    {
      new ChunkData[5],
      new ChunkData[5],
     };
    
    Application.targetFrameRate = 60;
    var sceneCount = SceneManager.sceneCountInBuildSettings;

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
      if (chunk.x < scenes.Length && chunk.y < scenes[chunk.x].Length)
      {
        scenes[chunk.x][chunk.y] = new ChunkData
        {
          isValid  = true,
          position = position,
          faces = new[]
          {
            position - new Vector2(0, chunk.size.z / 2),
            position - new Vector2(chunk.size.x    / 2, 0),
            position + new Vector2(0,                   chunk.size.z / 2),
            position + new Vector2(chunk.size.x                      / 2, 0),
          },
          vertices = new[]
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
          size            = chunk.size,
          chunkSceneIndex = scene.buildIndex,
          x               = chunk.x,
          y               = chunk.y,
        };
      }
      
      //var boxCollider = new GameObject($"Chunk - {scene.name}").AddComponent<BoxCollider>();
      //boxCollider.center = chunk.transform.position + chunk.offset;
      //boxCollider.size = chunk.size;
      SceneManager.UnloadSceneAsync(scene.buildIndex, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
    }

    LoadClosestChunk(new Vector2(transform.position.x, transform.position.z));
  }

  [SerializeField] private float closestDistance;
  [SerializeField] private byte closestX;
  [SerializeField] private byte closestY;
  [SerializeField] private float distance = 15;
  [SerializeField] private Vector2 chunkPos;
  [SerializeField] private Vector2 closestPos;

  struct LineData
  {
    public Vector3 begin;
    public Vector3 end;
    public Color color;
    public byte x;
    public byte y;
    public float distance;
  }

  struct BoxData
  {
    public Vector3 center;
    public Vector3 size;
    public Color color;
  }

  private List<LineData> _lines = new List<LineData>();
  private List<BoxData> _boxes = new List<BoxData>();

  public void LoadChunks(Vector2 position)
  {
    _lines.Clear();
    _boxes.Clear();

    closestX = currentChunk.x;
    closestY = currentChunk.y;
    closestDistance = Vector2.Distance(position, currentChunk.position);

    ChunkData chunk;
    if (currentChunk.x > 0) // (-1, 0)
    {
      chunk = scenes[(byte)(currentChunk.x - 1)][currentChunk.y];
      if (chunk != null) HandleChunk(position, chunk, chunk.faces[1], true, false);
    }

    // (-1, 1)
    if (currentChunk.x > 0 && currentChunk.y < scenes[currentChunk.x].Length - 1)
    {
      chunk = scenes[(byte)(currentChunk.x - 1)][(byte)(currentChunk.y + 1)];
      if (chunk != null) HandleChunk(position, chunk, chunk.vertices[1], true, true);
    }

    if (currentChunk.y > 0) // (0, -1)
    {
      chunk = scenes[currentChunk.x][(byte)(currentChunk.y - 1)];
      if (chunk != null) HandleChunk(position, chunk, chunk.faces[0], false, true);
    }

    if (currentChunk.x > 0 && currentChunk.y > 0) // (-1, -1)
    {
      chunk = scenes[(byte)(currentChunk.x - 1)][(byte)(currentChunk.y - 1)];
      if (chunk != null) HandleChunk(position, chunk, chunk.vertices[0], true, true);
    }

    // (1, -1)
    if (currentChunk.x < scenes.Length - 1 && currentChunk.y > 0)
    {
      chunk = scenes[(byte)(currentChunk.x + 1)][(byte)(currentChunk.y - 1)];
      if (chunk != null) HandleChunk(position, chunk, chunk.vertices[2], true, true);
    }

    if (currentChunk.x < scenes.Length - 1) // (1, 0)
    {
      chunk = scenes[(byte)(currentChunk.x + 1)][currentChunk.y];
      if (chunk != null) HandleChunk(position, chunk, chunk.faces[3], true, false);
    }

    if (currentChunk.y < scenes[currentChunk.x].Length - 1) // (0, 1)
    {
      chunk = scenes[currentChunk.x][(byte)(currentChunk.y + 1)];
      if (chunk != null) HandleChunk(position, chunk, chunk.faces[02], false, true);
    }

    if (currentChunk.x < scenes.Length - 1 && currentChunk.y < scenes[currentChunk.x].Length - 1) // (1, 1)
    {
      chunk = scenes[(byte)(currentChunk.x + 1)][(byte)(currentChunk.y + 1)];
      if (chunk != null) HandleChunk(position, chunk, chunk.vertices[3], true, true);
    }

    if (currentChunk.x == closestX && currentChunk.y == closestY)
    {
      closestPos = currentChunk.position;
      return;
    }

    //var chunkDistance = Vector2.Distance(position, currentChunk.position);
    //Debug.Log("");
    //if (chunkDistance > distance) UnloadChunk(currentChunk);
    chunkPos = closestPos;
    currentChunk = scenes[closestX][closestY];
  }

  private void HandleChunk(Vector2 position, ChunkData chunkData, Vector2 chunkPos, bool checkX, bool checkZ)
  {
    if (chunkData.isValid)
    {
      float nextDistance;
      if (checkX && checkZ) nextDistance = Vector2.Distance(position, chunkPos);
      else if (checkX) nextDistance = Mathf.Abs(position.x - chunkPos.x);
      else nextDistance = Mathf.Abs(position.y - chunkPos.y);
      if (nextDistance < distance)
      {
        Vector3 end;
        if (checkX && checkZ) end = new Vector3(chunkPos.x, 0, chunkPos.y);
        else if (checkX) end = new Vector3(chunkPos.x, 0, position.y);
        else end = new Vector3(position.x, 0, chunkPos.y);

        _boxes.Add(new BoxData()
        {
          center = new Vector3(chunkData.position.x, 0, chunkData.position.y),
          size = new Vector3(chunkData.size.x, 0.01f, chunkData.size.z),
          color = Color.green
        });
        _lines.Add(new LineData()
        {
          begin = new Vector3(position.x, 0, position.y),
          end = end,
          color = Color.green,
          x = chunkData.x,
          y = chunkData.y,
          distance = nextDistance
        });
        LoadChunk(chunkData);
        var distanceToCenter = Vector2.Distance(position, chunkData.position);
        if (distanceToCenter < closestDistance)
        {
          closestDistance = distanceToCenter;
          closestX = chunkData.x;
          closestY = chunkData.y;
          closestPos = chunkData.position;
        }
      }
      else
      {
        Vector3 end;
        if (checkX && checkZ) end = new Vector3(chunkPos.x, 0, chunkPos.y);
        else if (checkX) end = new Vector3(chunkPos.x, 0, position.y);
        else end = new Vector3(position.x, 0, chunkPos.y);
        _lines.Add(new LineData()
        {
          begin = new Vector3(position.x, 0, position.y),
          end = end,
          color = Color.red,
          x = chunkData.x,
          y = chunkData.y,
          distance = nextDistance
        });
        _boxes.Add(new BoxData()
        {
          center = new Vector3(chunkData.position.x, 0, chunkData.position.y),
          size = new Vector3(chunkData.size.x, 0.01f, chunkData.size.z),
          color = Color.red
        });
        UnloadChunk(chunkData);
      }
    }
  }

  private void LoadClosestChunk(Vector2 position)
  {
    closestX = 0;
    closestY = 0;
    closestDistance = float.MaxValue;
    foreach (var x in scenes)
    {
      foreach (var y in x)
      {
        if (y == null) continue;
        var nextXDistance = Vector2.Distance(position, y.position);
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
    currentChunk = scenes[closestX][closestY];
  }

  private void LoadChunk(ChunkData chunkData)
  {
    if (chunkData.isLoaded) return;
    SceneManager.LoadSceneAsync(chunkData.chunkSceneIndex, LoadSceneMode.Additive);
    chunkData.isLoaded = true;
  }

  private void UnloadChunk(ChunkData chunkData)
  {
    //Debug.Log("Unloading!");
    if (!chunkData.isLoaded) return;
    Debug.Log($"<color=green>Unloading! {chunkData.x}, {chunkData.y}</color>");
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
        if (y == null) continue;
        //if (!y.isLoaded)
        {
          Gizmos.color = Color.white;
          Gizmos.DrawWireCube(new Vector3(y.position.x, 0, y.position.y), new Vector3(y.size.x, 0.01f, y.size.z));
        }

        if (!y.isLoaded) GUI.contentColor = Color.white;
        else GUI.contentColor = Color.green;
        Handles.Label(new Vector3(y.position.x, 0, y.position.y), $"({y.x}, {y.y})");

        // foreach (var vert in y.faces)
        // {
        //   Gizmos.color = Color.green;
        //   Gizmos.DrawWireCube(new Vector3(vert.x, 0, vert.y), new Vector3(0.1f, 0.1f,0.1f));
        // }
        //
        //
        // for (int i = 0; i < y.vertices.Length; i++)
        // {
        //   var vert = y.vertices[i];
        //   Gizmos.color = Color.red;
        //   Gizmos.DrawWireSphere(new Vector3(vert.x, 0, vert.y), 0.1f);
        // }
      }
    }

    foreach (var line in _boxes)
    {
      Gizmos.color = line.color;
      Gizmos.DrawWireCube(line.center, line.size);
    }

    foreach (var line in _lines)
    {
      Gizmos.color = line.color;
      GUI.contentColor = line.color;
      Gizmos.DrawLine(line.begin, line.end);
      Handles.Label(Vector3.Lerp(line.begin, line.end, 0.5f), $"({line.x}, {line.y}) - {line.distance}");

      if (Gizmos.color == Color.red)
      {
        GUI.contentColor = line.color;
        for (int i = 0; i < scenes[line.x][line.y].vertices.Length; i++)
        {
          var vert = scenes[line.x][line.y].vertices[i];
          Handles.Label(new Vector3(vert.x, 0, vert.y), $"{i}");
        }
      }
    }
  }

  private Transform _transform;
  private const string ProfilerName = "ChunkLoader.FixedUpdate";
  public void FixedUpdate()
  {
    Profiler.BeginSample(ProfilerName);
    var position = _transform.position;
    LoadChunks(new Vector2(position.x, position.z));
    Profiler.EndSample();
  }
}