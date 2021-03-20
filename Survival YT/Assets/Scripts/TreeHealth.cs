using EnvSpawn;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeHealth : MonoBehaviour
{
    public int startHealth;
    public int health;
    public float destroyTime = 5f;
    private Transform treeSpawer;
    [SerializeField] private string spawnerName = "TreeSpawner";

    public void Start()
    {
        treeSpawer = GameObject.Find(spawnerName).transform;
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

        RespawnTree();
        Destroy(gameObject, destroyTime);

        

    }

    public void RespawnTree()
    {
        float randomX = Random.Range(treeSpawer.position.x - treeSpawer.GetComponent<EnviroSpawn_CS>().dimensions.x / 2, treeSpawer.position.x + treeSpawer.GetComponent<EnviroSpawn_CS>().dimensions.x / 2);
        float randomY = Random.Range(treeSpawer.position.z - treeSpawer.GetComponent<EnviroSpawn_CS>().dimensions.y / 2, treeSpawer.position.z + treeSpawer.GetComponent<EnviroSpawn_CS>().dimensions.y / 2);
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
                RespawnTree();
            }
        }
    }
}
