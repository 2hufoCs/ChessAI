using System;
using UnityEngine;

public class ArrayPerformanceTest : MonoBehaviour
{
    void Start()
    {
        // PerformanceTest();
    }

    private void PerformanceTest()
    {
        var startTime = DateTime.Now;
        Test1D(new byte[1000 * 1000 * 3]);
        Debug.Log("Total Time taken 1D = " + (DateTime.Now - startTime));

        startTime = DateTime.Now;
        Test2D(new byte[1000 * 1000, 3], 1000 * 1000);
        Debug.Log("Total Time taken 2D = " + (DateTime.Now - startTime));
    }

    public static void Test1D(byte[] array)
    {
        for (int c = 0; c < 2500; c++)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = 10;
            }
        }
    }    
    
    public static void Test2D(byte[,] array, int w)
    {
        for (int c = 0; c < 2500; c++)
        {
            for (int i = 0; i < w; i++)
            {
                    array[i, 0] = 10;
                    array[i, 1] = 10;
                    array[i, 2] = 10;
            }
        }
    }

    public static void Test3D(byte[,,] array, int w, int h)
    {
        for (int c = 0; c < 2500; c++)
        {
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    array[i, j, 0] = 10;
                    array[i, j, 1] = 10;
                    array[i, j, 2] = 10;
                }
            }
        }
    }
}
