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

    float decay = 1;
    float totalDecay = 1;
    float learningRate = 0.05f;

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

    float leftTimeSinceNoAttack;
    float rightTimeSinceNoAttack;

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

        if(GameManager.Instance.isVsNN)
        {
            isVsNN = true;
        }

        Time.timeScale = aiControl.speed;

        leftLastStateValue = neuralNetworkController.RunNN(valueNN, GetInput(true), NeuralNetworkController.ActivationFunctions.Sigmoid)[0];
        rightLastStateValue = neuralNetworkController.RunNN(valueNN, GetInput(false), NeuralNetworkController.ActivationFunctions.Sigmoid)[0];

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
            List<List<List<float>>> lastCalculation = outputVar.calculations;
        }

        double[] outputArray = SoftMaxFunction(output);

        double x = 0;
        int chosenAction = -1;
        while(x > 0)
        {
            chosenAction ++;
            x -= outputArray[chosenAction];
        }

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

        return chosenAction;
    }

    public void TrainNNS()
    {
        //Policy NN
        var leftVar = neuralNetworkController.RunNNAndSave(valueNN, GetInput(true), NeuralNetworkController.ActivationFunctions.Sigmoid);
        float leftThisStateValue = leftVar.output[0];
        float leftAdvantage = decay * leftThisStateValue - leftLastStateValue + Reward(true);

        policyNN = neuralNetworkController.SetPartialDerivatives(policyNN, leftLastCalculation, leftLastOutputResult, true);
        policyNN = neuralNetworkController.OneStopMethod(policyNN, (float)(learningRate *totalDecay * leftAdvantage / leftLastOutputProbability));

        valueNN = neuralNetworkController.SetPartialDerivatives(valueNN, leftVar.calculations);
        valueNN = neuralNetworkController.OneStopMethod(valueNN, learningRate * leftAdvantage);
        
        var rightVar = neuralNetworkController.RunNNAndSave(valueNN, GetInput(false), NeuralNetworkController.ActivationFunctions.Sigmoid);
        float rightThisStateValue = rightVar.output[0];
        float rightAdvantage = decay * rightThisStateValue - rightLastStateValue + Reward(false);

        policyNN = neuralNetworkController.SetPartialDerivatives(policyNN, rightLastCalculation, rightLastOutputResult, true);
        policyNN = neuralNetworkController.OneStopMethod(policyNN, (float)(learningRate *totalDecay * rightAdvantage / rightLastOutputProbability));

        valueNN = neuralNetworkController.SetPartialDerivatives(valueNN, rightVar.calculations);
        valueNN = neuralNetworkController.OneStopMethod(valueNN, learningRate * rightAdvantage);

        leftLastStateValue = leftThisStateValue;
        rightLastStateValue = rightThisStateValue;

        totalDecay *= decay;
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

    //https://gist.github.com/jogleasonjr/55641e503142be19c9d3692b6579f221
    double[] SoftMaxFunction(List<float> input)
    {
    double[] inputArray = Array.ConvertAll(input.ToArray(), x => (double)x);
	var inputArray_exp = inputArray.Select(Math.Exp);
	var sum_inputArray_exp = inputArray_exp.Sum();

	return (double[])inputArray_exp.Select(i => i / sum_inputArray_exp);
    }
}
