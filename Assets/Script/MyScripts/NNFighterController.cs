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
    bool sigmoid;

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
    int rightSpecialCount;

    float leftTimeSinceNoAttack;
    float rightTimeSinceNoAttack;

    bool humanVsNN;
    private List<float> output;
    private int sampleCount = 0;
    private int batchSize = 1024;
    private int batchCount = 0;
    private int batchSaveSize = 20;
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
        sigmoid = aiControl.AISaves[currentAISave].sigmoid;

        valueLearningRate = aiControl.AISaves[currentAISave].valueLearningRate;

        if(GameManager.Instance.humanVsNN)
        {
            humanVsNN = true;
            print(humanVsNN + " is vs human");
        }

        Time.timeScale = aiControl.speed;

        skipOneFrameTraining = true;

        policyNN = aiControl.AISaves[currentAISave].policyNN;
        valueNN = aiControl.AISaves[currentAISave].valueNN;

        if(aiControl.AISaves[currentAISave].oldPolicyNNs.Count() == 0)
        {
            aiControl.AISaves[currentAISave].oldPolicyNNs.Add(aiControl.AISaves[currentAISave].CreateSerializedCopy(policyNN));
        }

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

        if(!isLeftFighter || humanVsNN)
        {
            output = neuralNetworkController.RunNN(policyAlternativeNN, GetInput(isLeftFighter), sigmoid);
        }
        else
        {
            var outputVar = neuralNetworkController.RunNNAndSave(policyNN, GetInput(isLeftFighter), sigmoid);
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
            //Debug.Log(string.Format("[{0}]", string.Join(", ", GetInput(true))));
            //print(output[0] + " " + output[1] + " " + output[2] + " " + output[3] + " " + output[4] + " " + output[5] + " " + output[6] + " " + output[7] + " " + output[8]+ " " + chosenAction);
            //print(isLeftFighter + " " + outputArray[0] + " " + outputArray[1] + " " + outputArray[2] + " " + outputArray[3] + " " + outputArray[4] + " " + outputArray[5] + " " + outputArray[6] + " " + chosenAction);
        }
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
                rightLastOutputResult = chosenAction;
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
        if(isLeftFighter)
        {
            return battleCore.fighter1.isActionEnd || battleCore.fighter1.canCancelAttack() || battleCore.fighter1.fighterData.actions[battleCore.fighter1.currentActionID].alwaysCancelable;
        }

        return battleCore.fighter2.isActionEnd || battleCore.fighter2.canCancelAttack() || battleCore.fighter2.fighterData.actions[battleCore.fighter1.currentActionID].alwaysCancelable;
    }

    public void TrainNNS()
    {
        var leftState = neuralNetworkController.RunNNAndSave(valueNN, GetInput(true), sigmoid);
        float leftThisStateValue = leftState.output[0];
        var reward = Reward();
        float leftAdvantage = (float)(decayRate * leftThisStateValue - leftLastStateValue + reward);
        float policyAdvantage = leftAdvantage;
        float valueAdvantage = leftAdvantage + (leftLastStateValue < 1? 1:0);

        print(leftAdvantage + " " + policyAdvantage  + " " + valueAdvantage + " true advantage " + leftThisStateValue + " " + leftLastStateValue + "  " + reward + " " + leftLastOutputResult);

        if(!skipOneFrameTraining)
        {
            policyDerivatives.Add(neuralNetworkController.SetPartialDerivatives(policyNN, leftLastPolicyCalculation, sigmoid, leftLastOutputResult, true));
            policyDerivatives.LastOrDefault().LastOrDefault().LastOrDefault().Add((float)(policyLearningRate *policyTotalDecay * policyAdvantage / Math.Clamp(leftLastOutputProbability, 0.01, 1)));
            print(policyLearningRate *policyTotalDecay * policyAdvantage / Math.Clamp(leftLastOutputProbability, 0.01, 1));

            valueDerivatives.Add(neuralNetworkController.SetPartialDerivatives(valueNN, leftLastValueCalculation, sigmoid, 0, false));
            valueDerivatives.LastOrDefault().LastOrDefault().LastOrDefault().Add(valueLearningRate * valueAdvantage);
        }

        leftLastValueCalculation = leftState.calculations;
        leftLastStateValue = leftThisStateValue;

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
                    policyAlternativeNN = aiControl.AISaves[currentAISave].oldPolicyNNs.LastOrDefault();
                    skipOneFrameTraining = true;
                    print("change batch vs current NN");
                }
                else
                {
                    var rightNNIndex = (int)Math.Truncate(UnityEngine.Random.value * aiControl.AISaves[currentAISave].oldPolicyNNs.Count()-1);
                    policyAlternativeNN = aiControl.AISaves[currentAISave].oldPolicyNNs[rightNNIndex];
                    skipOneFrameTraining = true;
                    print("change batch vs NN: " + rightNNIndex);
                }
                aiControl.SaveFile();
            }
        }
    }

    private double Reward()
    {
        double reward = 0;
        double frameAdvantage = battleCore.GetFrameAdvantage(true);
        frameAdvantage *= Math.Abs(battleCore.fighter1.position.x - battleCore.fighter2.position.x) < 500 ? 1 : 0.1;
        reward += frameAdvantage*0;
        reward += battleCore.leftTotalReward;

        battleCore.leftTotalReward = 0;

        rewardCount ++;
        rewardTotal += (float)reward;
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

        Math.Clamp(leftTimeSinceNoAttack /100, 0, 1),

        battleCore.fighter1.guardHealth == 0 ? 1 : 0,
        battleCore.fighter1.currentHitStunFrame != 0 ? 1 : 0,
        battleCore.fighter1.isAlwaysCancelable? 1 : (float)battleCore.fighter1.currentActionFrame / (float)battleCore.fighter1.currentActionFrameCount,
        };

        List<float> rightInfo = new List<float>(){
        rightLastOutputResult == 1 || rightLastOutputResult == 5? 1 : 0,
        rightLastOutputResult == 2 || rightLastOutputResult == 6? 1 : 0,
        rightLastOutputResult >= 4 ? 1 : 0,
        rightLastOutputResult == 7 || rightLastOutputResult == 8? 1 : 0,

        Math.Clamp(rightTimeSinceNoAttack /100, 0, 1),

        battleCore.fighter2.guardHealth == 0 ? 1 : 0,
        battleCore.fighter2.currentHitStunFrame != 0 ? 1 : 0,
        battleCore.fighter2.isAlwaysCancelable? 1 : (float)battleCore.fighter2.currentActionFrame / (float)battleCore.fighter2.currentActionFrameCount,
        };

        var distance = Math.Abs(battleCore.fighter1.position.x - battleCore.fighter2.position.x);

        List<float> distanceInfo = new List<float>(){
            distance,
/*             distance > 1 ? 1 : 0,
            distance > 1.5 ? 1 : 0,
            distance > 2 ? 1 : 0,
            distance > 2.5 ? 1 : 0,
            distance > 3 ? 1 : 0,
            distance > 3.5 ? 1 : 0,
            distance > 4 ? 1 : 0, 
            distance > 4.5 ? 1 : 0, */
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
