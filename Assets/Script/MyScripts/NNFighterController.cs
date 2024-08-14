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

    List<List<List<List<float>>>> policyNN = new List<List<List<List<float>>>>();
    List<List<List<List<float>>>> valueNN = new List<List<List<List<float>>>>();

    List<List<List<List<float>>>> policyDerivatives = new List<List<List<List<float>>>>();
    List<List<List<List<float>>>> valueDerivatives = new List<List<List<List<float>>>>();

    List<List<List<float>>> lastCalculation = new List<List<List<float>>>();

    List<List<List<float>>> leftLastCalculation = new List<List<List<float>>>();
    int leftLastOutputResult;
    double leftLastOutputProbability;
    float leftLastStateValue;

    List<List<List<float>>> rightLastCalculation = new List<List<List<float>>>();
    int rightLastOutputResult;
    double rightLastOutputProbability;
    float rightLastStateValue;

    float leftTimeSinceNoAttack = 0;
    float rightTimeSinceNoAttack = 0;

    bool humanVsNN;
    private List<float> output;
    private int sampleCount = 0;
    private int batchSize = 64;
    private int batchCount = 0;
    private int batchSaveSize = 1000;
    private bool rightIsCurrentNN = true;
    private bool skipOneFrameTraining = false;
    List<List<List<List<float>>>> policyAlternativeNN = new List<List<List<List<float>>>>();



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

        if(GameManager.Instance.humanVsNN)
        {
            humanVsNN = true;
        }

        Time.timeScale = aiControl.speed;

        skipOneFrameTraining = true;

        policyNN = aiControl.AISaves[currentAISave].policyNN;
        valueNN = aiControl.AISaves[currentAISave].valueNN;
    }

    public int RunNN(bool isLeftFighter)
    {
        if(!ThisInputCounts(isLeftFighter))
        {
            print("Test");
            return isLeftFighter ? leftLastOutputResult : rightLastOutputResult;
        }
    
        if(isLeftFighter)
        {
            TrainNNS();
        }

        if(humanVsNN)
        {
            output = neuralNetworkController.RunNN(policyNN, GetInput(isLeftFighter), NeuralNetworkController.ActivationFunctions.Sigmoid);
        }
        else if(!isLeftFighter && !rightIsCurrentNN)
        {
            output = neuralNetworkController.RunNN(policyAlternativeNN, GetInput(isLeftFighter), NeuralNetworkController.ActivationFunctions.Sigmoid);
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
        while(x >= 0)
        {
            chosenAction ++;
            x -= outputArray[chosenAction];
        }

        if(!humanVsNN)
        {
            if(isLeftFighter)
            {
                leftLastCalculation = lastCalculation;
                leftLastOutputResult = chosenAction;
                leftLastOutputProbability = outputArray[chosenAction];
                leftTimeSinceNoAttack = chosenAction >= 4? leftTimeSinceNoAttack ++ : 0;
            }
            else
            {
                rightLastCalculation = lastCalculation;
                rightLastOutputResult = chosenAction;
                rightLastOutputProbability = outputArray[chosenAction];
                rightTimeSinceNoAttack = chosenAction >= 4? rightTimeSinceNoAttack ++ : 0;

                if(chosenAction == 1){
                    chosenAction = 2;
                }
                else if(chosenAction == 2){
                    chosenAction = 1;
                }
                else if(chosenAction == 5){
                    chosenAction = 6;
                }
                else if(chosenAction == 6){
                    chosenAction = 5;
                }
            }
        }
        skipOneFrameTraining = false;
        return chosenAction;
    }

    private bool ThisInputCounts(bool isLeftFighter)
    {
        if(!isLeftFighter)
        {
            return battleCore.fighter1.currentActionFrame >= battleCore.fighter1.fighterData.actions[battleCore.fighter1.currentActionID].frameCount -1 || battleCore.fighter1.fighterData.actions[battleCore.fighter1.currentActionID].alwaysCancelable;
        }

        return battleCore.fighter2.currentActionFrame >= battleCore.fighter2.fighterData.actions[battleCore.fighter2.currentActionID].frameCount -1 || battleCore.fighter2.fighterData.actions[battleCore.fighter1.currentActionID].alwaysCancelable;
    }

    public void TrainNNS()
    {
        var leftVar = neuralNetworkController.RunNNAndSave(valueNN, GetInput(true), NeuralNetworkController.ActivationFunctions.Sigmoid);
        float leftThisStateValue = leftVar.output[0];
        float leftAdvantage = (float)(decayRate * leftThisStateValue - leftLastStateValue + Reward(true));
        leftAdvantage = (float)Reward(true);

        var rightVar = neuralNetworkController.RunNNAndSave(valueNN, GetInput(false), NeuralNetworkController.ActivationFunctions.Sigmoid);
        float rightThisStateValue = rightVar.output[0];
        float rightAdvantage = (float)(decayRate * rightThisStateValue - rightLastStateValue + Reward(false));

        if(!skipOneFrameTraining)
        {
            policyDerivatives.Add(neuralNetworkController.SetPartialDerivatives(policyNN, leftLastCalculation, leftLastOutputResult, true));
            policyDerivatives.LastOrDefault().LastOrDefault().LastOrDefault().Add((float)(policyLearningRate *policyTotalDecay * leftAdvantage / leftLastOutputProbability));

            valueDerivatives.Add(neuralNetworkController.SetPartialDerivatives(valueNN, leftVar.calculations));
            valueDerivatives.LastOrDefault().LastOrDefault().LastOrDefault().Add(valueLearningRate * leftAdvantage);
            
            if(rightIsCurrentNN)
            {
                policyDerivatives.Add(neuralNetworkController.SetPartialDerivatives(policyNN, rightLastCalculation, rightLastOutputResult, true));
                policyDerivatives.LastOrDefault().LastOrDefault().LastOrDefault().Add((float)(policyLearningRate *policyTotalDecay * rightAdvantage / rightLastOutputProbability));

                valueDerivatives.Add(neuralNetworkController.SetPartialDerivatives(valueNN, rightVar.calculations));
                valueDerivatives.LastOrDefault().LastOrDefault().LastOrDefault().Add(valueLearningRate * rightAdvantage);
            }
        }

        leftLastStateValue = leftThisStateValue;
        rightLastStateValue = rightThisStateValue;

        policyTotalDecay *= decayRate;

        sampleCount++;
        if(sampleCount >= batchSize)
        {
            batchCount ++;
            sampleCount = 0;

            foreach(List<List<List<float>>> derivative in policyDerivatives)
            {
                policyNN = neuralNetworkController.GradientAscent(policyNN, derivative);
            }
            policyDerivatives.Clear();

            foreach(List<List<List<float>>> derivative in valueDerivatives)
            {
                valueNN = neuralNetworkController.GradientAscent(valueNN, derivative);
            }
            valueDerivatives.Clear();

            if(batchCount >= batchSaveSize)
            {
                batchCount = 0;
                aiControl.AISaves[currentAISave].oldPolicyNNs.Add(aiControl.AISaves[currentAISave].CreateSerializedCopy(policyNN));
                aiControl.AISaves[currentAISave].policyNN = aiControl.AISaves[currentAISave].CreateSerializedCopy(policyNN);
                aiControl.AISaves[currentAISave].valueNN = aiControl.AISaves[currentAISave].CreateSerializedCopy(valueNN);

                if(UnityEngine.Random.value > 0.3)
                {
                    rightIsCurrentNN = true;
                    print("vs current NN");
                }
                else
                {
                    var rightNNIndex = (int)Math.Truncate(UnityEngine.Random.value * aiControl.AISaves[currentAISave].oldPolicyNNs.Count()-1);
                    policyAlternativeNN = aiControl.AISaves[currentAISave].oldPolicyNNs[rightNNIndex];
                    skipOneFrameTraining = true;
                    print(" vs NN: " + rightNNIndex);
                }
                aiControl.SaveFile();
            }
        }
    }

    private double Reward(bool isLeftFighter)
    {
        return RewardTest(isLeftFighter);
        double reward = 0;
        double frameAdvantage = battleCore.GetFrameAdvantage(isLeftFighter);
        frameAdvantage *= Math.Abs(battleCore.fighter1.position.x - battleCore.fighter2.position.x) < 500 ? 0.5 : 0.1;
        reward += frameAdvantage;
        reward += isLeftFighter ? battleCore.leftTotalReward : battleCore.rightTotalReward;

        battleCore.leftTotalReward = isLeftFighter ? 0 : battleCore.leftTotalReward;
        battleCore.rightTotalReward = !isLeftFighter ? 0 : battleCore.rightTotalReward;

        return reward;
    }

    private double RewardTest(bool isLeftFighter)
    {
        //print(isLeftFighter ? leftLastOutputResult*-20 + 10 : rightLastOutputResult*-20 + 10);
        return isLeftFighter ? leftLastOutputResult*-20 + 10 : rightLastOutputResult*-20 + 10;
    }

    public List<float> GetInput(bool isLeftFighter)
    {
        if(isLeftFighter)
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
        else
        {
            return new List<float>(){
                Convert.ToInt32(isLeftFighter),

                rightTimeSinceNoAttack,
                battleCore.fighter2.position.x *-1,
                battleCore.fighter2.currentActionID,
                battleCore.fighter2.currentActionFrame,
                battleCore.fighter2.currentActionFrameCount,
                battleCore.fighter2.currentHitStunFrame,
                battleCore.fighter2.guardHealth,

                leftTimeSinceNoAttack,
                battleCore.fighter1.position.x *-1,
                battleCore.fighter1.currentActionID,
                battleCore.fighter1.currentActionFrame,
                battleCore.fighter1.currentActionFrameCount,
                battleCore.fighter1.currentHitStunFrame,
                battleCore.fighter1.guardHealth,
            };
        }
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
