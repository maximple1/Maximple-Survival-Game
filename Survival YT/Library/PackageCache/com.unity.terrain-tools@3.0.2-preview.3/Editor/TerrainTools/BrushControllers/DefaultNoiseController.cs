
using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
	public class DefaultNoiseController : IBrushNoiseController {
		private NoiseSettingsGUI m_noiseSettingsGUI;
		private NoiseSettings m_noiseSettings;

		public void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext) {

			if (m_noiseSettings == null) {
				m_noiseSettings = ScriptableObject.CreateInstance<NoiseSettings>();
				m_noiseSettings.Reset();
			}

			if (m_noiseSettingsGUI == null) {
				m_noiseSettingsGUI = new NoiseSettingsGUI();
				m_noiseSettingsGUI.Init(m_noiseSettings);
			}

			m_noiseSettingsGUI.OnGUI(NoiseSettingsGUIFlags.Settings);
		}

		public void Blit(BrushTransform brushXform, ref RenderTexture target) { }
	}
}
