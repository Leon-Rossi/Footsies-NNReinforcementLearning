using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Drawing.Printing;
using System.Linq;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using Footsies;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
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

    List<List<List<float>>> leftLastPolicyCalculation = new List<List<List<float>>>();
    List<List<List<float>>> leftLastValueCalculation = new List<List<List<float>>>();

    int leftLastOutputResult;
    double leftLastOutputProbability;
    float leftLastStateValue;
    int leftSpecialCount;

    List<List<List<float>>> rightLastPolicyCalculation = new List<List<List<float>>>();
    List<List<List<float>>> rightLastValueCalculation = new List<List<List<float>>>();

    int rightLastOutputResult;
    double rightLastOutputProbability;
    float rightLastStateValue;
    int rightSpecialCount;

    float leftTimeSinceNoAttack;
    float rightTimeSinceNoAttack;

    bool humanVsNN;
    private List<float> output;
    private int sampleCount = 0;
    private int batchSize = 1024;
    private int batchCount = 0;
    private int batchSaveSize = 5;
    private bool rightIsCurrentNN = true;
    private bool skipOneFrameTraining = false;
    List<List<List<List<float>>>> policyAlternativeNN = new List<List<List<List<float>>>>();

    int rewardCount;
    float rewardTotal;

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

        if(aiControl.AISaves[currentAISave].oldPolicyNNs.Count() == 0)
        {
            aiControl.AISaves[currentAISave].oldPolicyNNs.Add(aiControl.AISaves[currentAISave].CreateSerializedCopy(policyNN));
        }

        rightIsCurrentNN = false;
        policyAlternativeNN = aiControl.AISaves[currentAISave].oldPolicyNNs.LastOrDefault();

    }

    public int RunNN(bool isLeftFighter)
    {
        if(isLeftFighter && leftSpecialCount > 0)
        {
            leftSpecialCount ++;
            if(leftSpecialCount < 66)
            {
                leftTimeSinceNoAttack ++;
                return 4;
            }
            leftSpecialCount = -1;
        }

        if(!isLeftFighter && rightSpecialCount > 0)
        {
            rightSpecialCount ++;
            if(rightSpecialCount < 66)
            {
                rightTimeSinceNoAttack ++;
                return 4;
            }
            rightSpecialCount = -1;
        }

        if(!ThisInputCounts(isLeftFighter))
        {
            leftTimeSinceNoAttack = leftLastOutputResult >= 4? leftTimeSinceNoAttack ++ : 0;
            rightTimeSinceNoAttack = rightLastOutputResult >= 4? rightTimeSinceNoAttack ++ : 0;

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
        }
        //print(isLeftFighter + " " + outputArray[0] + " " + outputArray[1] + " " + outputArray[2] + " " + outputArray[3] + " " + outputArray[4] + " " + outputArray[5] + " " + outputArray[6] + " " + outputArray[7] + " " + outputArray[8]+ " " + chosenAction);
        if(!humanVsNN)
        {
            if(isLeftFighter)
            {
                leftLastPolicyCalculation = lastCalculation;
                leftLastOutputResult = chosenAction;
                leftLastOutputProbability = outputArray[chosenAction];
                leftTimeSinceNoAttack = chosenAction >= 4? leftTimeSinceNoAttack ++ : 0;
                if(chosenAction == 7)
                {
                    leftSpecialCount = 1;
                    chosenAction = 4;
                }

                if(chosenAction == 8)
                {
                    leftSpecialCount = 1;
                    chosenAction = 5;
                }
            }
            else
            {
                rightLastPolicyCalculation = lastCalculation;
                rightLastOutputResult = chosenAction;
                rightLastOutputProbability = outputArray[chosenAction];
                rightTimeSinceNoAttack = chosenAction >= 4? rightTimeSinceNoAttack ++ : 0;

                if(chosenAction == 7)
                {
                    rightSpecialCount = 1;
                    chosenAction = 4;
                }
                if(chosenAction == 8)
                {
                    rightSpecialCount = 1;
                    chosenAction = 5;
                }
                
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
        //print(chosenAction);
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
        var leftState = neuralNetworkController.RunNNAndSave(valueNN, GetInput(true));
        leftLastValueCalculation = leftState.calculations;
        float leftThisStateValue = leftState.output[0];
        var reward = Reward(true);
        float leftAdvantage = (float)(decayRate * leftThisStateValue - leftLastStateValue + reward);

        //print(leftAdvantage + " true advantage " + leftThisStateValue + " " + leftLastStateValue + "  " + reward);
        var rightState = neuralNetworkController.RunNNAndSave(valueNN, GetInput(false));
        rightLastValueCalculation = rightState.calculations;
        float rightThisStateValue = rightState.output[0];
        float rightAdvantage = (float)(decayRate * rightThisStateValue - rightLastStateValue + Reward(false));

        //print(rightAdvantage + " false advantage ");

        if(!skipOneFrameTraining)
        {
            policyDerivatives.Add(neuralNetworkController.SetPartialDerivatives(policyNN, leftLastPolicyCalculation, leftLastOutputResult, true));
            policyDerivatives.LastOrDefault().LastOrDefault().LastOrDefault().Add((float)(policyLearningRate *policyTotalDecay * leftAdvantage / (leftLastOutputProbability * 0.9 + 0.1)));

            valueDerivatives.Add(neuralNetworkController.SetPartialDerivatives(valueNN, leftLastValueCalculation));
            valueDerivatives.LastOrDefault().LastOrDefault().LastOrDefault().Add(valueLearningRate * leftAdvantage);
            
            if(rightIsCurrentNN)
            {
                policyDerivatives.Add(neuralNetworkController.SetPartialDerivatives(policyNN, rightLastPolicyCalculation, rightLastOutputResult, true));
                policyDerivatives.LastOrDefault().LastOrDefault().LastOrDefault().Add((float)(policyLearningRate *policyTotalDecay * rightAdvantage / (rightLastOutputProbability * 0.9 + 0.1)));

                valueDerivatives.Add(neuralNetworkController.SetPartialDerivatives(valueNN, rightLastValueCalculation));
                valueDerivatives.LastOrDefault().LastOrDefault().LastOrDefault().Add(valueLearningRate * rightAdvantage);
            }
        }

        leftLastStateValue = leftThisStateValue;
        rightLastStateValue = rightThisStateValue;

        //policyTotalDecay *= decayRate;

        sampleCount++;
        if(sampleCount >= batchSize && !skipOneFrameTraining)
        {
            batchCount ++;
            sampleCount = 0;
            policyTotalDecay = 1;

            print("Average reward this batch: " + rewardTotal/rewardCount + " " + rewardTotal);
            rewardCount = 0;
            rewardTotal = 0;

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


                if(UnityEngine.Random.value > 0.4)
                {
                    rightIsCurrentNN = false;
                    policyAlternativeNN = aiControl.AISaves[currentAISave].oldPolicyNNs.LastOrDefault();
                    skipOneFrameTraining = true;
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
    }

    private double Reward(bool isLeftFighter)
    {
        double reward = 0;
        double frameAdvantage = battleCore.GetFrameAdvantage(isLeftFighter);
        frameAdvantage *= Math.Abs(battleCore.fighter1.position.x - battleCore.fighter2.position.x) < 500 ? 1 : 0.1;
        reward += frameAdvantage*0;
        reward += isLeftFighter ? battleCore.leftTotalReward : battleCore.rightTotalReward;

        battleCore.leftTotalReward = isLeftFighter ? 0 : battleCore.leftTotalReward;
        battleCore.rightTotalReward = !isLeftFighter ? 0 : battleCore.rightTotalReward;

        if(isLeftFighter)
        {
            rewardCount ++;
            rewardTotal += (float)reward;
        }
        return reward;
    }

    public List<float> GetInput(bool isLeftFighter)
    {
        return AltGetInput(isLeftFighter);
        if(isLeftFighter)
        {
            return new List<float>(){

                Math.Abs(battleCore.fighter1.position.x - battleCore.fighter2.position.x),
                Math.Clamp(leftTimeSinceNoAttack, 0 , 100),
                battleCore.fighter1.position.x/100,
                battleCore.fighter1.currentActionID,
                battleCore.fighter1.currentActionFrame,
                battleCore.fighter1.currentActionFrameCount,
                battleCore.fighter1.currentHitStunFrame,
                battleCore.fighter1.guardHealth,

                Math.Clamp(rightTimeSinceNoAttack, 0, 100),
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

                Math.Abs(battleCore.fighter1.position.x - battleCore.fighter2.position.x),
                Math.Clamp(rightTimeSinceNoAttack, 0, 100),
                battleCore.fighter2.position.x *-1,
                battleCore.fighter2.currentActionID,
                battleCore.fighter2.currentActionFrame,
                battleCore.fighter2.currentActionFrameCount,
                battleCore.fighter2.currentHitStunFrame,
                battleCore.fighter2.guardHealth,

                Math.Clamp(leftTimeSinceNoAttack, 0, 100),
                battleCore.fighter1.position.x *-1,
                battleCore.fighter1.currentActionID,
                battleCore.fighter1.currentActionFrame,
                battleCore.fighter1.currentActionFrameCount,
                battleCore.fighter1.currentHitStunFrame,
                battleCore.fighter1.guardHealth,
            };
        }
    }

    public List<float> AltGetInput(bool isLeftFighter)
    {
        List<float> leftInfo = new List<float>(){
        leftLastOutputResult == 1 || leftLastOutputResult == 5? 1 : 0,
        leftLastOutputResult == 2 || leftLastOutputResult == 6? 1 : 0,
        leftLastOutputResult >= 4 ? 1 : 0,
        leftLastOutputResult == 7 || leftLastOutputResult == 8? 1 : 0,

        leftTimeSinceNoAttack >= 64? 1 : 0,

        battleCore.fighter1.guardHealth == 0 ? 1 : 0,
        battleCore.fighter1.currentHitStunFrame != 0 ? 1 : 0,
        (float)battleCore.fighter1.currentActionFrame / (float)battleCore.fighter1.currentActionFrameCount,
        };

        List<float> rightInfo = new List<float>(){
        rightLastOutputResult == 1 || rightLastOutputResult == 5? 1 : 0,
        rightLastOutputResult == 2 || rightLastOutputResult == 6? 1 : 0,
        rightLastOutputResult >= 4 ? 1 : 0,
        rightLastOutputResult == 7 || rightLastOutputResult == 8? 1 : 0,

        rightTimeSinceNoAttack >= 64? 1 : 0,

        battleCore.fighter2.guardHealth == 0 ? 1 : 0,
        battleCore.fighter2.currentHitStunFrame != 0 ? 1 : 0,
        (float)battleCore.fighter2.currentActionFrame / (float)battleCore.fighter2.currentActionFrameCount,
        };

        var distance = Math.Abs(battleCore.fighter1.position.x - battleCore.fighter2.position.x);

        List<float> distanceInfo = new List<float>(){
            distance > 1 ? 1 : 0,
            distance > 1.5 ? 1 : 0,
            distance > 2 ? 1 : 0,
            distance > 2.5 ? 1 : 0,
            distance > 3 ? 1 : 0,
            distance > 3.5 ? 1 : 0,
            distance > 4 ? 1 : 0, 
            distance > 4.5 ? 1 : 0,
        };

        

        if(isLeftFighter)
        {
            leftInfo.AddRange(rightInfo);
            leftInfo.AddRange(distanceInfo);

            string stringOut = "";
            leftInfo.ForEach(x => stringOut += x + " ");
            //print(leftLastOutputResult + " " + distance + " " + leftTimeSinceNoAttack);
            //print(stringOut);

            return leftInfo;
        }
        rightInfo.AddRange(leftInfo);
        rightInfo.AddRange(distanceInfo);
        return rightInfo;
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
