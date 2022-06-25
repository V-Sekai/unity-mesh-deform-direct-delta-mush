using System;
using System.IO;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using MathNet.Numerics.LinearAlgebra.Solvers;
using UnityEditor;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine.Profiling;

/*
	Vertex adjacency functions
*/
public class
MeshUtils // : MonoBehaviour
{
    const float EPSILON = 1e-8f;

    // Collect information about vertex adjacency into matrix
    public static int[,]
    BuildAdjacencyMatrix(
        Vector3[] v,
        int[] t,
        int maxNeighbors,
        float minSqrDistance = EPSILON
    )
    {
        Profiler.BeginSample("BuildAdjacencyMatrix");
        var adj = new int[v.Length, maxNeighbors];
        for (int i = 0; i < adj.GetLength(0); ++i)
            for (int j = 0; j < adj.GetLength(1); ++j) adj[i, j] = -1;

        if (minSqrDistance == 0.0f)
        {
            for (int tri = 0; tri < t.Length; tri = tri + 3)
            {
                AddEdgeToAdjacencyMatrixDirect(ref adj, t[tri], t[tri + 1]);
                AddEdgeToAdjacencyMatrixDirect(ref adj, t[tri], t[tri + 2]);
                AddEdgeToAdjacencyMatrixDirect(ref adj, t[tri + 1], t[tri + 2]);
            }
        }
        else
        {
            int[] mapToUnique = MapVerticesToUniquePositions(v, minSqrDistance);

            for (int tri = 0; tri < t.Length; tri = tri + 3)
            {
                AddEdgeToAdjacencyMatrix(ref adj,
                mapToUnique,
                t[tri],
                t[tri + 1]);
                AddEdgeToAdjacencyMatrix(ref adj,
                mapToUnique,
                t[tri],
                t[tri + 2]);
                AddEdgeToAdjacencyMatrix(ref adj,
                mapToUnique,
                t[tri + 1],
                t[tri + 2]);
            }

            BroadcastAdjacencyFromUniqueToAllVertices(ref adj, mapToUnique);
        }

        Profiler.EndSample();
        return adj;
    }

    // Find vertices that approximately share the same positions
    // Returns array of indices pointing to the first occurance of particular position in the vertex array
    public static int[]
    MapVerticesToUniquePositions(Vector3[] v, float minSqrDistance = EPSILON)
    {
        Profiler.BeginSample("MapVerticesToUniquePositions");

        var mapToUnique = new int[v.Length];
        for (int i = 0; i < mapToUnique.Length; ++i) mapToUnique[i] = -1;

        for (int i = 0; i < v.Length; i++)
            for (int j = i; j < v.Length; j++)
                if (
                    mapToUnique[j] == -1 // skip, if already pointing to unique position
                )
                {
                    var u = mapToUnique[i];
                    if (u == -1) u = i;

                    var dx = v[u].x - v[j].x;
                    var dy = v[u].y - v[j].y;
                    var dz = v[u].z - v[j].z;
                    if (
                        dx * dx + dy * dy + dz * dz <= minSqrDistance // 687ms
                    )
                    {
                        if (mapToUnique[i] == -1) mapToUnique[i] = u; // found new unique vertex
                        mapToUnique[j] = u;
                    }
                }

        for (int i = 0; i < v.Length; i++) Debug.Assert(mapToUnique[i] != -1);

        Profiler.EndSample();
        return mapToUnique;
    }

    private static void AddVertexToAdjacencyMatrix(
        ref int[,] adjacencyMatrix,
        int from,
        int to
    )
    {
        var maxNeighbors = adjacencyMatrix.GetLength(1);
        for (int i = 0; i < maxNeighbors; i++)
        {
            if (adjacencyMatrix[from, i] == to) break;

            if (adjacencyMatrix[from, i] == -1)
            {
                adjacencyMatrix[from, i] = to;
                break;
            }
        }
    }

    private static void AddEdgeToAdjacencyMatrixDirect(
        ref int[,] adjacencyMatrix,
        int v0,
        int v1
    )
    {
        AddVertexToAdjacencyMatrix(ref adjacencyMatrix, v0, v1);
        AddVertexToAdjacencyMatrix(ref adjacencyMatrix, v1, v0);
    }

    private static void AddEdgeToAdjacencyMatrix(
        ref int[,] adjacencyMatrix,
        int[] mapToUnique,
        int v0,
        int v1
    )
    {
        var u0 = mapToUnique[v0];
        var u1 = mapToUnique[v1];

        AddEdgeToAdjacencyMatrixDirect(ref adjacencyMatrix, u0, u1);
    }

    private static void BroadcastAdjacencyFromUniqueToAllVertices(
        ref int[,] adjacencyMatrix,
        int[] mapToUnique
    )
    {
        Profiler.BeginSample("BroadcastAdjacencyFromUniqueToAllVertices");

        var maxNeighbors = adjacencyMatrix.GetLength(1);
        Debug.Assert(adjacencyMatrix.GetLength(0) == mapToUnique.Length);

        for (int i = 0; i < mapToUnique.Length; ++i)
        {
            var u = mapToUnique[i];
            if (u == i) continue;

            Debug.Assert(adjacencyMatrix[i, 0] == -1);
            for (int j = 0; j < maxNeighbors && adjacencyMatrix[u, j] != -1; ++j
            )
                adjacencyMatrix[i, j] = adjacencyMatrix[u, j];
        }

        Profiler.EndSample();
    }

    /// <summary>
    /// Build Laplacian matrix from adjacent matrix.
    /// </summary>
    /// <param name="vCount">Vertex Count</param>
    /// <param name="adjacencyMatrix">Adjacent Matrix</param>
    /// <param name="normalized">Normalize Laplacian matrix if true</param>
    /// <param name="weightedSmooth">Boolean to control Laplacian matrix. Implement for "false" first.</param>
    /// <returns>Laplacian matrix: MathNet.Numerics.LinearAlgebra.Single.SparseMatrix</returns>
    public static SparseMatrix
    BuildLaplacianMatrixFromAdjacentMatrix(
        int vCount,
        int[,] adjacencyMatrix,
        bool normalize = true,
        bool weightedSmooth = false
    )
    {
        Profiler.BeginSample("BuildLaplacianMatrixFromAdjacentMatrix");
        SparseMatrix lapl = new SparseMatrix(vCount, vCount);
        int maxNeighbors = adjacencyMatrix.GetLength(1);

        for (int vi = 0; vi < vCount; vi++)
        {
            int viDeg = 0;
            for (int j = 0; j < maxNeighbors; j++)
            {
                int vj = adjacencyMatrix[vi, j];
                if (vj < 0)
                {
                    break;
                }
                ++viDeg;
                lapl.At(vi, vj, -1);
            }

            if (!normalize)
            {
                lapl.At(vi, vi, viDeg);
            }
            else
            {
                for (int j = 0; j < maxNeighbors; j++)
                {
                    int vj = adjacencyMatrix[vi, j];
                    if (vj < 0)
                    {
                        break;
                    }
                    lapl.At(vi, vj, lapl.At(vi, vj) / viDeg);
                }
                lapl.At(vi, vi, 1.0f);
            }
        }

        Profiler.EndSample();
        return lapl;
    }

    /// <summary>
    /// Build smooth matrix from laplacian. (So slow...)
    /// </summary>
    /// <param name="lapl">Laplacian matrix</param>
    /// <param name="smoothLambda">Lambda parameter</param>
    /// <param name="iteration">iteration</param>
    /// <returns>Smooth matrix: MathNet.Numerics.LinearAlgebra.Single.SparseMatrix</returns>
    public static SparseMatrix
    BuildSmoothMatrixFromLaplacian(
        SparseMatrix lapl,
        float smoothLambda,
        int iteration
    )
    {
        Profiler.BeginSample("BuildSmoothMatrixFromLaplacian");
        int vCount = lapl.ColumnCount;
        SparseMatrix identity = SparseMatrix.CreateIdentity(vCount);
        SparseMatrix a =
            iteration == 0
                ? identity
                : (identity - (smoothLambda / iteration) * lapl);

        SparseMatrix smooth = SparseMatrix.CreateIdentity(vCount);
        SparseMatrix smoothNext = new SparseMatrix(vCount);
        for (int i = 0; i < iteration; ++i)
        {
            smooth.Multiply(a, smoothNext);
            smooth = smoothNext;
        }
        Profiler.EndSample();
        return smooth;
    }
}


