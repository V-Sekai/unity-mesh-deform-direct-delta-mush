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

using System;
using MathNet.Numerics.LinearAlgebra.Single;
using UnityEngine;

public class DDMUtilsIterative
{
    public struct OmegaWithIndex
    {
        public float m00;

        public float m01;

        public float m11;

        public float m02;

        public float m12;

        public float m22;

        public float m03;

        public float m13;

        public float m23;

        public float m33;

        public int boneIndex;
    }

    public struct CompressedPpp
    {
        public float m00;

        public float m01;

        public float m11;

        public float m02;

        public float m12;

        public float m22;
    }

    public int n;

    public float dm_blend;

    public int num_transforms;

    public SparseMatrix C;

    public DenseMatrix W;

    public SparseMatrix B;

    public DenseMatrix V;

    public float translationSmooth = 0.1f;

    public float rotationSmooth = 0.1f;

    private DenseMatrix[,] psis;

    private bool[,] psisValid;

    private float[,] wps;

    private bool[,] wpsValid;

    private DenseVector[] ps;

    private bool[] psValid;

    public void InitCache()
    {
        psis = new DenseMatrix[n, num_transforms];
        psisValid = new bool[n, num_transforms];

        wps = new float[n, num_transforms];
        wpsValid = new bool[n, num_transforms];

        ps = new DenseVector[n];
        psValid = new bool[n];

        for (int vi = 0; vi < n; ++vi)
        {
            for (int bi = 0; bi < num_transforms; ++bi)
            {
                psisValid[vi, bi] = false;
                wpsValid[vi, bi] = false;
            }
            psValid[vi] = false;
        }
    }

    public DenseMatrix compute_psi(int i, int j)
    {
        if (psisValid[i, j])
        {
            return psis[i, j];
        }
        DenseMatrix res = new DenseMatrix(4);

        for (int k = 0; k < n; k++)
        {
            float w = W[k, j];
            float b = B[k, i];
            if (w != 0 && b != 0)
            {
                float[] v = new float[] { V[k, 0], V[k, 1], V[k, 2], 1.0f };
                DenseVector rw = new DenseVector(v);
                DenseMatrix hh = new DenseMatrix(4);
                rw.OuterProduct (rw, hh);
                hh *= (b * w);
                res += hh;
            }
        }

        psis[i, j] = res;
        psisValid[i, j] = true;
        return res;
    }

    public DenseVector compute_pi(int i)
    {
        if (psValid[i])
        {
            return ps[i];
        }
        DenseVector sum = new DenseVector(3);

        for (int k = 0; k < num_transforms; k++)
        {
            DenseMatrix psi = compute_psi(i, k);
            sum[0] += psi[0, 3];
            sum[1] += psi[1, 3];
            sum[2] += psi[2, 3];
        }
        ps[i] = sum;
        psValid[i] = true;
        return sum;
    }

    public float compute_w_prime(int i, int j)
    {
        if (wpsValid[i, j])
        {
            return wps[i, j];
        }

        float res = 0;
        for (int k = 0; k < n; k++)
        {
            float w = W[k, j];
            float c = C[k, i];
            res += w * c;
        }

        wps[i, j] = res;
        wpsValid[i, j] = true;
        return res;
    }

    public DenseMatrix compute_omega(int i, int j)
    {
        DenseVector p_i = compute_pi(i);
        DenseMatrix p_i_mat = new DenseMatrix(3);
        p_i.OuterProduct (p_i, p_i_mat);

        DenseMatrix p_i_mat_4 = new DenseMatrix(4);
        p_i_mat_4.SetSubMatrix(0, 0, p_i_mat);

        DenseMatrix psi_ij = compute_psi(i, j);

        return (1.0f - dm_blend) * psi_ij +
        (dm_blend * compute_w_prime(i, j)) * p_i_mat_4;
    }

    public DenseMatrix ComputePiMat4(int i, int j)
    {
        DenseVector p_i = compute_pi(i);
        DenseMatrix p_i_mat = new DenseMatrix(3);
        p_i.OuterProduct (p_i, p_i_mat);
        DenseMatrix p_i_mat_4 = new DenseMatrix(4);
        p_i_mat_4.SetSubMatrix(0, 0, p_i_mat);
        return compute_w_prime(i, j) * p_i_mat_4;
    }

    public DenseMatrix[,] ComputeOmegas(int vCount, int bCount, int iterations)
    {
        DenseMatrix[,] omegas = new DenseMatrix[vCount, bCount];
        DenseMatrix[,] prevOmegas = new DenseMatrix[vCount, bCount];
        DenseVector vertex = new DenseVector(4);
        DenseMatrix tmpOmega = new DenseMatrix(4);
        for (int vi = 0; vi < vCount; ++vi)
        {
            vertex[0] = V[vi, 0];
            vertex[1] = V[vi, 1];
            vertex[2] = V[vi, 2];
            vertex[3] = 1.0f;
            vertex.OuterProduct (vertex, tmpOmega);
            for (int bi = 0; bi < bCount; ++bi)
            {
                DenseMatrix omega = new DenseMatrix(4);
                omega += W[vi, bi] * tmpOmega;
                omegas[vi, bi] = omega;
            }
        }

        // Only psi now.
        for (int it = 0; it < iterations; ++it)
        {
            DenseMatrix[,] tmp = prevOmegas;
            prevOmegas = omegas;
            omegas = tmp;

            for (int vi = 0; vi < vCount; ++vi)
            {
                for (int bi = 0; bi < bCount; ++bi)
                {
                    DenseMatrix omega = new DenseMatrix(4);

                    for (int k = 0; k < vCount; ++k)
                    {
                        if (B[vi, k] != 0.0f)
                        {
                            omega += B[vi, k] * prevOmegas[k, bi];
                        }
                    }
                    omegas[vi, bi] = omega;
                }
            }
        }

        return omegas;
    }

