# Real-Time Mesh Skinning with Direct Delta Mush (Unity) (Fire Edition)

## Problem Solved

Efficient and high-quality skinning and animations for simply authored skinned meshes.

## Project Description

A Unity project implementing real-time mesh skinning using the GPU-based Direct Delta Mush algorithm. This algorithm and its variants enable efficient and high-quality computation of mesh skinning and animations, even for simply authored skinned meshes.

![Overview1](Readme/Overview1.gif)

With Direct Delta Mush (left), we get less bulging effect than what we get with built-in skinning (right).

## How to Build

### Requirements

- Unity 2020.3.36f1
- Visual Studio 2022

### Build Instructions

Add `MeshDeformUnity` to Unity Hub and select the Unity version to open this project.

## Features Overview

- Direct Delta Mush with GPU variant 0.
- Precomputation of Direct Delta Mush with GPU.

| Iterations | Visual Effect |
| ---------- | ------------- |
| 0 (LBS)    | ![img](Readme/VisualEffect_Iter0.png) |
| 2          | ![img](Readme/VisualEffect_Iter2.png) |
| 16         | ![img](Readme/VisualEffect_Iter16.png) |

The table above shows how the number of iterations affect the visual effect of skinning. With more iterations, the elbow appears smoother and has a reduced bulging effect.

### Variants of Direct Delta Mush

The [paper](https://www.ea.com/seed/news/siggraph2019-direct-delta-mush) also presents some variants equivalent to special cases of several previous skinning algorithms.

- Variant 0 is the full DDM model.

| LBS                                 | v0                                 |
| ----------------------------------- | ---------------------------------- |
| ![img](Readme/VisualEffect_LBS.png) | ![img](Readme/VisualEffect_v0.png) |

See [technical notes](notes.md) for technical details.

## How to Use

1. Load models and toggle `Read/Write Enabled`.
   ![Load Model](Readme/HowToUse_1Load_Label.png)
2. Drag the model into the scene or select the object with this model in the scene.
   ![Drag to Scene](Readme/HowToUse_2Select.png)
3. Expand and find the mesh object of the model. Add a component to the mesh object. Ensure that there is a `Skinned Mesh Render` component in this object.
   ![Add Component](Readme/HowToUse_3AddComponent_Label.png)
4. Examine the component `DDM skinned Mesh GPU Var 0`. It has several attributes:
   - `Iterations`: The iteration count of the precomputation.
   - `Smooth Lambda`: Determines the smoothing result for each step.
   - `Use Compute`: Determines whether you use GPU skinning or CPU skinning, but currently, we only implemented GPU skinning for most of the variants.
   - `Adjacency Matching Vertex Tolerance`: Set with a small positive float number if you need to merge the adjacency data of the vertices close to each other, but enabling this process may cause longer precomputations.
   - `Debug Mode`: For comparison to the visual effect of built-in skinning if you assign `Compare With Linear Blend` to this attribute.
   
   Modify `Iterations` and `Smooth Lambda` to change the visual effect of runtime skinning.
   ![Add Component](Readme/HowToUse_4Script.png)
5. Set `Iterations` to 30, for example. For this model, set the `Adjacency Matching Vertex Tolerance` to a positive number to enable vertex matching. Then click the `Play` button and switch to the `Scene` view. Expand the skeleton in the `Hierarchy` window, and you can select which joint to edit.
   ![Select Joint](Readme/HowToUse_5SelectJoint_Label.png)
6. Press E to rotate the joints to deform the mesh. Press W to translate the joints.
   ![Deform Mesh](Readme/HowToUse_6Deform_Label.png)
7. To play animation on the mesh, create an `Animator Controller` and set the animation as shown below. You can also set the speed as desired. Then choose the root of the model, add the component `Animator`, and set the animator controller mentioned before to the `Controller` attribute.
   ![Animator Controller](Readme/HowToUse_7Animator_Label.png)
   ![Animator](Readme/HowToUse_8Animator_Label.png)

After playing, you can see the animation. Some models can be found at [mixamo](https://www.mixamo.com/).

## Credit & References

1. Xuntong Liang
   - xuntong@seas.upenn.edu
   - [LinkedIn](https://www.linkedin.com/in/xuntong-liang-406429181/)
   - [GitHub](https://github.com/PacosLelouch)
   - [twitter](https://twitter.com/XTL90234545)
2. Bowen Deng
   - dengbw@seas.upenn.edu
   - [LinkedIn](www.linkedin.com/in/bowen-deng-7dbw13)
3. Beini Gu
   - gubeini@seas.upenn.edu
   - [LinkedIn](www.linkedin.com/in/bowen-deng-7dbw13)
   - [personal website](https://www.seas.upenn.edu/~gubeini/)
   - [twitter](https://twitter.com/scoutydren)
4. [Unity](https://unity.com/)
5. [Math.NET Numerics](https://github.com/mathnet/mathnet-numerics)
6. [Delta Mush: smoothing deformations while preserving detail](https://dl.acm.org/doi/10.1145/2633374.2633376)
7. [Direct Delta Mush Skinning and Variants](https://www.ea.com/seed/news/siggraph2019-direct-delta-mush)
8. [Direct Delta Mush Skinning Compression with Continuous Examples](https://www.ea.com/seed/news/ddm-compression-with-continuous-examples)
9. [Mixamo](https://www.mixamo.com/)

## Presentations

1. [Pitch](https://docs.google.com/presentation/d/1vwb5RJlEHCoQyWLS116C5mvTnZ4lScZMC8LQFr1BcJU/)
2. [Milestone 1](https://docs.google.com/presentation/d/1DddtqMYNPFK_de73_3AZ3dXIFQ1iPYBxOBAKMeCrQ8A/)
3. [Milestone 2](https://docs.google.com/presentation/d/14nwoKlDBEHcIAdbmpu_0bEEPnFItTixbUZDCPtQ1mfM/)
4. [Milestone 3](https://docs.google.com/presentation/d/1FIu6bGBnXOtndSAxtpXztczM1mbGk7st8uuC3rlGfBQ/)
5. [Final Presentation](https://docs.google.com/presentation/d/1wim-hyjRPX4jIR6AkHR4tgXh2nGj3MpmaegtT0uUxrw/)
6. [Sample Video](https://vimeo.com/655985843)