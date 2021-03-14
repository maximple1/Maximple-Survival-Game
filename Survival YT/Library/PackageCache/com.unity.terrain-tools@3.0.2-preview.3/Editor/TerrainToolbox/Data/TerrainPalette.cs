using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.TerrainAPI
{
	[Serializable]
	public class TerrainPalette : ScriptableObject
	{
		public List<TerrainLayer> PaletteLayers = new List<TerrainLayer>();
	}
}