    private static Matrix4x4 ConvertOmegaFromMathNet(DenseMatrix omega)
    {
        Matrix4x4 outOmega = Matrix4x4.zero;
        for (int i = 0; i < 4; ++i)
        {
            for (int j = 0; j < 4; ++j)
            {
                outOmega[i, j] = omega[i, j];
            }
        }
        return outOmega;
    }

    public static OmegaWithIndex[,]
    ConvertOmegas1D(DenseMatrix[,] omegas, int omegaCount)
    {
        int
            vCount = omegas.GetLength(0),
            bCount = omegas.GetLength(1);
        OmegaWithIndex[,] outOmegas = new OmegaWithIndex[vCount, omegaCount];
        for (int vi = 0; vi < vCount; ++vi)
        {
            int curOmegaCount = 0;
            for (int bi = 0; bi < bCount; ++bi)
            {
                if (curOmegaCount >= omegaCount)
                {
                    break;
                }
                DenseMatrix omega = omegas[vi, bi];
                bool skip = true;
                for (int row = 0; row < 4; ++row)
                {
                    for (int col = 0; col < 4; ++col)
                    {
                        if (Math.Abs(omega[row, col]) > 1e-6f)
                        {
                            skip = false;
                            break;
                        }
                    }
                }
                if (skip)
                {
                    continue;
                }
                outOmegas[vi, curOmegaCount].boneIndex = bi;
                outOmegas[vi, curOmegaCount].m00 = omega[0, 0];
                outOmegas[vi, curOmegaCount].m01 = omega[0, 1];
                outOmegas[vi, curOmegaCount].m11 = omega[1, 1];
                outOmegas[vi, curOmegaCount].m02 = omega[0, 2];
                outOmegas[vi, curOmegaCount].m12 = omega[1, 2];
                outOmegas[vi, curOmegaCount].m22 = omega[2, 2];
                outOmegas[vi, curOmegaCount].m03 = omega[0, 3];
                outOmegas[vi, curOmegaCount].m13 = omega[1, 3];
                outOmegas[vi, curOmegaCount].m23 = omega[2, 3];
                outOmegas[vi, curOmegaCount].m33 = omega[3, 3];
                ++curOmegaCount;
            }
            for (; curOmegaCount < omegaCount; ++curOmegaCount)
            {
                outOmegas[vi, curOmegaCount].boneIndex = -1;
            }
        }
        return outOmegas;
    }

    public static CompressedPpp[] ConvertPpp(DenseMatrix[] P_pps)
    {
        int vCount = P_pps.GetLength(0);
        CompressedPpp[] res = new CompressedPpp[vCount];
        for (int vi = 0; vi < vCount; ++vi)
        {
            DenseMatrix P_pp = P_pps[vi];

            res[vi].m00 = P_pp[0, 0];
            res[vi].m01 = P_pp[0, 1];
            res[vi].m11 = P_pp[1, 1];
            res[vi].m02 = P_pp[0, 2];
            res[vi].m12 = P_pp[1, 2];
            res[vi].m22 = P_pp[2, 2];
        }
        return res;
    }

    public static Matrix4x4[,] ConvertOmegas2D(DenseMatrix[,] omegas)
    {
        int
            vCount = omegas.GetLength(0),
            bCount = omegas.GetLength(1);
        Matrix4x4[,] outOmegas = new Matrix4x4[vCount, bCount];
        for (int vi = 0; vi < vCount; ++vi)
        {
            for (int bi = 0; bi < bCount; ++bi)
            {
                outOmegas[vi, bi] = ConvertOmegaFromMathNet(omegas[vi, bi]);
            }
        }
        return outOmegas;
    }

    public static Matrix4x4[,]
    CompressOmegas2D(DenseMatrix[,] omegas, BoneWeight[] boneWeights)
    {
        int vCount = boneWeights.Length;
        Matrix4x4[,] compressedOmegas = new Matrix4x4[vCount, 4];
        for (int vi = 0; vi < vCount; ++vi)
        {
            BoneWeight bw = boneWeights[vi];
            if (bw.boneIndex0 >= 0 && bw.weight0 > 0.0f)
            {
                compressedOmegas[vi, 0] =
                    ConvertOmegaFromMathNet(omegas[vi, bw.boneIndex0]);
            }
            if (bw.boneIndex1 >= 0 && bw.weight1 > 0.0f)
            {
                compressedOmegas[vi, 1] =
                    ConvertOmegaFromMathNet(omegas[vi, bw.boneIndex1]);
            }
            if (bw.boneIndex2 >= 0 && bw.weight2 > 0.0f)
            {
                compressedOmegas[vi, 2] =
                    ConvertOmegaFromMathNet(omegas[vi, bw.boneIndex2]);
            }
            if (bw.boneIndex3 >= 0 && bw.weight3 > 0.0f)
            {
                compressedOmegas[vi, 3] =
                    ConvertOmegaFromMathNet(omegas[vi, bw.boneIndex3]);
            }
        }
        return compressedOmegas;
    }
}
