using UnityEngine;

namespace Needle.XR.ARSimulation.Simulation
{
	internal static class LightHelpers
	{
		public static float ConvertBrightnessToLumen(float brightness)
		{
			const float kMaxLuminosity = 2000.0f;
			return Mathf.Clamp(brightness * kMaxLuminosity, 0f, kMaxLuminosity);
		}

		public static float ConvertBrightnessFromLumen(float brightness)
		{
			const float kMaxLuminosity = 2000.0f;
			return Mathf.Clamp(brightness / kMaxLuminosity, 0f, 1);
		}

		public static Color ColorTemperatureToRGB(float kelvin)
		{
			var temp = kelvin / 100;
			float red, green, blue;

			if (temp <= 66)
			{
				red = 255;
				green = temp;
				green = 99.4708025861f * Mathf.Log(green) - 161.1195681661f;

				if (temp <= 19)
				{
					blue = 0;
				}
				else
				{
					blue = temp - 10;
					blue = 138.5177312231f * Mathf.Log(blue) - 305.0447927307f;
				}
			}
			else
			{
				red = temp - 60;
				red = 329.698727446f * Mathf.Pow(red, -0.1332047592f);

				green = temp - 60;
				green = 288.1221695283f * Mathf.Pow(green, -0.0755148492f);

				blue = 255;
			}

			return new Color(Mathf.Clamp01(red / 255f), Mathf.Clamp01(green / 255f), Mathf.Clamp01(blue / 255f));
		}
	}
}