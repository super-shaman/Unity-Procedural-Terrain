using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.IO;
using System;


public class Chunk : MonoBehaviour
{

    public MeshFilter mf;
    public MeshCollider mc;
    public static int size;
    public static int loadSize;
    float[,] heights;
    List<Vector3> vertices = new List<Vector3>();
    List<int> indices = new List<int>();
    List<Vector3> normals = new List<Vector3>();
    List<Color> colors = new List<Color>();
    int index1;
    int index2;
    static List<Chunk> chunks = new List<Chunk>();
    static List<Chunk> loadedChunks = new List<Chunk>();
    static List<Chunk> allChunks = new List<Chunk>();
    bool loaded;
    bool loading;


    public void Load(int index1, int index2)
    {
        this.index1 = index1;
        this.index2 = index2;
        chunks.Add(this);
        if (!allChunks.Contains(this))
        {
            allChunks.Add(this);
        }
    }

    static bool done;
    static bool running;
    static int numThreads;
    static int amountOfThreads = Environment.ProcessorCount;
    static int amountToLoad = 1;
    static Chunk[] loads = new Chunk[amountToLoad];

    public static void LoadChunks()
    {
        if (!running)
        {
            if (numThreads < amountOfThreads)
            {
                Thread thread = new Thread(Run);
                numThreads++;
                running = true;
                thread.Start();
            }
        } else if (done)
        {
            Debug.Log(chunks.Count);
            Debug.Log("Gorp");
            running = false;
            done = false;
            numThreads--;
            for (int i = 0; i < amountToLoad; i++)
            {
                if (loads[i] != null)
                {
                    loads[i].LoadGraphics();
                    loads[i] = null;
                }
            }
        }
    }

    float DistToPlayer()
    {
        Vector3 p = Player.pos - new Vector3(index1 * size, 0, index2 * size);
        return p.magnitude;
    }

    static void Run()
    {
        Chunk[] loaderers = new Chunk[amountToLoad];
        for (int i = 0; i < allChunks.Count; i++)
        {
            Chunk c = allChunks[i];
            if (!c.loading && c.ShouldMove())
            {
                for (int ii = 0; ii < amountToLoad; ii++)
                {
                    Chunk cc = loaderers[ii];
                    if (cc == null)
                    {
                        loaderers[ii] = c;
                    }
                    else if (c.DistToPlayer() < cc.DistToPlayer())
                    {
                        loaderers[ii] = c;
                        c = cc;
                    }
                }
            }
        }
        for (int i = 0; i < amountToLoad; i++)
        {
            Chunk c = loaderers[i];
            if (c != null)
            {
                c.Unload();
                Vector2Int v = c.GetNewIndex();
                c.Load(v.x, v.y);
            }
        }
        Chunk[] loaders = new Chunk[amountToLoad];
        for (int i = 0; i < chunks.Count; i++)
        {
            Chunk c = chunks[i];
            for (int ii = 0; ii < amountToLoad; ii++)
            {
                Chunk cc = loaders[ii];
                if (cc == null)
                {
                    loaders[ii] = c;
                }
                else if (c.DistToPlayer() < cc.DistToPlayer())
                {
                    loaders[ii] = c;
                    c = cc;
                }
            }
        }
        for (int i = 0; i < amountToLoad; i++)
        {
            Chunk c = loaders[i];
            if (c != null && numThreads < amountOfThreads)
            {
                c.StartHeightThread();
            }
        }
        for (int i = 0; i < loadedChunks.Count; i++)
        {
            Chunk c = loadedChunks[i];
            if (c.loaded)
            {
                for (int ii = 0; ii < amountToLoad; ii++)
                {
                    Chunk cc = loads[ii];
                    if (cc == null)
                    {
                        loads[ii] = c;
                    }
                    else if (c.DistToPlayer() < cc.DistToPlayer())
                    {
                        loads[ii] = c;
                        c = cc;
                    }
                }
            }
        }
        for (int i = 0; i < amountToLoad; i++)
        {
            Chunk c = loads[i];
            if (c != null)
            {
                loadedChunks.Remove(c);
            }
        }
        done = true;
    }

    bool ShouldMove()
    {
        Vector3 p = Player.pos - new Vector3(index1 * size, 0, index2 * size);
        if (!(Mathf.Abs(p.x) <= loadSize/2*size && Mathf.Abs(p.z) <= loadSize / 2 * size))
        {
            return true;
        }
        return false;
    }

