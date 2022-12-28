using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Needle.XR.ARSimulation.Extensions
{
	public static class TextureFormatHelper
	{
		public static int CalcPixelStride(this Texture2D texture, int byteCount)
		{
			if (!texture) return -1;
			var cc = GraphicsFormatUtility.GetComponentCount(texture.graphicsFormat);
			// TODO: check if GraphicsFormatUtility.GetBlockSize outputs the pixel stride
			return (int) (byteCount / (texture.width * texture.height * cc));
		}
	}
}