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

public class TestDDMPrecomputation : MonoBehaviour
{
    public string
        howToTest =
            "Press 't' to trigger the test. Make sure to pause the profiling quickly!";

    public ComputeShader precomputeShader;

    public bool testCPU = true;

    public bool testGPU = true;

    public int iterations = 2;

    public float translationSmooth = 0.9f;

    public float rotationSmooth = 0.9f;

    public float adjacencyMatchingVertexTolerance = 1e-4f;

    internal Mesh mesh;

    internal SkinnedMeshRenderer skin;

    internal int[,] adjacencyMatrix;

    internal ComputeBuffer verticesCB; // float3

    internal ComputeBuffer normalsCB; // float3

    internal ComputeBuffer weightsCB; // float4 + int4

    internal ComputeBuffer bonesCB; // float4x4

    internal ComputeBuffer omegasCB; // float4x4 * 4

    internal ComputeBuffer outputCB; // float3 + float3

    internal DDMUtilsIterative.OmegaWithIndex[,] omegaWithIdxs;

    //////
    internal ComputeBuffer laplacianCB;

    //////laplacianCB
    internal Material ductTapedMaterial;

    // Start is called before the first frame update
    void Start()
    {
        Debug
            .Assert(SystemInfo.supportsComputeShaders &&
            precomputeShader != null);

        if (precomputeShader)
        {
            precomputeShader = Instantiate(precomputeShader);
        }
        skin = GetComponent<SkinnedMeshRenderer>();
        mesh = skin.sharedMesh;

        BoneWeight[] bws = mesh.boneWeights;

        int vCount = mesh.vertexCount;
        int bCount = skin.bones.Length;

        verticesCB = new ComputeBuffer(vCount, 3 * sizeof(float));
        normalsCB = new ComputeBuffer(vCount, 3 * sizeof(float));
        weightsCB =
            new ComputeBuffer(vCount, 4 * sizeof(float) + 4 * sizeof(int));
        bonesCB = new ComputeBuffer(bCount, 16 * sizeof(float));
        verticesCB.SetData(mesh.vertices);
        normalsCB.SetData(mesh.normals);
        weightsCB.SetData (bws);

        omegasCB =
            new ComputeBuffer(vCount * DDMSkinnedMeshGPUVar0.maxOmegaCount,
                (10 * sizeof(float) + sizeof(int)));
        laplacianCB =
            new ComputeBuffer(vCount * DDMSkinnedMeshGPUVar0.maxOmegaCount,
                (sizeof(int) + sizeof(float)));

        DDMUtilsGPU.isTestingPerformance = true;
    }

    void PrecomputationAdjacencyMatrix()
    {
        UnityEngine
            .Profiling
            .Profiler
            .BeginSample("PrecomputationAdjacencyMatrix");
        adjacencyMatrix =
            DDMSkinnedMeshGPUVar0
                .GetCachedAdjacencyMatrix(mesh,
                adjacencyMatchingVertexTolerance);
        UnityEngine.Profiling.Profiler.EndSample();
    }

    void CPU_Precomputation()
    {
        System.GC.Collect();
        int bCount = skin.bones.Length;
        Vector3[] vertices = mesh.vertices;
        BoneWeight[] weights = mesh.boneWeights;

        UnityEngine.Profiling.Profiler.BeginSample("CPU_Precomputation");
        DDMUtilsGPU.IndexWeightPair[,] laplacianWithIndex =
            DDMUtilsGPU.ComputeLaplacianWithIndexFromAdjacency(adjacencyMatrix);
        omegaWithIdxs =
            DDMUtilsGPU
                .ComputeOmegasFromLaplacian(vertices,
                laplacianWithIndex,
                weights,
                bCount,
                iterations,
                translationSmooth);
        UnityEngine.Profiling.Profiler.EndSample();
    }

    void GPU_Precomputation()
    {
        System.GC.Collect();
        int bCount = skin.bones.Length;
        UnityEngine.Profiling.Profiler.BeginSample("GPU_Precomputation");

        DDMUtilsGPU
            .ComputeLaplacianCBFromAdjacency(ref laplacianCB,
            precomputeShader,
            adjacencyMatrix);
        DDMUtilsGPU
            .ComputeOmegasCBFromLaplacianCB(ref omegasCB,
            precomputeShader,
            verticesCB,
            laplacianCB,
            weightsCB,
            bCount,
            iterations,
            translationSmooth);
        UnityEngine.Profiling.Profiler.EndSample();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (Input.GetKey("t"))
        {
            if (adjacencyMatrix == null)
            {
                Debug.Log("Test precomputation adjacency matrix.");
                PrecomputationAdjacencyMatrix();
            }
            if (testGPU)
            {
                testGPU = false;
                Debug.Log("Test GPU precomputation.");
                GPU_Precomputation();
            }
            if (testCPU)
            {
                testCPU = false;
                Debug.Log("Test CPU precomputation");
                CPU_Precomputation();
            }
        }
    }

    private void OnDestroy()
    {
        verticesCB.Release();
        normalsCB.Release();
        weightsCB.Release();
        bonesCB.Release();

        omegasCB.Release();
        laplacianCB.Release();
    }
}
