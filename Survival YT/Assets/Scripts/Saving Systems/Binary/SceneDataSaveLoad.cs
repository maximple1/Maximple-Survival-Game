using System.Collections;
using System.Collections.Generic;
using SETUtil.Extend;
using UnityEngine;

public class SceneDataSaveLoad : MonoBehaviour
{
    [SerializeField] private Transform _savingEnvironment;

    public void SaveScene()
    {
        BinarySavingSystem.SaveScene(_savingEnvironment);
    }

    public void LoadScene()
    {
        for (int i = 0; i < _savingEnvironment.childCount; i++)
        {
            Destroy(_savingEnvironment.GetChild(i).gameObject);
        }
        SceneData data = BinarySavingSystem.LoadScene();
        for (int i = 0; i < data.objectNames.Length; i++)
        {
            
            var prefabName = GetPrefabName(data, i);

            GameObject goToSpawn = Resources.Load<GameObject>($"ItemPrefabs/{prefabName}");
            Vector3 spawnPosition = new Vector3(data.objectPositions[i].x, data.objectPositions[i].y,
                data.objectPositions[i].z);
            GameObject sceneObject = Instantiate(goToSpawn, spawnPosition, Quaternion.identity);
            sceneObject.transform.SetParent(_savingEnvironment);
            sceneObject.GetComponent<Item>().amount = data.objectAmounts[i];
        }
    }

    private static string GetPrefabName(SceneData data, int i)
    {
        string prefabName = "";
        if (data.objectNames[i].IndexOf(" ") > 0)
        {
            int whitespaceIndex = data.objectNames[i].IndexOf(" ");
            int length = data.objectNames[i].Length;
            prefabName = data.objectNames[i].Remove(whitespaceIndex, data.objectNames[i].Length-whitespaceIndex);
        }
        else
        {
            prefabName = data.objectNames[i];
        }

        return prefabName;
    }
}
