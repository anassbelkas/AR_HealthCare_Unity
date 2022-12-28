# Features

## Zero Config
When you don't configure anything and don't add any objects to your scene, ARSimulation will by default look at the AR Foundation managers in the scene and prepare your scene when entering play mode accordingly. For example if your scene has an AR Plane Manager but neither a ``SimulatedARPlane`` nor a ``SimulatedARPlaneGeneration`` component in your scene it will create a ``SimplateARPlane`` in view of the camera.
This behaviour can be disabled in ``Project Settings/XR Plug-in Management/AR Simulation``

If you need more control over your scene, you can either set up geometry and use a ``SimulatedARPlaneGeneration`` component for automatically detecting planes (similar to what happens on device) or just add ``SimulatedARPlane`` components to your scene as necessary for testing.

## AR Planes
- ``SimulatedARPlane`` is the most basic way to spawn a ``ARPane``. Just add the component to a gameobject and position it in your scene. You can use local scale x and z for changing its size.
- ``SimulatedARPlaneGeneration`` is a more advanced component. It uses raycasts to sample points in your scene to generate planes dynamically. This is a closer representation of how planes get created on device.

Note: they don't have any geometry - they are purely generating XR SDK planes, which in turn generate the proper ARFoundation 

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

## Light Estimation (Experimental)
- A ``SimulatedARCameraFrameDataProvider`` component can be used to simulate some camera data and lighting information. You can reference a light component in ``Input Light`` wich can be used to control the light direction and intensity for spherical harmonics (it is recommended to disable this light and use it only as a control).