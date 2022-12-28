using System;
using UnityEngine;

namespace Needle.XR.ARSimulation
{
    /// <summary>
    /// This component just serves as an example implementation of an IMeshProvider/IMeshProviderWithMatrix
    /// </summary>
    public class SimulatedSimpleMeshProvider : MonoBehaviour, IMeshProviderWithMatrix
    {
        [SerializeField] private Mesh mesh = default;

        public bool Dynamic = true;

        public Mesh Mesh
        {
            get => currentlyAssignedMesh;
            set => mesh = value;// MeshUtils.CopyMesh(value);
        }

        public ulong Id => (ulong) GetInstanceID();
        public bool ApplyLocalToWorld => true;
        public Matrix4x4 LocalToWorld => transform.localToWorldMatrix;

        private Mesh currentlyAssignedMesh;

        private bool _isDirty = false;

        private void Awake()
        {
            currentlyAssignedMesh = mesh;// MeshUtils.CopyMesh(mesh);
        }

        private void OnEnable()
        {
            _isDirty = true;
        }

        private void Start()
        {
            if(!Dynamic) HandleChange();
        }

        private void OnDisable()
        {
            MeshManager.Unregister(this);
            _isDirty = false;
        }

        private void OnValidate()
        {
            _isDirty = true;
        }

        private void Update()
        {
            if (!Dynamic) return;
            if (transform.hasChanged || _isDirty)
            {
                HandleChange();
            }
        }

        private void HandleChange()
        {
            _isDirty = false;
            
            // TODO: unregistering while transform has changed because update while registered does not work anymore (MeshInfo.ChangeState)
            if (currentlyAssignedMesh != mesh)// || transform.hasChanged)
            {
                MeshManager.Unregister(this);
                currentlyAssignedMesh = mesh;
                _isDirty = true;
            }
            else
            {
                MeshManager.RegisterOrUpdate(this);
            }
            
            transform.hasChanged = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (Mesh)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.color = new Color(1, .5f, 0, .5f);
                Gizmos.DrawWireMesh(Mesh, Vector3.zero);
            }
        }


        internal static Mesh GetPrimitiveMesh(PrimitiveType primitiveType)
        {
            return Resources.GetBuiltinResource<Mesh>(GetPrimitiveMeshPath(primitiveType));
        }
        
        private static string GetPrimitiveMeshPath(PrimitiveType primitiveType)
        {
            switch (primitiveType)
            {
                case PrimitiveType.Sphere:
                    return "New-Sphere.fbx";
                case PrimitiveType.Capsule:
                    return "New-Capsule.fbx";
                case PrimitiveType.Cylinder:
                    return "New-Cylinder.fbx";
                case PrimitiveType.Cube:
                    return "Cube.fbx";
                case PrimitiveType.Plane:
                    return "New-Plane.fbx";
                case PrimitiveType.Quad:
                    return "Quad.fbx";
                default:
                    throw new ArgumentOutOfRangeException(nameof(primitiveType), primitiveType, null);
            }
        }
    }
}