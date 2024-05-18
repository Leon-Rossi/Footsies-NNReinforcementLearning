using System.Collections;
using System.Collections.Generic;
using Footsies;
using UnityEngine;

public class NNFighterController : MonoBehaviour
{
    AIControl aiControl;
    NeuralNetworkController neuralNetworkController;
    BattleCore battleCore;

    int currentAISave;

    List<List<List<List<List<float>>>>> NNList = new List<List<List<List<List<float>>>>>();

    List<List<List<List<float>>>> NNLeft = new List<List<List<List<float>>>>();
    List<List<List<List<float>>>> NNRight = new List<List<List<List<float>>>>();

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
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void NextNNDuel(bool rightFighterWon)
    {
        //Sorts based on Revised Bubble Sort
        int n = NNList.Count-1;

        //Switch if necessary
        if(rightFighterWon)
        {
            NNList[listPos] = NNLeft;
            NNList[listPos + 1] = NNRight;
            bubbleSortFlag = true;
        }

        if(listRun !> n)
        {
            if(listPos >= n)
            {
                listRun ++;
                listPos = 0;
                if(bubbleSortFlag)
                {
                    StartNextGeneration();
                    bubbleSortFlag = false;
                }
            }
            else
            {
                NNRight = NNList[listPos];
                NNLeft = NNList[listPos + 1];
            }
        }
        else
        {
            //Start next Generation
            bubbleSortFlag = false;
            StartNextGeneration();
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

        List<float> output = neuralNetworkController.RunNN(NNRight, GetRightInputs(), NeuralNetworkController.ActivationFunctions.Sigmoid);
        
        //Add outputs as Fighter Inputs
        if(output[0] > 0.5)
        {
            attack = true;
        }
        if(output[1] < 0.4)
        {
            moveLeft = true;
        }
        else if(output[2] > 0.6)
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

        List<float> output = neuralNetworkController.RunNN(NNLeft, GetLeftInputs(), NeuralNetworkController.ActivationFunctions.Sigmoid);
        
        //Add outputs as Fighter Inputs
        if(output[0] > 0.5)
        {
            attack = true;
        }
        if(output[1] < 0.4)
        {
            moveLeft = true;
        }
        else if(output[2] > 0.6)
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
        for (int i = maxFightStateRecord; i > 0; i--)
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

        for (int i = maxFightStateRecord; i > 0; i--)
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
}