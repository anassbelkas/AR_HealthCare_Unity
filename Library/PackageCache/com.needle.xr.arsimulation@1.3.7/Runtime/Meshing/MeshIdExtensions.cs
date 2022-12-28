using System.Reflection;
using UnityEngine;
using UnityEngine.XR;

namespace Needle.XR.ARSimulation
{
    public static class MeshIdExtensions
    {
        private static FieldInfo id2Field, id1Field;

        private static bool EnsureFields()
        {
            var type = typeof(MeshId);
            if (id1Field == null)
                id1Field = type.GetField("m_SubId1", BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic);
            if (id2Field == null)
                id2Field = type.GetField("m_SubId2", BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic);
            return id2Field != null && id1Field != null;
        }

        internal static MeshId GetId(ulong id1, ulong id2)
        {
            if (!EnsureFields()) return MeshId.InvalidId;
            var mid = new MeshId();
            // boxing to make reflection work
            var obj = (object) mid;
            id1Field.SetValue(obj, id1);
            id2Field.SetValue(obj, id2);
            return (MeshId) obj;
        }

        internal static MeshId GetId(this Mesh mesh, ulong secondary)
        {
            return GetId((ulong)mesh.GetInstanceID(), secondary);
        }

        internal static MeshInfo GetInfo(this MeshId id, MeshChangeState state = MeshChangeState.Unchanged, int priorityHint = 0)
        {
            return new MeshInfo()
            {
                MeshId = id,
                ChangeState = state,
                PriorityHint = priorityHint
            };
        }
    }
}