using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NNFighterController : MonoBehaviour
{
    AIControl aiControl;
    
    // Start is called before the first frame update
    void Start()
    {
        aiControl = GameObject.Find("GameMaster").GetComponent<AIControl>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
