using UnityEngine;
using UnityEngine.UI;
public class Test : MonoBehaviour
{

    public GameObject Seekerbot;
    public GameObject Hiderbot;
    public GameObject BasePillar;


    public Transform spawnPoint;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    public void SpawnSeekerBots()
    {
            Instantiate(Seekerbot, spawnPoint.position, spawnPoint.rotation);
    
        
    }
    public void SpawnHiderBots()
    {
            
    
            Instantiate(Hiderbot, spawnPoint.position, spawnPoint.rotation);
       
    }
    public void SpawnBasePillar()
    {
            
    
            Instantiate(BasePillar, spawnPoint.position, spawnPoint.rotation);
       
    }
}

