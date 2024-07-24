using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Footsies;
using UnityEngine;

public class NNFighterController : MonoBehaviour
{
    AIControl aiControl;
    NeuralNetworkController neuralNetworkController;
    BattleCore battleCore;
    SelectNN selectNN;

    float decayRate = 1;
    float policyLearningRate = 0.05f;
    float policyTotalDecay = 1;

    float valueLearningRate;

    int currentAISave;

    List<List<List<List<List<float>>>>> NNList = new List<List<List<List<List<float>>>>>();

    List<List<List<List<float>>>> policyNN = new List<List<List<List<float>>>>();
    List<List<List<List<float>>>> valueNN = new List<List<List<List<float>>>>();

    List<List<List<float>>> lastCalculation = new List<List<List<float>>>();

    List<List<List<float>>> leftLastCalculation = new List<List<List<float>>>();
    int leftLastOutputResult;
    double leftLastOutputProbability;
    bool leftAction;
    float leftLastStateValue;

    List<List<List<float>>> rightLastCalculation = new List<List<List<float>>>();
    int rightLastOutputResult;
    double rightLastOutputProbability;
    bool rightAction;
    float rightLastStateValue;

    float leftTimeSinceNoAttack = 0;
    float rightTimeSinceNoAttack = 0;

    bool isVsNN;
    private List<float> output;


    // Start is called before the first frame update
    void Awake()
    {
        aiControl = GameObject.Find("GameMaster").GetComponent<AIControl>();
        neuralNetworkController = GameObject.Find("GameMaster").GetComponent<NeuralNetworkController>();
        battleCore = GameObject.Find("BattleCore").GetComponent<BattleCore>();
        selectNN = GameObject.Find("MenuManager").GetComponent<SelectNN>();

        currentAISave = aiControl.currentAISave;
        decayRate = aiControl.AISaves[currentAISave].decayRate;
        policyLearningRate = aiControl.AISaves[currentAISave].policyLearningRate;

        valueLearningRate = aiControl.AISaves[currentAISave].valueLearningRate;

        if(GameManager.Instance.isVsNN)
        {
            isVsNN = true;
        }

        Time.timeScale = aiControl.speed;

        leftLastStateValue = 0;
        rightLastStateValue = 0;

        policyNN = aiControl.AISaves[currentAISave].policyNN;
        valueNN = aiControl.AISaves[currentAISave].valueNN;
    }

    public int RunNN(bool isLeftFighter)
    {
        if(isVsNN)
        {
            output = neuralNetworkController.RunNN(policyNN, GetInput(isLeftFighter), NeuralNetworkController.ActivationFunctions.Sigmoid);
        }
        else
        {
            var outputVar = neuralNetworkController.RunNNAndSave(policyNN, GetInput(isLeftFighter), NeuralNetworkController.ActivationFunctions.Sigmoid);
            output = outputVar.output;
            lastCalculation = outputVar.calculations;
        }

        double[] outputArray = SoftMaxFunction(output);

        double x = UnityEngine.Random.value;
        int chosenAction = -1;
        while(x > 0)
        {
            chosenAction ++;
            x -= outputArray[chosenAction];
        }
        print(outputArray.Count());
        print(chosenAction);

        if(!isVsNN)
        {
            if(isLeftFighter)
            {
                leftLastCalculation = lastCalculation;
                leftLastOutputResult = chosenAction;
                leftLastOutputProbability = outputArray[chosenAction];
                leftAction = true;
                leftTimeSinceNoAttack = chosenAction >= 4? leftTimeSinceNoAttack ++ : 0;
            }
            else
            {
                rightLastCalculation = lastCalculation;
                rightLastOutputResult = chosenAction;
                rightLastOutputProbability = outputArray[chosenAction];
                rightAction = true;
                rightTimeSinceNoAttack = chosenAction >= 4? rightTimeSinceNoAttack ++ : 0;
            }

            if(leftAction && rightAction)
            {
                leftAction = false;
                rightAction = false;
                
                TrainNNS();        
            }
        }

        return chosenAction;
    }

    public void TrainNNS()
    {
        var leftVar = neuralNetworkController.RunNNAndSave(valueNN, GetInput(true), NeuralNetworkController.ActivationFunctions.Sigmoid);
        float leftThisStateValue = leftVar.output[0];
        float leftAdvantage = decayRate * leftThisStateValue - leftLastStateValue + Reward(true);

        policyNN = neuralNetworkController.SetPartialDerivatives(policyNN, leftLastCalculation, leftLastOutputResult, true);
        policyNN = neuralNetworkController.OneStopMethod(policyNN, (float)(policyLearningRate *policyTotalDecay * leftAdvantage / leftLastOutputProbability));

        valueNN = neuralNetworkController.SetPartialDerivatives(valueNN, leftVar.calculations);
        valueNN = neuralNetworkController.OneStopMethod(valueNN, valueLearningRate * leftAdvantage);
        
        var rightVar = neuralNetworkController.RunNNAndSave(valueNN, GetInput(false), NeuralNetworkController.ActivationFunctions.Sigmoid);
        float rightThisStateValue = rightVar.output[0];
        float rightAdvantage = decayRate * rightThisStateValue - rightLastStateValue + Reward(false);

        policyNN = neuralNetworkController.SetPartialDerivatives(policyNN, rightLastCalculation, rightLastOutputResult, true);
        policyNN = neuralNetworkController.OneStopMethod(policyNN, (float)(policyLearningRate *policyTotalDecay * rightAdvantage / rightLastOutputProbability));

        valueNN = neuralNetworkController.SetPartialDerivatives(valueNN, rightVar.calculations);
        valueNN = neuralNetworkController.OneStopMethod(valueNN, valueLearningRate * rightAdvantage);

        leftLastStateValue = leftThisStateValue;
        rightLastStateValue = rightThisStateValue;

        policyTotalDecay *= decayRate;
    }

    private float Reward(bool isLeftFighter)
    {
        int reward = 0;
        reward += battleCore.GetFrameAdvantage(isLeftFighter);
        reward += isLeftFighter ? battleCore.leftTotalReward : battleCore.rightTotalReward;

        battleCore.leftTotalReward = isLeftFighter ? 0 : battleCore.leftTotalReward;
        battleCore.rightTotalReward = !isLeftFighter ? 0 : battleCore.rightTotalReward;

        return reward;
    }

    public List<float> GetInput(bool isLeftFighter)
    {
        return new List<float>(){
            Convert.ToInt32(isLeftFighter),

            leftTimeSinceNoAttack,
            battleCore.fighter1.position.x,
            battleCore.fighter1.currentActionID,
            battleCore.fighter1.currentActionFrame,
            battleCore.fighter1.currentActionFrameCount,
            battleCore.fighter1.currentHitStunFrame,
            battleCore.fighter1.guardHealth,

            rightTimeSinceNoAttack,
            battleCore.fighter2.position.x,
            battleCore.fighter2.currentActionID,
            battleCore.fighter2.currentActionFrame,
            battleCore.fighter2.currentActionFrameCount,
            battleCore.fighter2.currentHitStunFrame,
            battleCore.fighter2.guardHealth,
        };
    }

    //https://gist.github.com/jogleasonjr/55641e503142be19c9d3692b6579f221
    double[] SoftMaxFunction(List<float> input)
    {
        double[] inputArray = Array.ConvertAll(input.ToArray(), x => (double)x);
        var inputArray_exp = inputArray.Select(Math.Exp);
        var sum_inputArray_exp = inputArray_exp.Sum();

        return inputArray_exp.Select(i => i / sum_inputArray_exp).ToArray();
    }
}
