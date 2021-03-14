using UnityEngine;

[System.Serializable]
public class TerrainGroup : MonoBehaviour
{
	public int GroupID = 0;

	public void UpdateChildTerrains()
	{
		Terrain[] childTerrains = GetComponentsInChildren<Terrain>();

		foreach (Terrain terrain in childTerrains)
		{
			GameObject existingGameObject = terrain.gameObject;
			terrain.groupingID = GroupID;
		}
	}
	
	public void DestroyChildTerrains()
	{
		Terrain[] childTerrains = GetComponentsInChildren<Terrain>();

		foreach (Terrain terrain in childTerrains)
		{
			GameObject existingGameObject = terrain.gameObject;
			DestroyImmediate(existingGameObject);
		}
	}
}
