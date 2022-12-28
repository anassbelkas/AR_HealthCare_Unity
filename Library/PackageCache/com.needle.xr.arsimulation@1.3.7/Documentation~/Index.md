# AR Simulation
by [needle — tools for unity](https://needle.tools)

> Build AR apps with confidence.  
Iterate fast, right in Editor.  
Non-invasive, drop-in solution.  
Fair pricing.  

This package allows you to fly around in the Editor and test your AR app, without having to change any code or structure. Iterate faster, test out more ideas, build better apps.  
ARSimulation is a custom XR backend, built on top of the [XR plugin architecture](https://blogs.unity3d.com/2020/01/24/unity-xr-platform-updates/).

AR Simulation implements an XR backend, just as ARCore on Android or ARKit on iOS do. Because of that architecture it does work in editor just as it would on device (this holds true for all supported features, please see [Known Limitations section](#Known-limitations)). It enables creators to develop AR apps with AR Foundation and reduces iteration time significantly: just hit play in editor for testing. 

**Note: for the most up-to-date documentation, please see [AR Simulation on GitHub](https://github.com/needle-tools/ar-simulation). Thank you!**

# Quick Start ⚡

1) If your scene is empty or not setup for AR just click ``Tools/AR Simulation/Convert to Basic AR scene``
2) Click play
3) Press RMB (Right Mouse Button) + Use WASD to move around,
LMB (Left Mouse Button) to click • touch • interact with your app 

For more examples import the ``Getting Started`` samples from the Package Manager window.

# Usage 📜

AR Simulation comes with a couple of built-in components that let you create and control tracked AR planes, pointclouds or images as well as the camera (your device).

## AR Planes
- ``SimulatedARPlane`` is the most basic way to spawn a ``ARPane``. Just add the component to a gameobject and position it in your scene. You can use local scale x and z for changing its size.
- ``SimulatedARPlaneGeneration`` is a more advanced component. It uses raycasts to sample points in your scene to generate planes dynamically. This is a closer representation of how planes get created on device.

## AR Tracked Images
- ``SimulatedARTrackedImage`` can be used to simulate image tracking. You can track any image that is used in a ``XRReferenceImageLibrary`` asset. By default tracking automatically uses the camera frustum to update its ``Tracking State``.

## AR PointClouds
- ``SimulatedARPointCloud`` is the most basic way to spawn a ``ARPointCloud``. By default is generates random points in either a spherical or a planar shape but it's also possible to edit points directly in editor or via code.
- ``SimulatedARPointCloudRaycaster`` uses raycasts to sample points in your scene. 

## AR Anchors
- Supported but we don't have a editor component implementation as with the other features right now. 
You should be able to spawn anchors with AR Foundation and see them being created just as on device.

## Background Image (Experimental)
- ``SimulatedAREnvironment`` can be added to your root environment gameobject (the gameobject that contains objects that you want to render as a camera image). You can enable ``IsActive`` to assign itself to a ``SimulatedAREnvironemntManager`` which does the heavy lifting.
- ``SimulatedAREnvironmentManager`` can be added to a scene for handling camera background rendering. The ``Scene Or Prefab`` field can be used to reference either a scene asset, a prefab or a gameobject in the current scene to be rendered as a camera image.


# Technical Details 🔬

## Requirements
- Unity 2019.3 and above
- AR Foundation 3 and above
- Support for: Device Simulator
- Support for: New Input System / Legacy Input System / Both
- Support for: URP / Built-in

## Known limitations

- Camera background is supported (with custom 3D scenes), but no occlusion support right now
- Environment cubemaps is platform-specific and currently not supported. [Issue tracker](https://issuetracker.unity3d.com/product/unity/issues/guid/1215635)
- No support for simulating faces, people, or collaboration right now
(let us know if you feel this is important to you!)
- Partial support for meshing simulation
(some support, but not identical to specific devices)
- Object tracking is not yet supported
- Touch input is single-touch for now, waiting for Unity to support it better
(Device Simulator only supports single touch, since Input.SimulateTouch only supports one)
- There's a number of warnings around subsystem usage in Editor. They seem to not matter much but are annoying (and incorrect).
- Device Simulator disables Mouse input completely - we're working around that here but be aware when you try to create Android / iOS apps that also support mouse. [Forum Thread](https://forum.unity.com/threads/new-device-simulator-preview.751067/page-4#post-5952482)
- in 2020.1 and 2020.2, even when you enable "New Input System", the Input System package is not installed in package manager. You have to install it manually. [Forum Thread](https://forum.unity.com/threads/new-input-system-not-installed-in-2020-1-after-enabling-it.908027/)
- switching from a scene with Object Tracking to a scene with Image Tracking on device crashes Android apps (we'll report a bug soon)
- If your scene feels to dark / does not use environment lighting, make sure "Auto Generate" is on in Lighting Window or bake light data.
(spherical harmonics simulation will only work if the shaders are aware that they should use it)
- AR Simulation currently has a dependency on XRLegacyInputHelpers that isn't needed in call cases; we will remove that dependency in a future release.

# Contact ✍️

[Forum](https://forum.needle.tools/ar-simulation) • [Discord](https://discord.gg/CFZDp4b)


<b>[needle — tools for unity](https://needle.tools)</b> • 
[@NeedleTools](https://twitter.com/NeedleTools) • 
[@marcel_wiessler](https://twitter.com/marcel_wiessler) • 
[@hybridherbst](https://twitter.com/hybdridherbst) • 
[Say hi!](mailto:hi@needle.tools?subject=Hi!)

<div style="page-break-after: always; visibility: hidden"></div>

# Documentation

:[Features](Features.md)

<div style="page-break-after: always; visibility: hidden"></div>

:[Compatibility](Compatibility.md)

<div style="page-break-after: always; visibility: hidden"></div>

# Revision History
| Date | Reason |
| ---- | ------ |
| 2020-06-27 | Added forum link, sub documents and prepared PDF |
| 2020-06-22 | Updated documentation |
| 2020-06-01 | Initial public release |