//[ExecuteInEditMode]
public class DDMSkinnedMeshGPUVar0 : MonoBehaviour
{

    public int iterations = 30;

    public float smoothLambda = 0.9f;

    public bool useCompute = true;

    public float adjacencyMatchingVertexTolerance = 1e-4f;

    public enum DebugMode
    {
        Off,
        CompareWithLinearBlend
    }

    public DebugMode debugMode = DebugMode.Off;

    protected bool actuallyUseCompute
    {
        get
        {
            return useCompute && debugMode != DebugMode.CompareWithLinearBlend;
        }
    }

    protected int vCount;

    protected int bCount;

    protected Mesh mesh;

    protected Mesh meshForCPUOutput;

    protected SkinnedMeshRenderer skin;

    protected struct DeformedMesh
    {
        public DeformedMesh(int vertexCount_)
        {
            vertexCount = vertexCount_;
            vertices = new Vector3[vertexCount];
            normals = new Vector3[vertexCount];
            deltaV = new Vector3[vertexCount];
            deltaN = new Vector3[vertexCount];
        }

        public int vertexCount;

        public Vector3[] vertices;

        public Vector3[] normals;

        public Vector3[] deltaV;

        public Vector3[] deltaN;
    }

    protected DeformedMesh deformedMesh;

    protected int[,] adjacencyMatrix;

