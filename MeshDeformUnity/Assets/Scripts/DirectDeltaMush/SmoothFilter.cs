//BSD 2 - Clause License

//Copyright(c) 2022, K. S. Ernest (iFire) Lee
//Copyright(c) 2021, PacosLelouch
//All rights reserved.

//Redistribution and use in source and binary forms, with or without
//modification, are permitted provided that the following conditions are met:

//1.Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.

//2. Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.

//THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
//AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
//IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
//FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
//DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
//SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
//CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
//OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
//OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
	Laplacian Smooth Filter, HC-Smooth Filter
 
	Based on MarkGX SmoothFilter.cs (Jan 2011)
*/
public class
SmoothFilter // : MonoBehaviour
{
    public static Vector3[]
    laplacianFilter(Vector3[] sv, int[,] adjacencyMatrix)
    {
        Vector3[] wv = new Vector3[sv.Length];
        int maxNeighbors = adjacencyMatrix.GetLength(1);

        for (int vi = 0; vi < sv.Length; vi++)
        {
            float dx = 0.0f;
            float dy = 0.0f;
            float dz = 0.0f;

            int count = 0;
            for (int j = 0; j < maxNeighbors; j++)
            {
                var i = adjacencyMatrix[vi, j];
                if (i < 0)
                {
                    break;
                }

                dx += sv[i].x - sv[vi].x;
                dy += sv[i].y - sv[vi].y;
                dz += sv[i].z - sv[vi].z;
                count++;
            }

            wv[vi].x = sv[vi].x + dx / count;
            wv[vi].y = sv[vi].y + dy / count;
            wv[vi].z = sv[vi].z + dz / count;
        }

        return wv;
    }

    public static Vector3[]
    distanceWeightedLaplacianFilter(Vector3[] sv, int[,] adjacencyMatrix)
    {
        Vector3[] wv = new Vector3[sv.Length];

        int maxNeighbors = adjacencyMatrix.GetLength(1);

        for (int vi = 0; vi < sv.Length; vi++)
        {
            float dx = 0.0f;
            float dy = 0.0f;
            float dz = 0.0f;

            float totalWeight = 0;
            for (int j = 0; j < maxNeighbors; j++)
            {
                var i = adjacencyMatrix[vi, j];
                if (i == vi)
                {
                    continue;
                }

                if (i < 0)
                {
                    break;
                }

                float x = sv[i].x - sv[vi].x;
                float y = sv[i].y - sv[vi].y;
                float z = sv[i].z - sv[vi].z;
                float sqr = x * x + y * y + z * z;
                if (sqr < 1e-8f)
                {
                    continue;
                }

                float w = 1.0f / Mathf.Sqrt(sqr);
                dx += x * w;
                dy += y * w;
                dz += z * w;
                totalWeight += w;
            }

            if (totalWeight > 1e-4f)
            {
                wv[vi].x = sv[vi].x + dx / totalWeight;
                wv[vi].y = sv[vi].y + dy / totalWeight;
                wv[vi].z = sv[vi].z + dz / totalWeight;
            }
            else
            {
                wv[vi].x = sv[vi].x;
                wv[vi].y = sv[vi].y;
                wv[vi].z = sv[vi].z;
            }
        }

        return wv;
    }

    public static float[,]
    distanceWeightedLaplacianFilter(
        Vector3[] sv,
        float[,] interpolants,
        int[,] adjacencyMatrix
    )
    {
        int maxChannels = interpolants.GetLength(1);
        int maxNeighbors = adjacencyMatrix.GetLength(1);

        Debug.Assert(sv.Length == interpolants.GetLength(0));
        var res = new float[sv.Length, maxChannels];

        for (int vi = 0; vi < sv.Length; vi++)
        {
            // C# arrays always initialized to 0
            for (int ch = 0; ch < maxChannels; ch++)
            {
                res[vi, ch] = 0.0f;
            }

            float totalWeight = 0.0f;
            for (int j = 0; j < maxNeighbors; j++)
            {
                var i = adjacencyMatrix[vi, j];
                if (i == vi)
                {
                    continue;
                }

                if (i < 0)
                {
                    break;
                }

                float x = sv[i].x - sv[vi].x;
                float y = sv[i].y - sv[vi].y;
                float z = sv[i].z - sv[vi].z;
                float w = 1.0f / Mathf.Sqrt(x * x + y * y + z * z);
                for (int ch = 0; ch < maxChannels; ch++)
                {
                    res[vi, ch] += (interpolants[i, ch] - interpolants[vi, ch]) * w;
                }

                totalWeight += w;
            }

            for (int ch = 0; ch < maxChannels; ch++)
            {
                res[vi, ch] /= totalWeight;
                res[vi, ch] += interpolants[vi, ch];
            }
        }

        return res;
    }

    /*
		HC (Humphrey’s Classes) Smooth Algorithm - Reduces Shrinkage of Laplacian Smoother
 
		Where sv - original points
				pv - previous points,
				alpha [0..1] influences previous points pv, e.g. 0
				beta  [0..1] e.g. > 0.5
	*/
    public static Vector3[]
    hcFilter(
        Vector3[] sv,
        Vector3[] pv,
        int[,] adjacencyMatrix,
        float alpha,
        float beta
    )
    {
        Vector3[] wv = new Vector3[sv.Length];
        Vector3[] bv = new Vector3[sv.Length];

        // Perform Laplacian Smooth
        wv = laplacianFilter(sv, adjacencyMatrix);

        // Compute Differences
        for (int i = 0; i < wv.Length; i++)
        {
            bv[i].x = wv[i].x - (alpha * sv[i].x + (1 - alpha) * sv[i].x);
            bv[i].y = wv[i].y - (alpha * sv[i].y + (1 - alpha) * sv[i].y);
            bv[i].z = wv[i].z - (alpha * sv[i].z + (1 - alpha) * sv[i].z);
        }

        int maxNeighbors = adjacencyMatrix.GetLength(1);

        for (int j = 0; j < bv.Length; j++)
        {
            // Find the bv neighboring vertices
            float dx = 0.0f;
            float dy = 0.0f;
            float dz = 0.0f;

            // Add the vertices and divide by the number of vertices
            int count = 0;
            for (int k = 0; k < maxNeighbors; k++)
            {
                var i = adjacencyMatrix[j, k];
                if (i < 0)
                {
                    break;
                }

                dx += bv[i].x;
                dy += bv[i].y;
                dz += bv[i].z;
                ++count;
            }

            if (count == 0)
            {
                Debug.Log("Empty!");
            }

            wv[j].x -= beta * bv[j].x + ((1 - beta) / count) * dx;
            wv[j].y -= beta * bv[j].y + ((1 - beta) / count) * dy;
            wv[j].z -= beta * bv[j].z + ((1 - beta) / count) * dz;
        }

        return wv;
    }
}
