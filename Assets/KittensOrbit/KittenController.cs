using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Kitten {
    public Vector3 pos;
    public Vector3 vel;
}

[System.Serializable]
public struct Star {
    public Transform transform;
    public float mass;
}

public class KittenController : MonoBehaviour {
    [SerializeField]
    public Star[] stars;
    public float G;
    public float size;
    public Color color;
    public float simSpeed = 1;
    public float stepSize = 0.005f;
    public int numParticle = 100000;

    public Mesh quad;
    public ComputeShader compute;
    public Material mat;

    Kitten[] data = null;
    int kernelSize = -1, kernel;
    int numCats = 0;
    bool dataChanged = true;
    ComputeBuffer buffer;
    ComputeBuffer commandBuffer;
    uint[] command = new uint[5];
    int GHash = Shader.PropertyToID("G"), hHash = Shader.PropertyToID("h"), dataHash = Shader.PropertyToID("data"), starHash = Shader.PropertyToID("stars"), itrHash = Shader.PropertyToID("itr");
    System.Random rand = new System.Random();
    float itrQueue = 0;

    void Awake() {
        commandBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        data = new Kitten[numParticle];
        buffer = new ComputeBuffer(data.Length, 6 * sizeof(float));
        kernel = compute.FindKernel("CSMain");
        compute.SetBuffer(kernel, dataHash, buffer);
        mat.SetBuffer(dataHash, buffer);
        uint x, y, z;
        compute.GetKernelThreadGroupSizes(kernel, out x, out y, out z);
        kernelSize = (int)x;
        OnValidate();

        for (int i = 0; i < numParticle; i++) {
            Vector3 pos = Quaternion.Euler(15 * (UnityEngine.Random.value - 0.5f), 720 * UnityEngine.Random.value, 0) * Vector3.forward;
            float r = 1 + 0.5f * Mathf.Pow(UnityEngine.Random.value, 0.5f);
            SpawnCat(pos * r, (1 + 0.4f * (UnityEngine.Random.value - 0.5f)) * Vector3.Cross(pos, Vector3.up).normalized * Mathf.Sqrt(G / r));
        }
    }

    private void OnValidate() {
        if (Application.isPlaying) {
            compute.SetFloat(GHash, G);
            compute.SetFloat(hHash, stepSize);
        }
        mat.SetColor("_Color", color);
        mat.SetFloat("_Scale", size);
    }

    void SpawnCat(Vector3 pos, Vector3 vel) {
        if (numCats == data.Length)
            Array.Resize<Kitten>(ref data, 2 * data.Length);
        data[numCats++] = new Kitten() { pos = pos, vel = vel };
        dataChanged = true;
    }

    Vector4[] starData = new Vector4[4];

    private void OnDestroy() {
        commandBuffer.Release();
        buffer.Release();
    }

    void Update() {
        if (dataChanged) {
            if (data.Length != buffer.count) {
                buffer.Release();
                buffer = new ComputeBuffer(data.Length, 6 * sizeof(float));
                compute.SetBuffer(kernel, dataHash, buffer);
                mat.SetBuffer(dataHash, buffer);
            }
            buffer.SetData(data);
            dataChanged = false;
        }

        itrQueue += Time.deltaTime * simSpeed;
        itrQueue = Mathf.Min(simSpeed / stepSize, itrQueue);

        int itr = (int)itrQueue;

        if (itr != 0) {
            int i = 0;
            foreach (var item in stars)
                if (item.transform.gameObject.activeSelf && i < 4)
                    starData[i++] = new Vector4(item.transform.position.x, item.transform.position.y, item.transform.position.z, item.mass);
            for (; i < 4; i++) starData[i] = Vector4.zero;

            compute.SetVectorArray(starHash, starData);
            compute.SetInt(itrHash, itr);
            compute.Dispatch(kernel, (numCats - 1) / kernelSize + 1, 1, 1);

            itrQueue -= itr;
        }

        command[0] = quad.GetIndexCount(0);
        command[1] = (uint)numCats;
        command[2] = quad.GetIndexStart(0);
        command[3] = quad.GetBaseVertex(0);
        commandBuffer.SetData(command);

        Graphics.DrawMeshInstancedIndirect(quad, 0, mat, new Bounds(Vector3.zero, 10000 * Vector3.one), commandBuffer, 0, null, UnityEngine.Rendering.ShadowCastingMode.Off, false, 0, null, UnityEngine.Rendering.LightProbeUsage.Off);
    }
}
