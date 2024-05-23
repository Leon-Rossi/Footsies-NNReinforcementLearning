using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Footsies;
using UnityEngine;
using UnityEngine.Animations;

public class NNFighterController : MonoBehaviour
{
    AIControl aiControl;
    NeuralNetworkController neuralNetworkController;
    BattleCore battleCore;

    int currentAISave;

    List<List<List<List<List<float>>>>> NNList = new List<List<List<List<List<float>>>>>();

    int NNLeft;
    int NNRight;
    int maxFightPerCapita = 10;

    float leftTimeSinceNoAttack;
    float rightTimeSinceNoAttack;

    int listRun;
    int listPos;
    

    // Start is called before the first frame update
    void Awake()
    {
        aiControl = GameObject.Find("GameMaster").GetComponent<AIControl>();
        neuralNetworkController = GameObject.Find("GameMaster").GetComponent<NeuralNetworkController>();
        battleCore = GameObject.Find("BattleCore").GetComponent<BattleCore>();

        currentAISave = aiControl.currentAISave;
        NNList = aiControl.AISaves[currentAISave].GiveLastGeneration();

        listPos = 0;
        listRun = 0;
        NNRight = 0;

        float smallestValue = 10000;
        for(int i = 0; i <= NNList.Count -1; i++)
        {
            if(NNList[NNRight][0][0][0][1] - NNList[i][0][0][0][1] < smallestValue)
            {
                NNLeft = i;
                smallestValue = NNList[NNRight][0][0][0][1] - NNList[i][0][0][0][1];
            }
        }

        Time.timeScale = aiControl.speed;
    }

    public void NextNNDuel(bool rightFighterWon)
    {
        float WinExpectedRight = (float)(1/(1+ Math.Pow(10, (NNList[NNLeft][0][0][0][1] - NNList[NNRight][0][0][0][1])/ 400)));
        float WinExpectedLeft = (float)(1/(1+ Math.Pow(10, (NNList[NNRight][0][0][0][1] - NNList[NNLeft][0][0][0][1])/ 400)));

        if(rightFighterWon)
        {
            NNList[NNRight][0][0][0][1] += 30 * (1-WinExpectedRight);
            NNList[NNLeft][0][0][0][1] += 30 * (0-WinExpectedLeft);
        }
        else
        {
            NNList[NNRight][0][0][0][1] += 30 * (0-WinExpectedRight);
            NNList[NNLeft][0][0][0][1] += 30 * (1-WinExpectedLeft);
        }

        if(listRun < maxFightPerCapita -1)
        {
            NNRight = listPos;

            float smallestValue = 10000;
            for(int i = 0; i <= NNList.Count -1; i++)
            {
                if(NNList[NNRight][0][0][0][1] - NNList[i][0][0][0][1] < smallestValue)
                {
                    NNLeft = i;
                    smallestValue = NNList[NNRight][0][0][0][1] - NNList[i][0][0][0][1];
                }
            }

            listPos ++;
            if(listPos >= NNList.Count)
            {
                listPos = 0;
                listRun ++;
            }
        }
        else
        {
            StartNextGeneration();
            listPos = 0;
            listRun = 0;
        }
    }

    void StartNextGeneration()
    {
        aiControl.AISaves[currentAISave].ReplaceLastGeneration(NNList);
        aiControl.AISaves[currentAISave].SetUpNextGeneration();
        NNList = aiControl.AISaves[currentAISave].GiveLastGeneration();
    }

    public int RunRightNN()
    {
        bool attack = false;
        bool moveRight = false;
        bool moveLeft = false;

        int inputData = 0;

        List<float> output = neuralNetworkController.RunNN(NNList[NNRight], GetRightInputs(), NeuralNetworkController.ActivationFunctions.Sigmoid);
        
        //Add outputs as Fighter Inputs
        if(output[0] > 0.5)
        {
            attack = true;
        }
        if(output[1] < 0.4)
        {
            moveLeft = true;
        }
        else if(output[1] > 0.6)
        {
            moveRight = true;
        }

        inputData |= attack ? (int)InputDefine.Attack : 0;
        inputData |= moveLeft ? (int)InputDefine.Left : 0;
        inputData |= moveRight ? (int)InputDefine.Right : 0;

        return inputData;
    }

    public int RunLeftNN()
    {
        bool attack = false;
        bool moveRight = false;
        bool moveLeft = false;

        int inputData = 0;

        List<float> output = neuralNetworkController.RunNN(NNList[NNLeft], GetLeftInputs(), NeuralNetworkController.ActivationFunctions.Sigmoid);
        
        //Add outputs as Fighter Inputs
        if(output[0] > 0.5)
        {
            attack = true;
        }
        if(output[1] < 0.4)
        {
            moveLeft = true;
        }
        else if(output[1] > 0.6)
        {
            moveRight = true;
        }

        inputData |= attack ? (int)InputDefine.Attack : 0;
        inputData |= moveLeft ? (int)InputDefine.Left : 0;
        inputData |= moveRight ? (int)InputDefine.Right : 0;

        return inputData;
    }