    Vector2Int GetNewIndex()
    {
        Vector2Int v = new Vector2Int(index1, index2);
        Vector3 p = Player.pos - new Vector3(index1 * size, 0, index2 * size);
        if (p.x > loadSize/2*size)
        {
            v.x += loadSize;
        }
        if (p.x < -loadSize / 2 * size)
        {
            v.x -= loadSize;
        }
        if (p.z > loadSize / 2 * size)
        {
            v.y += loadSize;
        }
        if (p.z < -loadSize / 2 * size)
        {
            v.y -= loadSize;
        }
        return v;
    }

    void Unload()
    {
        chunks.Remove(this);
        loadedChunks.Remove(this);
        loaded = false;
    }
    
    void StartHeightThread()
    {
        Thread thread = new Thread(LoadChunk);
        numThreads++;
        loading = true;
        chunks.Remove(this);
        loadedChunks.Add(this);
        thread.Start();
    }

    void LoadChunk()
    {
        heights = new float[size + 1, size + 1];
        vertices.Clear();
        normals.Clear();
        indices.Clear();
        colors.Clear();
        for (int i = 0; i < size + 1; i++)
        {
            for (int ii = 0; ii < size + 1; ii++)
            {
                float height = worldNoise.GetHeight(index1 * size + i, index2 * size + ii);
                heights[i, ii] = height;
                vertices.Add(new Vector3(i, height, ii));
                normals.Add(new Vector3());
            }
        }
        for (int i = 0; i < size; i++)
        {
            for (int ii = 0; ii < size; ii++)
            {
                Vector3 U = vertices[i * (size + 1) + ii] - vertices[i * (size + 1) + ii + 1];
                Vector3 V = vertices[i * (size + 1) + ii] - vertices[(i + 1) * (size + 1) + ii];
                Vector3 normal = new Vector3(U.y * V.z - U.z * V.y, U.z * V.x - U.x * V.z, U.x * V.y - U.y * V.x);
                normals[i * (size + 1) + ii] += normal;
                normals[(i + 1) * (size + 1) + ii] += normal;
                normals[i * (size + 1) + ii + 1] += normal;
                U = vertices[(i + 1) * (size + 1) + ii + 1] - vertices[(i + 1) * (size + 1) + ii];
                V = vertices[(i + 1) * (size + 1) + ii + 1] - vertices[i * (size + 1) + ii + 1];
                normal = new Vector3(U.y * V.z - U.z * V.y, U.z * V.x - U.x * V.z, U.x * V.y - U.y * V.x);
                normals[(i + 1) * (size + 1) + ii + 1] += normal;
                normals[(i + 1) * (size + 1) + ii] += normal;
                normals[i * (size + 1) + ii + 1] += normal;
            }
        }
        for (int i = 0; i < size + 1; i++)
        {
            for (int ii = 0; ii < size + 1; ii++)
            {
                normals[i * (size + 1) + ii] = Vector3.Normalize(normals[i * (size + 1) + ii]);
                if (vertices[i*(size+1)+ii].y < 0 || normals[i*(size+1)+ii].y < 0.9)
                {
                    colors.Add(new Color(1, 0, 0, 0));
                }else
                {
                    colors.Add(new Color());
                }
            }
        }
        for (int i = 0; i < size; i++)
        {
            for (int ii = 0; ii < size; ii++)
            {
                indices.Add((i) * (size + 1) + ii);
                indices.Add((i) * (size + 1) + ii+1);
                indices.Add((i+1) * (size + 1) + ii);
                indices.Add((i+1) * (size + 1) + ii);
                indices.Add((i) * (size + 1) + ii+1);
                indices.Add((i+1) * (size + 1) + ii+1);
            }
        }
        numThreads--;
        loaded = true;
        loading = false;
    }

    void LoadGraphics()
    {
        Debug.Log("Pytoom");
        Mesh m = mf.mesh;
        m.Clear();
        m.vertices = vertices.ToArray();
        m.triangles = indices.ToArray();
        m.normals = normals.ToArray();
        m.colors = colors.ToArray();
        m.RecalculateBounds();
        m.UploadMeshData(false);
        mc.sharedMesh = m;
        transform.position = new Vector3(index1 * size, 0, index2 * size);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
