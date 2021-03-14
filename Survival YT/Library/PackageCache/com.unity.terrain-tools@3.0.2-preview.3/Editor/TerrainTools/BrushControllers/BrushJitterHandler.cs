
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
	public class BrushJitterHandler
	{
		private readonly float m_MinValue;
		private readonly float m_MaxValue;
		
		private bool m_UseNewRandomValue;
		private float m_RandomValue;

		public float jitter { get; set; }

        public bool smoothJitter { get; set; } = false;
        public float frequency { get; set; } = 1.0f;

		public BrushJitterHandler(float jitter, float minValue, float maxValue, bool smoothJitter = false)
		{
			this.jitter = jitter;
            this.smoothJitter = smoothJitter;
			m_MinValue = minValue;
			m_MaxValue = maxValue;
		}

		public float CalculateValue(float initialValue)
		{
			return Mathf.Lerp(initialValue, initialValue + m_RandomValue, jitter); 
		}

		public void RequestRandomization()
		{
			m_UseNewRandomValue = true;
		}

		public void Update()
		{
			if(m_UseNewRandomValue)
			{
                if (smoothJitter) {
                    m_RandomValue = Mathf.Lerp(m_MinValue, m_MaxValue, Mathf.PerlinNoise(Time.time * frequency, 0.0f));
                } else {
                    m_RandomValue = Random.Range(m_MinValue, m_MaxValue);
                }
				m_UseNewRandomValue = false;
			}
		}

		public void OnGuiLayout(string toolTip)
		{
			GUIContent content = EditorGUIUtility.TrTextContent("Jitter", toolTip);
			
			jitter = EditorGUILayout.Slider(content, jitter, 0.0f, 1.0f);
		}
	}
}
