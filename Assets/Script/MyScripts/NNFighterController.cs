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

    int debugI = 0;

    int currentAISave;

    List<List<List<List<List<float>>>>> NNList = new List<List<List<List<List<float>>>>>();

    int NNLeft;
    int NNRight;
    int maxFightPerCapita = 2;

    List<List<float>> leftFightState = new List<List<float>>();
    List<List<float>> rightFightState = new List<List<float>>();
    int maxFightStateRecord = 5;

    bool bubbleSortFlag;
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

        InitiateFightStates();
    }

    public void OldNextNNDuel(bool rightFighterWon)
    {
        //Sorts based on Revised Bubble Sort
        int n = NNList.Count-1;

        //Switch if necessary
        if(rightFighterWon)
        {
            NNList[listPos] = NNList[NNLeft];
            NNList[listPos + 1] = NNList[NNRight];
            bubbleSortFlag = true;
        }

        listPos++;

        if(listRun <= n)
        {
            if(listPos >= n)
            {
                listRun ++;
                listPos = 0;

                if(!bubbleSortFlag)
                {
                    StartNextGeneration();
                    listRun = 0;
                    listPos = 0;
                }
            }
            else
            {
                NNList[NNRight] = NNList[listPos];
                NNList[NNLeft] = NNList[listPos + 1];
            }
        }
        else
        {
            //Start next Generation
            bubbleSortFlag = false;
            listRun = 0;
            listPos = 0;
            StartNextGeneration();
        }
    }

    public void NextNNDuel(bool rightFighterWon)
    {
        float WinExpectedRight = (float)(1/(1+ Math.Pow(10, (NNList[NNLeft][0][0][0][1] - NNList[NNRight][0][0][0][1])/ 400)));
        float WinExpectedLeft = (float)(1/(1+ Math.Pow(10, (NNList[NNRight][0][0][0][1] - NNList[NNLeft][0][0][0][1])/ 400)));

        debugI ++;
        print(debugI);

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

    List<float> GetRightInputs()
    {
        List<float> output = new List<float>(){};
        foreach(List<float> list in rightFightState)
        {
            foreach(float i in list)
            {
                output.Add(i);
            }
        }
        return output;
    }

    List<float> GetLeftInputs()
    {
        List<float> output = new List<float>(){};
        foreach(List<float> list in leftFightState)
        {
            foreach(float i in list)
            {
                output.Add(i);
            }
        }
        return output;
    }

    public void UpdateFightStates()
    {
        for (int i = maxFightStateRecord - 1; i > 0; i--)
        {
            leftFightState[i] = leftFightState[i - 1];
        }

        leftFightState[0] = new List<float>(){
            1, //is Right
            battleCore.fighter2.position.x - battleCore.fighter1.position.x,

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

        for (int i = maxFightStateRecord - 1; i > 0; i--)
        {
            rightFightState[i] = rightFightState[i - 1];
        }

        rightFightState[0] = new List<float>(){
            0, //is Right
            battleCore.fighter2.position.x - battleCore.fighter1.position.x,

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

    private void InitiateFightStates()
    {
        for(int i = 0; i < maxFightStateRecord; i++)
        {
            List<float> list = new List<float>(){0,0,0,0,0,0,0,0,0,0,0,0};
            leftFightState.Add(list);
        }  
        rightFightState = leftFightState;
    }
}