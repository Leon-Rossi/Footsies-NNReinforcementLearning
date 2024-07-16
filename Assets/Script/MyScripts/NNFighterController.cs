using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Footsies;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.XR;

public class NNFighterController : MonoBehaviour
{
    AIControl aiControl;
    NeuralNetworkController neuralNetworkController;
    BattleCore battleCore;
    SelectNN selectNN;

    int currentAISave;

    List<List<List<List<List<float>>>>> NNList = new List<List<List<List<List<float>>>>>();

    float leftTimeSinceNoAttack;
    float rightTimeSinceNoAttack;

    bool isVsNN;
    

    // Start is called before the first frame update
    void Awake()
    {
        aiControl = GameObject.Find("GameMaster").GetComponent<AIControl>();
        neuralNetworkController = GameObject.Find("GameMaster").GetComponent<NeuralNetworkController>();
        battleCore = GameObject.Find("BattleCore").GetComponent<BattleCore>();
        selectNN = GameObject.Find("MenuManager").GetComponent<SelectNN>();

        currentAISave = aiControl.currentAISave;
        NNList = aiControl.AISaves[currentAISave].GiveLastGeneration();

        Time.timeScale = aiControl.speed;
    }

    public int RunRightNN()
    {
        outputArry = 
    }

 6   public int RunLeftNN()
    {
    }

    public List<float> GetLeftInputs()
    {
        return new List<float>(){
            Math.Abs(battleCore.fighter2.position.x - battleCore.fighter1.position.x),

            leftTimeSinceNoAttack,
            battleCore.fighter1.currentActionID,
            battleCore.fighter1.currentActionFrame,
            battleCore.fighter1.currentHitStunFrame,
            battleCore.fighter1.guardHealth,

            rightTimeSinceNoAttack,
            battleCore.fighter2.currentActionID,
            battleCore.fighter2.currentActionFrame,
            battleCore.fighter2.currentHitStunFrame,
            battleCore.fighter2.guardHealth,
        };
    }

    public List<float> GetRightInputs()
    {
        return new List<float>(){
            Math.Abs(battleCore.fighter2.position.x - battleCore.fighter1.position.x),

            rightTimeSinceNoAttack,
            battleCore.fighter2.currentActionID,
            battleCore.fighter2.currentActionFrame,
            battleCore.fighter2.currentHitStunFrame,
            battleCore.fighter2.guardHealth,

            leftTimeSinceNoAttack,
            battleCore.fighter1.currentActionID,
            battleCore.fighter1.currentActionFrame,
            battleCore.fighter1.currentHitStunFrame,
            battleCore.fighter1.guardHealth,
        };
    }
}