    // Compute
    [HideInInspector]
    public ComputeShader precomputeShader;

    [HideInInspector]
    public Shader ductTapedShader;

    [HideInInspector]
    public ComputeShader computeShader;

    protected int deformKernel;

    protected int computeThreadGroupSizeX;

    protected ComputeBuffer verticesCB; // float3

    protected ComputeBuffer normalsCB; // float3

    protected ComputeBuffer weightsCB; // float4 + int4

    protected ComputeBuffer bonesCB; // float4x4

    protected ComputeBuffer omegasCB; // float4x4 * 4

    protected ComputeBuffer outputCB; // float3 + float3

    protected ComputeBuffer laplacianCB;

    protected Material ductTapedMaterial;

    public const int maxOmegaCount = 32;

    protected void InitBase()
    {
        Debug
            .Assert(SystemInfo.supportsComputeShaders &&
            precomputeShader != null);

        if (precomputeShader)
        {
            precomputeShader = Instantiate(precomputeShader);
        }
        if (computeShader)
        {
            computeShader = Instantiate(computeShader);
        }
        skin = GetComponent<SkinnedMeshRenderer>();
        mesh = skin.sharedMesh;
        meshForCPUOutput = Instantiate(mesh);

        deformedMesh = new DeformedMesh(mesh.vertexCount);

        adjacencyMatrix =
            GetCachedAdjacencyMatrix(mesh, adjacencyMatchingVertexTolerance);

        vCount = mesh.vertexCount;
        bCount = skin.bones.Length;

        BoneWeight[] bws = mesh.boneWeights;

        // Compute
        verticesCB = new ComputeBuffer(vCount, 3 * sizeof(float));
        normalsCB = new ComputeBuffer(vCount, 3 * sizeof(float));
        weightsCB =
            new ComputeBuffer(vCount, 4 * sizeof(float) + 4 * sizeof(int));
        bonesCB = new ComputeBuffer(bCount, 16 * sizeof(float));
        verticesCB.SetData(mesh.vertices);
        normalsCB.SetData(mesh.normals);
        weightsCB.SetData(bws);

        omegasCB =
            new ComputeBuffer(vCount * maxOmegaCount,
                (10 * sizeof(float) + sizeof(int)));

        outputCB = new ComputeBuffer(vCount, 6 * sizeof(float));

        laplacianCB =
            new ComputeBuffer(vCount * maxOmegaCount,
                (sizeof(int) + sizeof(float)));
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
            smoothLambda);

