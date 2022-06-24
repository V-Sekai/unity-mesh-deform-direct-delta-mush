//#define WITH_SCALE_MATRIX
using System;
using System.IO;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using MathNet.Numerics.LinearAlgebra.Solvers;
using UnityEditor;
using UnityEngine;

//[ExecuteInEditMode]
[RequireComponent(typeof(SkinnedMeshRenderer))]
public abstract class DDMSkinnedMeshGPUVar0 : MonoBehaviour
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
            {

                UpdateMeshOnGPU();
            }
            else
            {
                UpdateMeshOnCPU();
            }
        }
        if (compareWithSkinning)
        {
            DrawVerticesVsSkin();
        }
        else
        {
            DrawMesh();
        }

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

        //#if UNITY_EDITOR
        //		if (readCachedADjacencyMatrix)
        //		{
        //			//var path = Path.Combine(Application.persistentDataPath, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mesh)) + ".adj");
        //			var path = Path.Combine("", AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mesh)) + "_" + adjacencyMatchingVertexTolerance.ToString() + ".adj");
        //			Debug.Log(path);
        //			if (File.Exists(path))
        //			{
        //				string json = File.ReadAllText(path);
        //				adjacencyMatrix = JsonUtility.FromJson<AdjacencyMatrix>(json).data;
        //			}
        //			else
        //			{
        //#endif
        adjacencyMatrix =
            MeshUtils
                .BuildAdjacencyMatrix(mesh.vertices,
                mesh.triangles,
                maxOmegaCount,
                adjacencyMatchingVertexTolerance *
                adjacencyMatchingVertexTolerance);

        //#if UNITY_EDITOR
        //				var json = JsonUtility.ToJson(new AdjacencyMatrix(adjacencyMatrix));
        //				Debug.Log(json);
        //				using (FileStream fs = new FileStream(path, FileMode.Create))
        //				{
        //					using (StreamWriter writer = new StreamWriter(fs))
        //					{
        //						writer.Write(json);
        //					}
        //				}
        //			}
        //		}
        //		else
        //        {
        //			adjacencyMatrix = MeshUtils.BuildAdjacencyMatrix(mesh.vertices, mesh.triangles, maxOmegaCount, adjacencyMatchingVertexTolerance * adjacencyMatchingVertexTolerance);
        //		}
        //#endif
        adjacencyMatrixMap.Add(mesh, adjacencyMatrix);
        return adjacencyMatrix;
    }
    #endregion



    #region Direct Delta Mush implementation
    protected Matrix4x4[] GenerateBoneMatrices()
    {
        Matrix4x4[] boneMatrices = new Matrix4x4[skin.bones.Length];

#if WITH_SCALE_MATRIX
        Matrix4x4[] scaleMatrices = new Matrix4x4[skin.bones.Length];
#endif // WITH_SCALE_MATRIX

        for (int i = 0; i < boneMatrices.Length; i++)
        {
            Matrix4x4 localToWorld = skin.bones[i].localToWorldMatrix;
            Matrix4x4 bindPose = mesh.bindposes[i];

#if WITH_SCALE_MATRIX
            Vector3 localScale = localToWorld.lossyScale;
            Vector3 bpScale = bindPose.lossyScale;

            localToWorld.SetColumn(0, localToWorld.GetColumn(0) / localScale.x);
            localToWorld.SetColumn(1, localToWorld.GetColumn(1) / localScale.y);
            localToWorld.SetColumn(2, localToWorld.GetColumn(2) / localScale.z);
            bindPose.SetColumn(0, bindPose.GetColumn(0) / bpScale.x);
            bindPose.SetColumn(1, bindPose.GetColumn(1) / bpScale.y);
            bindPose.SetColumn(2, bindPose.GetColumn(2) / bpScale.z);

            scaleMatrices[i] =
                Matrix4x4.Scale(localScale) * Matrix4x4.Scale(bpScale);
#endif // WITH_SCALE_MATRIX

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
            Graphics
                .DrawMesh(meshForCPUOutput,
                Matrix4x4.identity,
                skin.sharedMaterial,
                0);
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
#if WITH_SCALE_MATRIX
            Matrix4x4 scaleMatrix =
                (bw[vi].boneIndex0 >= 0 && bw[vi].weight0 > 0.0f)
                    ? scaleMatrices[bw[vi].boneIndex0]
                    : Matrix4x4.identity;
            if (bw[vi].boneIndex1 >= 0 && bw[vi].weight1 > 0.0f)
            {
                for (int idx = 0; idx < 16; ++idx)
                {
                    scaleMatrix[idx] += scaleMatrices[bw[vi].boneIndex1][idx];
                }
            }
            if (bw[vi].boneIndex2 >= 0 && bw[vi].weight2 > 0.0f)
            {
                for (int idx = 0; idx < 16; ++idx)
                {
                    scaleMatrix[idx] += scaleMatrices[bw[vi].boneIndex2][idx];
                }
            }
            if (bw[vi].boneIndex3 >= 0 && bw[vi].weight3 > 0.0f)
            {
                for (int idx = 0; idx < 16; ++idx)
                {
                    scaleMatrix[idx] += scaleMatrices[bw[vi].boneIndex3][idx];
                }
            }
#endif // WITH_SCALE_MATRIX

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

#if WITH_SCALE_MATRIX
            gamma *= scaleMatrix;
#endif // WITH_SCALE_MATRIX

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
