# Render Pipelines

URP is supported. For camera background rendering we provide a renderer feature that works similarly to the AR Foundation background renderer feature.

| Render Pipeline |  |
| - | - |
| Built-in | ✔️ (Camera Background is Experimental) |
| URP | ✔️ (Camera Background is Experimental) |
| HDRP | ❌ (Not tested) |



# Supported Configurations

| Unity Version | Input System |      |     | ARFoundation |             | Interaction Mode |                  |
|---------------|--------------|------|-----|--------------|-------------|------------------|------------------|
|               | Old          | Both | [New](https://github.com/needle-mirror/com.unity.inputsystem) | [3.1](https://github.com/needle-mirror/com.unity.xr.arfoundation/tree/3.1.3)          | [4.0](https://github.com/needle-mirror/com.unity.xr.arfoundation/)          | Game View        | [Device Simulator](https://github.com/needle-mirror/com.unity.device-simulator)<sup><a href="#table-sup-1">1</a></sup> |
| [![](https://img.shields.io/badge/%40-2019.3/4-green.svg)](https://unity.com/de/releases/2019-3)      | ✔️           | ✔️   | ✔️  | ✔️           | ✔️          | ✔️               | ✔️               |
| [![](https://img.shields.io/badge/%40-2020.1b-green.svg)](https://unity3d.com/de/beta/2020.1b)      | ✔️           | ✔️   | ✔️  | ✔️           | ✔️          | ✔️               | ✔️               |
| [![](https://img.shields.io/badge/%40-2020.2a-green.svg)](https://unity3d.com/de/beta/2020.2a)       | ✔️           | ✔️   | ✔️  | ✔️           | ✔️          | ✔️               | ✔️               |

| Unity Version | Render Pipeline |           |                 | Platform |                   |               |
|---------------|-----------------|-----------|-----------------|----------|-------------------|---------------|
|               | Built-in        | [URP](https://github.com/needle-mirror/com.unity.render-pipelines.universal)       | HDRP<sup><a href="#table-sup-2">2</a></sup> | Editor   | iOS/Android Build<sup><a href="#table-sup-3">3</a></sup> | Desktop Build<sup><a href="#table-sup-4">4</a></sup>                |
| [![](https://img.shields.io/badge/%40-2019.3/4-green.svg)](https://unity.com/de/releases/2019-3)        | ✔️              | ✔️        | —      | ✔️      |  ✔️                                         | untested     |
| [![](https://img.shields.io/badge/%40-2020.1b-green.svg)](https://unity3d.com/de/beta/2020.1b)       | ✔️              | ✔️        | —      | ✔️      |  ✔️                                         | untested     |
| [![](https://img.shields.io/badge/%40-2020.2a-green.svg)](https://unity3d.com/de/beta/2020.2a)       | ✔️              | ✔️        | —      | ✔️      |  ✔️                                         | untested     |

<sup id="table-sup-1">1</sup> Recommended. Feels very nice to use, and gives correct sizes for UI etc.  
<sup id="table-sup-2">2</sup> HDRP is not supported by Unity on iOS/Android currently.  
<sup id="table-sup-3">3</sup> "Support" here means: ARSimulation does not affect your builds, it is purely for Editor simulation.  
<sup id="table-sup-4">4</sup> We haven't done as extensive testing as with the others yet. Making Desktop builds with ARSimulation is very useful for testing multiplayer scenarios without the need to deploy to multiple mobile devices.

# XR Interaction Toolkit

Works out of the box.
(If you have unity editor input that has multitouch, we recommend using LeanTouch)

# Comparison between MARS and ARSimulation ⚔
| ⚔ | ARSimulation | MARS |
| -- | -- | -- |
| Claim | Non-invasive editor simulation backend for ARFoundation | Framework for simplified, flexible AR Authoring |
| Functionality | XR SDK plugin for Desktop:<br>positional tracking simulation, touch input simulation, image tracking, ... | Wrapper around ARFoundation with added functionality: <br>custom simulation window, object constraints and forces, editor simulation (including most of what ARSimulation can do), file system watchers, custom Editor handles, codegen, ... |
| Complexity | <ul><li>1 package</li><li>no additional files in project,<br>only for XR SDK configuration</li><li>< 80 Types</li></ul> | <ul><li>6 packages</li><li>5 new top-level folders in your project</li><li>> 800 Types and classes</li><li>27 different ScriptableObjects with settings</li><li>18 code-generated scripts with defines etc.</li></ul> |
| Changes to project | none |  |
| Required changes | none | ARFoundation components need to be replaced with their MARS counterparts |
  
**The following table compares ARSimulation and MARS in respect to in-editor simulation for features available in ARFoundation.**  
Note that MARS has a lot of additional tools and features (functionality injection, proxies, recordings, automatic placement of objects, constraints, ...) not mentioned here that might be relevant to your usecase. See the [MARS docs](https://docs.unity3d.com/Packages/com.unity.mars@1.0/manual/MARSConcepts.html) for additional featuers.

| ⚔ | ARSimulation<br>*Simulation Features* | MARS<br>*Simulation Features* |
| -- | -- | -- |
| Plane Tracking | ✔️ | ✔️ |
| Touch Input | ✔️ | ❌<sup><a href="#comparison-table-sup-1">1</a></sup> |
| Simulated Environments | (✔️)<sup><a href="#comparison-table-sup-2">2</a></sup> | ✔️ |
| Device Simulator | ✔️ | ❌<sup><a href="#comparison-table-sup-3">3</a></sup> |
| Point Clouds | ✔️ | ✔️ |
| Image Tracking | ✔️ | ✔️ |
| Light Estimation<br>Spherical Harmonics | ✔️ | ❌ |
| Anchors | ✔️ | ❌ |
| Meshing | (✔️) | ✔️ |
| Face Tracking | ❌ | (✔️)<sup><a href="#comparison-table-sup-4">4</a></sup> |
| Object Tracking | ❌ | ❌ |
| Human Segmentation | ❌ | ❌ |
