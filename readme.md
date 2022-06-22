# Real-Time Mesh Skinning with Direct Delta Mush (Unity) (fire edition) 

## Problem solved

Simply authored skinned meshes can have skinning and animations with efficiency and quality.

## Project Description

This is a Unity project implementing real-time mesh skinning using GPU-based Direct Delta Mush algorithm. This algorithm and its variants enable us to compute mesh skinning and animations with efficiency and quality, even with simply authored skinned meshes.

[![Overview1](Readme/Overview1.gif)](https://vimeo.com/655985843)

With Direct Delta Mush (left), we get less bulging effect than what we get with built-in skinning (right).

## How to Build

### Requirement

- Unity 2020.3.36f1
- Visual Studio 2022

### Build

Add `MeshDeformUnity` to Unity Hub and select Unity version to open this project.

## Features Overview

- Direct Delta Mush with GPU variant 0.
- Precomputation of Direct Delta Mush with GPU.

| 0 iter (LBS)                          | 2 iters                               | 16 iters                               |
| ------------------------------------- | ------------------------------------- | -------------------------------------- |
| ![img](Readme/VisualEffect_Iter0.png) | ![img](Readme/VisualEffect_Iter2.png) | ![img](Readme/VisualEffect_Iter16.png) |

The table above shows how the number of iterations affect the visual effect of skinning. With more and more iterations, the elbow shows smoother, and less bulging effect.

### Variants of Direct Delta Mush

The [paper](https://www.ea.com/seed/news/siggraph2019-direct-delta-mush) also shows some variants which are equivalent to special cases of several previous skinning algorithms.

- The variant 0 is the full DDM model.

| LBS                                 | v0                                 |
| ----------------------------------- | ---------------------------------- |
| ![img](Readme/VisualEffect_LBS.png) | ![img](Readme/VisualEffect_v0.png) |

See [technical notes](notes.md) for technical details.

## How to Use

1. Load models, and toggle `Read/Write Enabled`.

   ![Load Model](Readme/HowToUse_1Load_Label.png)

2. Drag the model into the scene, or select the object with this model in the scene.

   ![Drag to Scene](Readme/HowToUse_2Select.png)

3. Expand and find the mesh object of the model. Add component to the mesh object. Make sure that there is a `Skinned Mesh Render` component in this object.

   ![Add Component](Readme/HowToUse_3AddComponent_Label.png)

4. Take a look at the component `DDM skinned Mesh GPU Var 0` for example. There are several attributes.

   - `Iterations` represents the iteration count of the precomputation.
   - `Smooth Lambda` determines the smoothing result for each step.
   - `Use Compute` determines whether you use GPU skinning or CPU skinning, but currently we only implemented GPU skinning for most of the variants.
   - `Adjacency Matching Vertex Tolerance` can be set with a small positive float number if you need to merge the adjacency data of the vertices which are very close to each other, but enabling this process may cause longer precomputations.
   - `Debug Mode` is for comparison to the visual effect of the built-in skinning if you assign `Compare With Linear Blend` to this attribute.

   You can modify `Iterations` and `Smooth Lambda` to change the visual effect of the runtime skinning.

   ![Add Component](Readme/HowToUse_4Script.png)

5. Set `Iterations` to 30, for example. For this model, set the `Adjacency Matching Vertex Tolerance` to a positive number to enable vertex matching. Then click the `Play` button, and switch to the `Scene` view. Expand the skeleton in the `Hierarchy` window and you can select which joint to edit.

   ![Select Joint](Readme/HowToUse_5SelectJoint_Label.png)

6. Press E to rotate the joints to deform mesh. Press W to translate the joints.

   ![Deform Mesh](Readme/HowToUse_6Deform_Label.png)

7. If you want to play animation on the mesh, you can create an `Animator Controller` and set the animation like the figure below. You can also set the speed as you want. Then choose the root of the model, and add component `Animator`, and set the animator controller mentioned before to the `Controller` attribute.

   ![Animator Controller](Readme/HowToUse_7Animator_Label.png)

   ![Animator](Readme/HowToUse_8Animator_Label.png)

   After you play, you can see the animation. Some of the models can be found at [mixamo](https://www.mixamo.com/).

## Credit & Reference

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
1. [Unity](https://unity.com/)
1. [Math.NET Numerics](https://github.com/mathnet/mathnet-numerics)
1. [Delta Mush: smoothing deformations while preserving detail](https://dl.acm.org/doi/10.1145/2633374.2633376)
1. [Direct Delta Mush Skinning and Variants](https://www.ea.com/seed/news/siggraph2019-direct-delta-mush)
1. [Direct Delta Mush Skinning Compression with Continuous Examples](https://www.ea.com/seed/news/ddm-compression-with-continuous-examples)
1. [Mixamo](https://www.mixamo.com/)

## Presentations

1. [Pitch](https://docs.google.com/presentation/d/1vwb5RJlEHCoQyWLS116C5mvTnZ4lScZMC8LQFr1BcJU/)
2. [Milestone 1](https://docs.google.com/presentation/d/1DddtqMYNPFK_de73_3AZ3dXIFQ1iPYBxOBAKMeCrQ8A/)
3. [Milestone 2](https://docs.google.com/presentation/d/14nwoKlDBEHcIAdbmpu_0bEEPnFItTixbUZDCPtQ1mfM/)
4. [Milestone 3](https://docs.google.com/presentation/d/1FIu6bGBnXOtndSAxtpXztczM1mbGk7st8uuC3rlGfBQ/)
5. [Final Presentation](https://docs.google.com/presentation/d/1wim-hyjRPX4jIR6AkHR4tgXh2nGj3MpmaegtT0uUxrw/)
6. [Sample Video](https://vimeo.com/655985843)
