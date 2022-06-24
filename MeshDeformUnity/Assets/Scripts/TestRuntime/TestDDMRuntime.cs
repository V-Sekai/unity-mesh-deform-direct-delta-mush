using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestDDMRuntime : MonoBehaviour
{
    //public List<GameObject> meshObjects;
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

    void UpdateValues(DeltaMushSkinnedMesh script)
    {
        if (script != null)
        {
            if (iterations >= 0)
            {
                script.iterations = iterations;
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
            FindObjectsOfType<DDMSkinnedMeshGPUVar0>(false);
        Debug.Log("Find " + scriptsDDM.Length.ToString() + " DDM scripts.");
        foreach (DDMSkinnedMeshGPUVar0 script in scriptsDDM)
        {
            UpdateValues (script);
        }
        DeltaMushSkinnedMesh[] scriptsDM =
            FindObjectsOfType<DeltaMushSkinnedMesh>(false);
        Debug.Log("Find " + scriptsDM.Length.ToString() + " DM scripts.");
        foreach (DeltaMushSkinnedMesh script in scriptsDM)
        {
            UpdateValues (script);
        }

        //foreach (GameObject meshObject in meshObjects)
        //{
        //    DDMSkinnedMeshGPUVar0 DDMBaseScript = meshObject.GetComponent<DDMSkinnedMeshGPUVar0>();
        //    UpdateValues(DDMBaseScript);
        //}
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
