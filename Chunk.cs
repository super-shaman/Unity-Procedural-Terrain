using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.IO;
using System;


public class Chunk : MonoBehaviour
{

    public MeshFilter mf;
    public static int size;
    public static int loadSize;
    float[,] heights;
    List<Vector3> vertices = new List<Vector3>();
    List<int> indices = new List<int>();
    int index1;
    int index2;
    static List<Chunk> chunks = new List<Chunk>();
    static List<Chunk> loadedChunks = new List<Chunk>();
    bool loaded;
    bool loading;


    public void Load(int index1, int index2)
    {
        this.index1 = index1;
        this.index2 = index2;
        if (!chunks.Contains(this))
        {
            chunks.Add(this);
        }
    }

    static bool done;
    static bool running;
    static int numThreads;
    static int amountOfThreads = Environment.ProcessorCount;
    static int amountToLoad = 8;
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
        return Mathf.Sqrt(index1 * index1 + index2 * index2);
    }

    static void Run()
    {
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
        for (int i = 0; i < size + 1; i++)
        {
            for (int ii = 0; ii < size + 1; ii++)
            {
                float height = worldNoise.GetHeight(index1 * size + i, index2 * size + ii);
                heights[i, ii] = height;
                vertices.Add(new Vector3(i, height, ii));
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
    }

    void LoadGraphics()
    {
        Debug.Log("Pytoom");
        Mesh m = mf.mesh;
        m.vertices = vertices.ToArray();
        m.triangles = indices.ToArray();
        m.RecalculateNormals();
        m.RecalculateBounds();
        m.UploadMeshData(false);
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
