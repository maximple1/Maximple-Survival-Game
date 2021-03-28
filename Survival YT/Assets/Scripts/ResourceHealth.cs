using EnvSpawn;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceHealth : MonoBehaviour
{
    public int startHealth;
    public int health;
    public float destroyTime = 5f;
    private Transform resourceSpawer;
    public ItemScriptableObject resourceType;
    public GameObject rockBreakFX;
    [SerializeField] private string spawnerName = "TreeSpawner";

    public void Start()
    {
        resourceSpawer = GameObject.Find(spawnerName).transform;
        health = startHealth;
    }
    public void TreeFall()
    {
        gameObject.AddComponent<Rigidbody>();
        Rigidbody rig = GetComponent<Rigidbody>();
        rig.isKinematic = false;
        rig.useGravity = true;
        rig.mass = 200;
        rig.constraints = RigidbodyConstraints.FreezeRotationY;

        RespawnResource();
        Destroy(gameObject, destroyTime);
    }
    public void StoneGathered()
    {
        Vector3 spawnPosition;
        if (transform.parent.parent == null)
        {
            spawnPosition = transform.position;
        }
        else
        {
            spawnPosition = transform.parent.position;
        }
        Instantiate(rockBreakFX, spawnPosition, Quaternion.identity);
        Destroy(gameObject);
    }
    public void RespawnResource()
    {
        float randomX = Random.Range(resourceSpawer.position.x - resourceSpawer.GetComponent<EnviroSpawn_CS>().dimensions.x / 2, resourceSpawer.position.x + resourceSpawer.GetComponent<EnviroSpawn_CS>().dimensions.x / 2);
        float randomY = Random.Range(resourceSpawer.position.z - resourceSpawer.GetComponent<EnviroSpawn_CS>().dimensions.y / 2, resourceSpawer.position.z + resourceSpawer.GetComponent<EnviroSpawn_CS>().dimensions.y / 2);
        Vector3 rayPos = new Vector3(randomX, 100, randomY);
        RaycastHit hit;
        if (Physics.SphereCast(rayPos, 2, Vector3.down, out hit, 200))
        {
            if (hit.collider.gameObject.layer == 10)
            {
               GameObject newTree = Instantiate(gameObject,hit.point,Quaternion.identity);
               Destroy(newTree.GetComponent<Rigidbody>());
                newTree.transform.rotation = Quaternion.identity;
            }
            else
            {
                RespawnResource();
            }
        }
    }
}
