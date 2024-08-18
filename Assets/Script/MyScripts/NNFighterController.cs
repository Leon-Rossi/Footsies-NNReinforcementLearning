using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.InteropServices;
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
    private int batchSize = 32;
    private int batchCount = 0;
    private int batchSaveSize = 100;
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
            return isLeftFighter ? leftLastOutputResult : rightLastOutputResult;
        }
        
        if(isLeftFighter)
        {
            TrainNNS();
            skipOneFrameTraining = false;
        }

        if(humanVsNN)
        {
            output = neuralNetworkController.RunNN(policyNN, GetInput(isLeftFighter));
        }
        else if(!isLeftFighter && !rightIsCurrentNN)
        {
            output = neuralNetworkController.RunNN(policyAlternativeNN, GetInput(isLeftFighter));
        }
        else
        {
            var outputVar = neuralNetworkController.RunNNAndSave(policyNN, GetInput(isLeftFighter));
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
        if(isLeftFighter)
        {
            //print(output[0] + " " + output[1] + " " + output[2] + " " + output[3] + " " + output[4] + " " + output[5] + " " + output[6]);
            //print(outputArray[0] + " " + outputArray[1] + " " + outputArray[2] + " " + outputArray[3] + " " + outputArray[4] + " " + outputArray[5] + " " + outputArray[6] + " " + chosenAction);
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
        return chosenAction;
    }

    private bool ThisInputCounts(bool isLeftFighter)
    {
        if(!isLeftFighter)
        {
            return battleCore.fighter1.currentActionFrame == battleCore.fighter1.fighterData.actions[battleCore.fighter1.currentActionID].frameCount -1 || battleCore.fighter1.fighterData.actions[battleCore.fighter1.currentActionID].alwaysCancelable;
        }

        return battleCore.fighter2.currentActionFrame == battleCore.fighter2.fighterData.actions[battleCore.fighter2.currentActionID].frameCount -1 || battleCore.fighter2.fighterData.actions[battleCore.fighter1.currentActionID].alwaysCancelable;
    }

    public void TrainNNS()
    {
        float leftThisStateValue = neuralNetworkController.RunNN(valueNN, GetInput(true))[0] + 100;
        float leftAdvantage = (float)(decayRate * leftThisStateValue - leftLastStateValue + Reward(true));

        float rightThisStateValue = neuralNetworkController.RunNN(valueNN, GetInput(false))[0] + 100;
        float rightAdvantage = (float)(decayRate * rightThisStateValue - rightLastStateValue + Reward(false));

        if(!skipOneFrameTraining)
        {
            policyDerivatives.Add(neuralNetworkController.SetPartialDerivatives(policyNN, leftLastCalculation, leftLastOutputResult, true));
            policyDerivatives.LastOrDefault().LastOrDefault().LastOrDefault().Add((float)(policyLearningRate *policyTotalDecay * leftAdvantage / (leftLastOutputProbability * 0.9 + 0.1)));

            valueDerivatives[^2].LastOrDefault().LastOrDefault().Add(valueLearningRate * leftAdvantage);
            
            if(rightIsCurrentNN)
            {
                policyDerivatives.Add(neuralNetworkController.SetPartialDerivatives(policyNN, rightLastCalculation, rightLastOutputResult, true));
                policyDerivatives.LastOrDefault().LastOrDefault().LastOrDefault().Add((float)(policyLearningRate *policyTotalDecay * rightAdvantage / (rightLastOutputProbability * 0.9 + 0.1)));

                valueDerivatives.LastOrDefault().LastOrDefault().LastOrDefault().Add(valueLearningRate * rightAdvantage);
            }
        }

        leftLastStateValue = leftThisStateValue;
        rightLastStateValue = rightThisStateValue;

        policyTotalDecay *= decayRate;

        sampleCount++;
        if(sampleCount >= batchSize && !skipOneFrameTraining)
        {
            batchCount ++;
            sampleCount = 0;
            policyTotalDecay = 1;

            foreach(List<List<List<float>>> derivative in policyDerivatives)
            {
                policyNN = neuralNetworkController.GradientAscent(policyNN, derivative);
            }
            policyDerivatives.Clear();

            for(int i = 0; i <= valueDerivatives.Count() - 3; i++) 
            {
                valueNN = neuralNetworkController.GradientAscent(valueNN, valueDerivatives[i]);
            }
            valueDerivatives.RemoveRange(0, valueDerivatives.Count() - 2);

            if(batchCount >= batchSaveSize)
            {
                batchCount = 0;
                aiControl.AISaves[currentAISave].oldPolicyNNs.Add(aiControl.AISaves[currentAISave].CreateSerializedCopy(policyNN));
                aiControl.AISaves[currentAISave].policyNN = aiControl.AISaves[currentAISave].CreateSerializedCopy(policyNN);
                aiControl.AISaves[currentAISave].valueNN = aiControl.AISaves[currentAISave].CreateSerializedCopy(valueNN);

                if(UnityEngine.Random.value > 0.4)
                {
                    rightIsCurrentNN = true;
                    print("vs current NN");
                }
                else
                {
                    var rightNNIndex = (int)Math.Truncate(UnityEngine.Random.value * aiControl.AISaves[currentAISave].oldPolicyNNs.Count()-1);
                    policyAlternativeNN = aiControl.AISaves[currentAISave].oldPolicyNNs[rightNNIndex];
                    skipOneFrameTraining = true;
                    rightIsCurrentNN = false;
                    print(" vs NN: " + rightNNIndex);
                }
                aiControl.SaveFile();
            }
        }
        var leftCalcuation = neuralNetworkController.RunNNAndSave(valueNN, GetInput(true)).calculations;
        var rightCalculation = neuralNetworkController.RunNNAndSave(valueNN, GetInput(false)).calculations;

        valueDerivatives.Add(neuralNetworkController.SetPartialDerivatives(valueNN, leftCalcuation));
        valueDerivatives.Add(neuralNetworkController.SetPartialDerivatives(valueNN, rightCalculation));
    }

    private double Reward(bool isLeftFighter)
    {
        double reward = 0;
        double frameAdvantage = battleCore.GetFrameAdvantage(isLeftFighter);
        frameAdvantage *= Math.Abs(battleCore.fighter1.position.x - battleCore.fighter2.position.x) < 500 ? 1 : 0.1;
        //reward += frameAdvantage*0;
        reward += isLeftFighter ? battleCore.leftTotalReward : battleCore.rightTotalReward;

        battleCore.leftTotalReward = isLeftFighter ? 0 : battleCore.leftTotalReward;
        battleCore.rightTotalReward = !isLeftFighter ? 0 : battleCore.rightTotalReward;

        return reward;
    }

    public List<float> GetInput(bool isLeftFighter)
    {
        if(isLeftFighter)
        {
            return new List<float>(){

                Math.Abs(battleCore.fighter1.position.x - battleCore.fighter2.position.x)/100,
                leftTimeSinceNoAttack,
                battleCore.fighter1.position.x/100,
                battleCore.fighter1.currentActionID,
                battleCore.fighter1.currentActionFrame,
                battleCore.fighter1.currentActionFrameCount,
                battleCore.fighter1.currentHitStunFrame,
                battleCore.fighter1.guardHealth,

                rightTimeSinceNoAttack,
                battleCore.fighter2.position.x/100,
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

                Math.Abs(battleCore.fighter1.position.x - battleCore.fighter2.position.x)/100,
                rightTimeSinceNoAttack,
                battleCore.fighter2.position.x *-1/100,
                battleCore.fighter2.currentActionID,
                battleCore.fighter2.currentActionFrame,
                battleCore.fighter2.currentActionFrameCount,
                battleCore.fighter2.currentHitStunFrame,
                battleCore.fighter2.guardHealth,

                leftTimeSinceNoAttack,
                battleCore.fighter1.position.x *-1/100,
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
