using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NNFighterController : MonoBehaviour
{
    AIControl aiControl;
    NeuralNetworkController neuralNetworkController;

    List<List<List<List<List<float>>>>> NNList = new List<List<List<List<List<float>>>>>();

    List<List<List<List<float>>>> NNLeft = new LList<List<List<List<float>>>>();
    List<List<List<List<float>>>> NNRight = new LList<List<List<List<float>>>>();

    bool bubbleSortFlag;
    int listRun;
    int listPos;

    // Start is called before the first frame update
    void Start()
    {
        aiControl = GameObject.Find("GameMaster").GetComponent<AIControl>();
        neuralNetworkController = GameObject.Find("GameMaster").GetComponent<NeuralNetworkController>();

        NNList = aiControl.AIs  
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void NextNNDuel(bool rightFighterWon)
    {
        bool 
        //Sorts based on Revised Bubble Sort
        int n = NNList.Count()-1;

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
                    StartNextGeneration()
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
            StartNextGeneration()
        }
    }

    void StartNextGeneration()
    {

    }

    void RunNNs()
    {
        rightOutput = neuralNetworkController.RunNN(NNRight, GetRightInputs(), NeuralNetworkController.ActivationFunctions.Sigmoid);
        leftOutput = neuralNetworkController.RunNN(NNLeft, GetLeftInputs(), NeuralNetworkController.ActivationFunctions.Sigmoid);
        //Add outputs as Fighter Inputs
    }

    List<float> GetRightInputs()
    {

    }

    List<float> GetLeftInputs()
    {

    }
}