    public List<float> GetLeftInputs()
    {
        if(battleCore.fighter1.currentActionID == (int)CommonActionID.N_ATTACK || battleCore.fighter1.currentActionID == (int)CommonActionID.B_ATTACK)
        {
            leftTimeSinceNoAttack += Time.deltaTime;
        }
        else
        {
            leftTimeSinceNoAttack = 0;
        }
    
        return new List<float>(){
            1, //is Right
            battleCore.fighter2.position.x - battleCore.fighter1.position.x,
            leftTimeSinceNoAttack,

            battleCore.fighter1.currentActionID == (float)CommonActionID.DAMAGE ? 1 : 0,
            battleCore.fighter1.currentActionID == (int)CommonActionID.GUARD_BREAK ? 1 : 0,
            battleCore.fighter1.currentActionID == (int)CommonActionID.GUARD_CROUCH
                                                    || battleCore.fighter1.currentActionID == (int)CommonActionID.GUARD_STAND
                                                    || battleCore.fighter1.currentActionID == (int)CommonActionID.GUARD_M ? 1 : 0,
            battleCore.fighter1.currentActionID == (int)CommonActionID.N_ATTACK
                                                    || battleCore.fighter1.currentActionID == (int)CommonActionID.B_ATTACK ? 1 : 0,
            battleCore.fighter1.currentActionID == (int)CommonActionID.N_SPECIAL
                                                    || battleCore.fighter1.currentActionID == (int)CommonActionID.B_SPECIAL ? 1 : 0,

            battleCore.fighter2.currentActionID == (float)CommonActionID.DAMAGE ? 1 : 0,
            battleCore.fighter2.currentActionID == (int)CommonActionID.GUARD_BREAK ? 1 : 0,
            battleCore.fighter2.currentActionID == (int)CommonActionID.GUARD_CROUCH
                                                    || battleCore.fighter2.currentActionID == (int)CommonActionID.GUARD_STAND
                                                    || battleCore.fighter2.currentActionID == (int)CommonActionID.GUARD_M ? 1 : 0,
            battleCore.fighter2.currentActionID == (int)CommonActionID.N_ATTACK
                                                    || battleCore.fighter2.currentActionID == (int)CommonActionID.B_ATTACK ? 1 : 0,
            battleCore.fighter2.currentActionID == (int)CommonActionID.N_SPECIAL
                                                    || battleCore.fighter2.currentActionID == (int)CommonActionID.B_SPECIAL ? 1 : 0,
        };
    }

    public List<float> GetRightInputs()
    {
        if(battleCore.fighter1.currentActionID == (int)CommonActionID.N_ATTACK || battleCore.fighter1.currentActionID == (int)CommonActionID.B_ATTACK)
        {
            rightTimeSinceNoAttack += Time.deltaTime;
        }
        else
        {
            rightTimeSinceNoAttack = 0;
        }

        return new List<float>(){
            0, //is Right
            battleCore.fighter2.position.x - battleCore.fighter1.position.x,
            rightTimeSinceNoAttack,

            battleCore.fighter2.currentActionID == (float)CommonActionID.DAMAGE ? 1 : 0,
            battleCore.fighter2.currentActionID == (int)CommonActionID.GUARD_BREAK ? 1 : 0,
            battleCore.fighter2.currentActionID == (int)CommonActionID.GUARD_CROUCH
                                                    || battleCore.fighter2.currentActionID == (int)CommonActionID.GUARD_STAND
                                                    || battleCore.fighter2.currentActionID == (int)CommonActionID.GUARD_M ? 1 : 0,
            battleCore.fighter2.currentActionID == (int)CommonActionID.N_ATTACK
                                                    || battleCore.fighter2.currentActionID == (int)CommonActionID.B_ATTACK ? 1 : 0,
            battleCore.fighter2.currentActionID == (int)CommonActionID.N_SPECIAL
                                                    || battleCore.fighter2.currentActionID == (int)CommonActionID.B_SPECIAL ? 1 : 0,

            battleCore.fighter1.currentActionID == (float)CommonActionID.DAMAGE ? 1 : 0,
            battleCore.fighter1.currentActionID == (int)CommonActionID.GUARD_BREAK ? 1 : 0,
            battleCore.fighter1.currentActionID == (int)CommonActionID.GUARD_CROUCH
                                                    || battleCore.fighter1.currentActionID == (int)CommonActionID.GUARD_STAND
                                                    || battleCore.fighter1.currentActionID == (int)CommonActionID.GUARD_M ? 1 : 0,
            battleCore.fighter1.currentActionID == (int)CommonActionID.N_ATTACK
                                                    || battleCore.fighter1.currentActionID == (int)CommonActionID.B_ATTACK ? 1 : 0,
            battleCore.fighter1.currentActionID == (int)CommonActionID.N_SPECIAL
                                                    || battleCore.fighter1.currentActionID == (int)CommonActionID.B_SPECIAL ? 1 : 0,

        };
    }
}