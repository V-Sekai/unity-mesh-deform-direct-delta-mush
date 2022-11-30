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

public class TestDDMRuntime : MonoBehaviour
{
    public int iterations = -1;

    public float smoothLambda = -1.0f;

    public float adjacencyMatchingVertexTolerance = -1.0f;

    void UpdateValues(DDMSkinnedMeshGPUVar0 script)
    {
        if (script != null)
        {
            if (iterations >= 0)
            {
                script.iterations = iterations;
            }
            if (smoothLambda >= 0.0f)
            {
                script.smoothLambda = smoothLambda;
            }
            if (adjacencyMatchingVertexTolerance >= 0.0f)
            {
                script.adjacencyMatchingVertexTolerance =
                    adjacencyMatchingVertexTolerance;
            }
        }
    }

    void Awake()
    {
        Debug.Log("Test DDM runtime awake.");
        DDMSkinnedMeshGPUVar0[] scriptsDDM =
            FindObjectsOfType<DDMSkinnedMeshGPUVar0>();
        Debug.Log("Find " + scriptsDDM.Length.ToString() + " DDM scripts.");
        foreach (DDMSkinnedMeshGPUVar0 script in scriptsDDM)
        {
            UpdateValues (script);
        }
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
