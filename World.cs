using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{

    public Chunk chunk;
    int loadSize = 8;
    int size = 64;

    // Start is called before the first frame update
    void Start()
    {
        Chunk.size = size;
        Chunk.loadSize = loadSize;
        for (int i = 0; i < loadSize; i++)
        {
            for (int ii = 0; ii < loadSize; ii++)
            {
                Chunk c = Instantiate(chunk);
                c.Load(-loadSize / 2 + i, -loadSize / 2 + ii);
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        Chunk.LoadChunks();
    }
}

public class worldNoise
{

    public static float GetHeight(int x, int z)
    {
        double max = 0;
        double amount = 1;
        double height = 0;
        for (int i = 0; i < 8; i++)
        {
            height += ValueCoherentNoise3D(x / amount, z / amount, i, 0) * amount;
            max += amount;
            amount *= 2;
        }
        height /= max;
        height = height < 0 ? height * 0.1 : height * height;
        height *= max;
        return (float)height;
    }

    const int X_NOISE_GEN = 1619;
    const int Y_NOISE_GEN = 31337;
    const int Z_NOISE_GEN = 6971;
    const int SEED_NOISE_GEN = 1013;
    const int SHIFT_NOISE_GEN = 8;

    public static int IntValueNoise3D(int x, int y, int z, int seed)
    {
        int n = (
            X_NOISE_GEN * x
            + Y_NOISE_GEN * y
            + Z_NOISE_GEN * z
            + SEED_NOISE_GEN * seed)
        & 0x7fffffff;
        n = (n >> 13) ^ n;
        return (n * (n * n * 60493 + 19990303) + 1376312589) & 0x7fffffff;
    }
    public static double ValueCoherentNoise3D(double x, double y, double z, int seed)
    {
        int x0 = (x > 0.0 ? (int)x : (int)x - 1);
        int x1 = x0 + 1;
        int y0 = (y > 0.0 ? (int)y : (int)y - 1);
        int y1 = y0 + 1;
        int z0 = (z > 0.0 ? (int)z : (int)z - 1);
        int z1 = z0 + 1;
        double xs = 0;
        double ys = 0;
        double zs = 0;

        xs = SCurve5(x - (double)x0);
        ys = SCurve5(y - (double)y0);
        zs = SCurve5(z - (double)z0);
        double n0 = 0;
        double n1 = 0;
        double ix0 = 0;
        double ix1 = 0;
        double iy0 = 0;
        double iy1 = 0;
        n0 = ValueNoise3D(x0, y0, z0, seed);
        n1 = ValueNoise3D(x1, y0, z0, seed);
        ix0 = LinearInterp(n0, n1, xs);
        n0 = ValueNoise3D(x0, y1, z0, seed);
        n1 = ValueNoise3D(x1, y1, z0, seed);
        ix1 = LinearInterp(n0, n1, xs);
        iy0 = LinearInterp(ix0, ix1, ys);
        n0 = ValueNoise3D(x0, y0, z1, seed);
        n1 = ValueNoise3D(x1, y0, z1, seed);
        ix0 = LinearInterp(n0, n1, xs);
        n0 = ValueNoise3D(x0, y1, z1, seed);
        n1 = ValueNoise3D(x1, y1, z1, seed);
        ix1 = LinearInterp(n0, n1, xs);
        iy1 = LinearInterp(ix0, ix1, ys);
        return LinearInterp(iy0, iy1, zs);
    }

    public static double interpQuad(double x, double y, double v1, double v2, double v3, double v4)
    {
        double xs = 0;
        double ys = 0;

        xs = x;// SCurve5 (x);
        ys = y;// SCurve5 (y);
        double n0 = 0;
        double n1 = 0;
        double ix0 = 0;
        double ix1 = 0;
        n0 = v1;
        n1 = v2;
        ix0 = LinearInterp(n0, n1, xs);
        n0 = v3;
        n1 = v4;
        ix1 = LinearInterp(n0, n1, xs);
        return LinearInterp(ix0, ix1, ys);
    }
    public static double interpQuadS(double x, double y, double v1, double v2, double v3, double v4)
    {
        double xs = 0;
        double ys = 0;

        xs = SCurve5(x);
        ys = SCurve5(y);
        double n0 = 0;
        double n1 = 0;
        double ix0 = 0;
        double ix1 = 0;
        n0 = v1;
        n1 = v2;
        ix0 = LinearInterp(n0, n1, xs);
        n0 = v3;
        n1 = v4;
        ix1 = LinearInterp(n0, n1, xs);
        return LinearInterp(ix0, ix1, ys);
    }

    public static double ValueNoise3D(int x, int y, int z, int seed)
    {
        return 1.0 - ((double)IntValueNoise3D(x, y, z, seed) / 1073741824.0);
    }

    public static double LinearInterp(double n0, double n1, double a)
    {
        return ((1.0 - a) * n0) + (a * n1);
    }

    public static double SCurve5(double a)
    {
        double a3 = a * a * a;
        double a4 = a3 * a;
        double a5 = a4 * a;
        return (6.0 * a5) - (15.0 * a4) + (10.0 * a3);
    }

}