        if (computeShader && ductTapedShader)
        {
            deformKernel = computeShader.FindKernel("DeformMesh");
            computeShader.SetBuffer(deformKernel, "Vertices", verticesCB);
            computeShader.SetBuffer(deformKernel, "Normals", normalsCB);
            computeShader.SetBuffer(deformKernel, "Bones", bonesCB);
            computeShader.SetBuffer(deformKernel, "Output", outputCB);
            computeShader.SetInt("VertexCount", vCount);

            uint
                threadGroupSizeX,
                threadGroupSizeY,
                threadGroupSizeZ;
            computeShader
                .GetKernelThreadGroupSizes(deformKernel,
                out threadGroupSizeX,
                out threadGroupSizeY,
                out threadGroupSizeZ);
            computeThreadGroupSizeX = (int)threadGroupSizeX;

            ductTapedMaterial = new Material(ductTapedShader);
            ductTapedMaterial.CopyPropertiesFromMaterial(skin.sharedMaterial);
        }
        else
        {
            useCompute = false;
        }
    }

    protected void ReleaseBase()
    {
        if (verticesCB == null)
        {
            return;
        }
        laplacianCB.Release();

        verticesCB.Release();
        normalsCB.Release();
        weightsCB.Release();
        bonesCB.Release();
        omegasCB.Release();
        outputCB.Release();
    }

    protected void UpdateBase()
    {
        bool compareWithSkinning =
            debugMode == DebugMode.CompareWithLinearBlend;
        if (!compareWithSkinning)
        {
            if (actuallyUseCompute)
                UpdateMeshOnGPU();
            else
                UpdateMeshOnCPU();
        }
        if (compareWithSkinning)
            DrawVerticesVsSkin();
        else
            DrawMesh();

        skin.enabled = compareWithSkinning;
    }


    #region Adjacency matrix cache
    [System.Serializable]
    public struct AdjacencyMatrix
    {
        public int

                w,
                h;

        public int[] storage;

        public AdjacencyMatrix(int[,] src)
        {
            w = src.GetLength(0);
            h = src.GetLength(1);
            storage = new int[w * h];
            Buffer.BlockCopy(src, 0, storage, 0, storage.Length * sizeof(int));
        }

        public int[,] data
        {
            get
            {
                var retVal = new int[w, h];
                Buffer
                    .BlockCopy(storage,
                    0,
                    retVal,
                    0,
                    storage.Length * sizeof(int));
                return retVal;
            }
        }
    }

    protected static System.Collections.Generic.Dictionary<Mesh, int[,]>
        adjacencyMatrixMap =
            new System.Collections.Generic.Dictionary<Mesh, int[,]>();

    public static int[,]
    GetCachedAdjacencyMatrix(
        Mesh mesh,
        float adjacencyMatchingVertexTolerance = 1e-4f,
        bool readCachedADjacencyMatrix = false
    )
    {
        int[,] adjacencyMatrix;
        if (adjacencyMatrixMap.TryGetValue(mesh, out adjacencyMatrix))
        {
            return adjacencyMatrix;
        }

        adjacencyMatrix =
            MeshUtils
                .BuildAdjacencyMatrix(mesh.vertices,
                mesh.triangles,
                maxOmegaCount,
                adjacencyMatchingVertexTolerance *
                adjacencyMatchingVertexTolerance);

        adjacencyMatrixMap.Add(mesh, adjacencyMatrix);
        return adjacencyMatrix;
    }
    #endregion



    #region Direct Delta Mush implementation
    protected Matrix4x4[] GenerateBoneMatrices()
    {
        Matrix4x4[] boneMatrices = new Matrix4x4[skin.bones.Length];

        for (int i = 0; i < boneMatrices.Length; i++)
        {
            Matrix4x4 localToWorld = skin.bones[i].localToWorldMatrix;
            Matrix4x4 bindPose = mesh.bindposes[i];
            boneMatrices[i] = localToWorld * bindPose;
        }
        return boneMatrices;
    }


    #endregion



    #region Helpers
    void DrawMesh()
    {
        if (actuallyUseCompute)
        {
            mesh.bounds = skin.bounds; // skin is actually disabled, so it only remembers the last animation frame
            Graphics.DrawMesh(mesh, Matrix4x4.identity, ductTapedMaterial, 0);
        }
        else
        {
            Graphics
                .DrawMesh(meshForCPUOutput,
                Matrix4x4.identity,
                skin.sharedMaterial,
                0);
        }
    }

    void DrawDeltas()
    {
    }

    void DrawVerticesVsSkin()
    {
    }
    #endregion

    internal DDMUtilsIterative.OmegaWithIndex[,] omegaWithIdxs;

    void Start()
    {
        InitBase();
        if (computeShader && ductTapedShader)
        {
            computeShader.SetBuffer(deformKernel, "Omegas", omegasCB);
        }
        if (!useCompute)
        {
            omegaWithIdxs =
                new DDMUtilsIterative.OmegaWithIndex[vCount, maxOmegaCount];
            omegasCB.GetData(omegaWithIdxs);
        }
    }

    void OnDestroy()
    {
        ReleaseBase();
    }

    void LateUpdate()
    {
        UpdateBase();
    }


    #region Direct Delta Mush implementation
    protected void UpdateMeshOnCPU()
    {
        Matrix4x4[] boneMatrices = GenerateBoneMatrices();
        BoneWeight[] bw = mesh.boneWeights;
        Vector3[] vs = mesh.vertices;
        Vector3[] ns = mesh.normals;

        DenseMatrix[] boneMatricesDense = new DenseMatrix[boneMatrices.Length];
        for (int i = 0; i < boneMatrices.Length; ++i)
        {
            boneMatricesDense[i] = new DenseMatrix(4);
            for (int row = 0; row < 4; ++row)
            {
                for (int col = 0; col < 4; ++col)
                {
                    boneMatricesDense[i][row, col] = boneMatrices[i][row, col]; //mesh.bindposes[i][row, col];
                }
            }
        }

        for (int vi = 0; vi < mesh.vertexCount; ++vi)
        {
            DenseMatrix mat4 = DenseMatrix.CreateIdentity(4);

            DDMUtilsIterative.OmegaWithIndex oswi0 = omegaWithIdxs[vi, 0];
            if (oswi0.boneIndex >= 0)
            {
                DenseMatrix omega0 = new DenseMatrix(4);
                omega0[0, 0] = oswi0.m00;
                omega0[0, 1] = oswi0.m01;
                omega0[0, 2] = oswi0.m02;
                omega0[0, 3] = oswi0.m03;
                omega0[1, 0] = oswi0.m01;
                omega0[1, 1] = oswi0.m11;
                omega0[1, 2] = oswi0.m12;
                omega0[1, 3] = oswi0.m13;
                omega0[2, 0] = oswi0.m02;
                omega0[2, 1] = oswi0.m12;
                omega0[2, 2] = oswi0.m22;
                omega0[2, 3] = oswi0.m23;
                omega0[3, 0] = oswi0.m03;
                omega0[3, 1] = oswi0.m13;
                omega0[3, 2] = oswi0.m23;
                omega0[3, 3] = oswi0.m33;
                mat4 = boneMatricesDense[oswi0.boneIndex] * omega0;
                for (int i = 1; i < maxOmegaCount; ++i)
                {
                    DDMUtilsIterative.OmegaWithIndex oswi =
                        omegaWithIdxs[vi, i];
                    if (oswi.boneIndex < 0)
                    {
                        break;
                    }
                    DenseMatrix omega = new DenseMatrix(4);
                    omega[0, 0] = oswi.m00;
                    omega[0, 1] = oswi.m01;
                    omega[0, 2] = oswi.m02;
                    omega[0, 3] = oswi.m03;
                    omega[1, 0] = oswi.m01;
                    omega[1, 1] = oswi.m11;
                    omega[1, 2] = oswi.m12;
                    omega[1, 3] = oswi.m13;
                    omega[2, 0] = oswi.m02;
                    omega[2, 1] = oswi.m12;
                    omega[2, 2] = oswi.m22;
                    omega[2, 3] = oswi.m23;
                    omega[3, 0] = oswi.m03;
                    omega[3, 1] = oswi.m13;
                    omega[3, 2] = oswi.m23;
                    omega[3, 3] = oswi.m33;
                    mat4 += boneMatricesDense[oswi.boneIndex] * omega;
                }
            }

            DenseMatrix Qi = new DenseMatrix(3);
            for (int row = 0; row < 3; ++row)
            {
                for (int col = 0; col < 3; ++col)
                {
                    Qi[row, col] = mat4[row, col];
                }
            }

            DenseVector qi = new DenseVector(3);
            qi[0] = mat4[0, 3];
            qi[1] = mat4[1, 3];
            qi[2] = mat4[2, 3];

            DenseVector pi = new DenseVector(3);
            pi[0] = mat4[3, 0];
            pi[1] = mat4[3, 1];
            pi[2] = mat4[3, 2];

            DenseMatrix qi_piT = new DenseMatrix(3);
            qi.OuterProduct(pi, qi_piT);
            DenseMatrix M = Qi - qi_piT;
            Matrix4x4 gamma = Matrix4x4.zero;
            var SVD = M.Svd(true);
            DenseMatrix U = (DenseMatrix)SVD.U;
            DenseMatrix VT = (DenseMatrix)SVD.VT;
            DenseMatrix R = U * VT;

            DenseVector ti = qi - (R * pi);

            // Get gamma
            for (int row = 0; row < 3; ++row)
            {
                for (int col = 0; col < 3; ++col)
                {
                    gamma[row, col] = R[row, col];
                }
            }
            gamma[0, 3] = ti[0];
            gamma[1, 3] = ti[1];
            gamma[2, 3] = ti[2];
            gamma[3, 3] = 1.0f;

            Vector3 vertex = gamma.MultiplyPoint3x4(vs[vi]);
            deformedMesh.vertices[vi] = vertex;
            Vector3 normal = gamma.MultiplyVector(ns[vi]);
            deformedMesh.normals[vi] = normal;
        }

        Bounds bounds = new Bounds();
        for (int i = 0; i < deformedMesh.vertexCount; i++)
            bounds.Encapsulate(deformedMesh.vertices[i]);

        meshForCPUOutput.vertices = deformedMesh.vertices;
        meshForCPUOutput.normals = deformedMesh.normals;
        meshForCPUOutput.bounds = bounds;
    }

    protected void UpdateMeshOnGPU()
    {
        int threadGroupsX =
            (vCount + computeThreadGroupSizeX - 1) / computeThreadGroupSizeX;

        Matrix4x4[] boneMatrices = GenerateBoneMatrices();

        bonesCB.SetData(boneMatrices);
        computeShader.SetBuffer(deformKernel, "Bones", bonesCB);
        computeShader.Dispatch(deformKernel, threadGroupsX, 1, 1);
        ductTapedMaterial.SetBuffer("Vertices", outputCB);
    }


    #endregion

}
