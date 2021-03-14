
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
	public static class BrushUITools
	{
		public static int s_TerrainEditorHash = "TerrainEditor".GetHashCode();
		public static float ConstrainedIntField(float value, float minValue, float maxValue)
		{
			var intVal = Mathf.RoundToInt(value);
			var newVal = EditorGUILayout.IntField(intVal, GUILayout.Width(50));
			if (intVal != newVal)
				return System.Math.Min(System.Math.Max(newVal, minValue), maxValue);
			return value;
		}

		public static float MinMaxSliderWithTextBoxes(ref float min, ref float max, float minLimit, float maxLimit)
		{
			EditorGUILayout.BeginHorizontal();
			min = ConstrainedIntField(min, minLimit, max);
			EditorGUILayout.MinMaxSlider(ref min, ref max, minLimit, maxLimit);
			max = ConstrainedIntField(max, min, maxLimit);
			EditorGUILayout.EndHorizontal();

			return (min + max) * 0.5f;
		}

		public static float PercentSlider(GUIContent content, float valueInPercent, float minVal, float maxVal)
		{
			EditorGUI.BeginChangeCheck();
			float v = EditorGUILayout.Slider(content, Mathf.Round(valueInPercent * 100f), minVal * 100f, maxVal * 100f);

			if (EditorGUI.EndChangeCheck())
			{
				return v / 100f;
			}
			return valueInPercent;
		}
	}
}
