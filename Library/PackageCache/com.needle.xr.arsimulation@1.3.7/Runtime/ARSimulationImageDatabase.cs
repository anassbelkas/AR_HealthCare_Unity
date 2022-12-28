using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation
{
	internal sealed class ARSimulationImageDatabase : MutableRuntimeReferenceImageLibrary
	{
		private readonly List<XRReferenceImage> Images = new List<XRReferenceImage>();

		public ARSimulationImageDatabase(IReferenceImageLibrary serializedLibrary)
		{
			Add(serializedLibrary);
		}

		public void Add(IReferenceImageLibrary serializedLibrary)
		{
			if (serializedLibrary != null)
			{
				for (var i = 0; i < serializedLibrary.count; i++)
				{
					var img = serializedLibrary[i];
					this.Images.Add(img);
				}
			}
		}

		public bool TryFind(Predicate<XRReferenceImage> p, out XRReferenceImage img)
		{
			img = Images.FirstOrDefault(i => p(i));
			return img != null;
		}

		public static readonly TextureFormat[] k_SupportedFormats =
		{
			TextureFormat.Alpha8,
			TextureFormat.R8,
			TextureFormat.RFloat,
			TextureFormat.RGB24,
			TextureFormat.RGBA32,
			TextureFormat.ARGB32,
			TextureFormat.BGRA32,
		};

		public override int supportedTextureFormatCount => k_SupportedFormats.Length;

		protected override JobHandle ScheduleAddImageJobImpl(NativeSlice<byte> imageBytes,
			Vector2Int sizeInPixels,
			TextureFormat format,
			XRReferenceImage referenceImage,
			JobHandle inputDeps)
		{
			if (this.Images.Contains(referenceImage)) return new JobHandle();
			this.Images.Add(referenceImage);
			return new JobHandle();
		}

		protected override TextureFormat GetSupportedTextureFormatAtImpl(int index) => k_SupportedFormats[index];


		protected override XRReferenceImage GetReferenceImageAt(int index) => Images[index];
		public override int count => this.Images.Count;

#if UNITY_ARSUBSYSTEMS_4_1_3_OR_NEWER
		protected override AddReferenceImageJobState ScheduleAddImageWithValidationJobImpl(NativeSlice<byte> imageBytes,
			Vector2Int sizeInPixels,
			TextureFormat format,
			XRReferenceImage referenceImage,
			JobHandle inputDeps)
		{
			var job = new AddReferenceImageJobState().SetLibrary(this);
			if (!referenceImage.texture)
			{
				_jobStateDict.Add(job, AddReferenceImageJobStatus.ErrorInvalidImage);
				return job;
			}

			if (this.Images.Contains(referenceImage))
			{
				_jobStateDict.Add(job, AddReferenceImageJobStatus.Success);
				return job;
			}

			this.Images.Add(referenceImage);
			_jobStateDict.Add(job, AddReferenceImageJobStatus.Success);
			return job;
		}

		private readonly Dictionary<AddReferenceImageJobState, AddReferenceImageJobStatus> _jobStateDict =
			new Dictionary<AddReferenceImageJobState, AddReferenceImageJobStatus>();

		protected override AddReferenceImageJobStatus GetAddReferenceImageJobStatus(AddReferenceImageJobState state)
		{
			if (_jobStateDict.TryGetValue(state, out var status)) return status;
			return AddReferenceImageJobStatus.None;
		}
#endif
	}

#if UNITY_ARSUBSYSTEMS_4_1_3_OR_NEWER
	internal static class AddReferenceImageJobStatusExtensions
	{
		private static FieldInfo libraryField;

		public static AddReferenceImageJobState SetLibrary(this AddReferenceImageJobState status, MutableRuntimeReferenceImageLibrary lib)
		{
			if (libraryField == null)
			{
				libraryField = typeof(AddReferenceImageJobState).GetField("m_Library", BindingFlags.Instance | BindingFlags.NonPublic);
				if (libraryField == null) return status;
			}

			var boxed = (object)status;
			libraryField.SetValue(boxed, lib);
			return (AddReferenceImageJobState)boxed;
		}
	}
#endif
}