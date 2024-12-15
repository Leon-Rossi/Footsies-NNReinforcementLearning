using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Footsies;
using UnityEditor;
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
    float lastStateValue;
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
    private int batchSaveSize = 30;
    private int batchSwitchCount = 0;
    private int batchSwitchSize = 2000;

    private bool skipOneFrameTraining = false;
    List<List<List<List<float>>>> policyAlternativeNN = new List<List<List<List<float>>>>();

    int verificationTimeout = 5;
    int verificationTimer = 0;
    bool isVerification = false;
    int verificationCount;
    int verificationMax = 4096;
    float verificationReward;

    float totalMatches;
    float totalWins;

    bool displayInfo;
    bool onlyVsBot = true;

    void Awake()
    {

        aiControl = GameObject.Find("GameMaster").GetComponent<AIControl>();
        neuralNetworkController = GameObject.Find("GameMaster").GetComponent<NeuralNetworkController>();
        battleCore = GameObject.Find("BattleCore").GetComponent<BattleCore>();
        selectNN = GameObject.Find("MenuManager").GetComponent<SelectNN>();
        
        Time.timeScale = aiControl.speed;

        currentAISave = aiControl.currentAISave;
        decayRate = aiControl.AISaves[currentAISave].decayRate;
        policyLearningRate = aiControl.AISaves[currentAISave].policyLearningRate;
        sigmoid = aiControl.AISaves[currentAISave].sigmoid;
        valueLearningRate = aiControl.AISaves[currentAISave].valueLearningRate;

        policyNN = aiControl.AISaves[currentAISave].policyNN;
        valueNN = aiControl.AISaves[currentAISave].valueNN;

        if(aiControl.AISaves[currentAISave].oldPolicyNNs.Count() == 0)
        {
            aiControl.AISaves[currentAISave].oldPolicyNNs.Add(aiControl.AISaves[currentAISave].CreateSerializedCopy(policyNN));
        }

        policyAlternativeNN = aiControl.AISaves[currentAISave].oldPolicyNNs.LastOrDefault();

        if(GameManager.Instance.humanVsNN)
        {
            humanVsNN = true;
            policyAlternativeNN = selectNN.generation ==  -1? aiControl.AISaves[currentAISave].oldPolicyNNs.LastOrDefault() : aiControl.AISaves[currentAISave].oldPolicyNNs[selectNN.generation];
            print(humanVsNN + " is vs human");
        }
        else
        {
            isVerification = true;
        }
        skipOneFrameTraining = true;
    }

    public int RunNN(bool isLeftFighter)
    {
        bool mustAttack = false;

        if(!isLeftFighter && isVerification)
        {
            verificationCount++;
            skipOneFrameTraining = true;
            if(verificationCount <= verificationMax)
            {
                return battleCore.battleAI.getNextAIInput();
            }

            isVerification = false;
            verificationCount = 0;
            print("Verification Reward: " + verificationReward);
            verificationReward = 0;
            return battleCore.battleAI.getNextAIInput();
        }

        if(!isLeftFighter && onlyVsBot)
        {
            return battleCore.battleAI.getNextAIInput();
        }

        if(isLeftFighter && leftSpecialCount > 0)
        {
            leftSpecialCount ++;
            if(leftSpecialCount < 75)
            {
                leftTimeSinceNoAttack ++;
                mustAttack = true;
            }
            else{
                leftSpecialCount = -1;
            }
        }

        if(!isLeftFighter && rightSpecialCount > 0)
        {
            rightSpecialCount ++;
            if(rightSpecialCount < 75)
            {
                rightTimeSinceNoAttack ++;
                mustAttack = true;
            }
            else{
                rightSpecialCount = -1;
            }
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
        int trueChosenAction = chosenAction;
        if(mustAttack && chosenAction < 4)
        {
            chosenAction += 4;
        }        
        
        if(displayInfo)
        {
            print("Action: " + chosenAction);
        }

        if(isLeftFighter)
        {
            if(displayInfo)
            {
                print("Output1: " + output[0] + " " + output[1] + " " + output[2] + " " + output[3] + " " + output[4] + " " + output[5] + " " + output[6] + " " + output[7] + " " + output[8]);
                print("Output2: " + outputArray[0] + " " + outputArray[1] + " " + outputArray[2] + " " + outputArray[3] + " " + outputArray[4] + " " + outputArray[5] + " " + outputArray[6] + " " +  outputArray[7] + " " +  outputArray[8] + " " + chosenAction);
            }
        }

        if(isLeftFighter)
        {
            leftLastPolicyCalculation = lastCalculation;
            leftLastOutputResult = trueChosenAction;
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
            rightLastOutputResult = trueChosenAction;
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
        float stateValue = leftState.output[0];
        var reward = Reward();
        if(battleCore.isTerminalState)
        {
            stateValue = 0;
            if(displayInfo)
            {
                print("Reward: " + reward + " " + lastStateValue);
            }

            //Variables that need semi random updates
            displayInfo = aiControl.displayInfo;
        }

        float advantage = (float)(decayRate * stateValue - lastStateValue + reward);

        if(displayInfo)
        {
            print("Advantage: " + advantage + " " + stateValue + " " + lastStateValue + "  " + reward + " " + leftLastOutputResult);
        }
        
        if(!skipOneFrameTraining)
        {
            policyDerivatives.Add(neuralNetworkController.SetPartialDerivatives(policyNN, leftLastPolicyCalculation, sigmoid, leftLastOutputResult, true));
            policyDerivatives.LastOrDefault().LastOrDefault().LastOrDefault().Add((float)(policyLearningRate *policyTotalDecay * advantage / Math.Clamp(leftLastOutputProbability+0.1, 0, 1)));

            valueDerivatives.Add(neuralNetworkController.SetPartialDerivatives(valueNN, leftLastValueCalculation, sigmoid, 0, false));
            valueDerivatives.LastOrDefault().LastOrDefault().LastOrDefault().Add(valueLearningRate * advantage);
        }

        leftLastValueCalculation = leftState.calculations;
        lastStateValue = stateValue;

        sampleCount++;
        if(sampleCount >= batchSize && !skipOneFrameTraining)
        {
            batchCount ++;
            sampleCount = 0;

            if(displayInfo)
            {
                print("Batch: " + policyDerivatives.Count);
            }

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

            if(batchCount >= batchSaveSize && !onlyVsBot)
            {
                batchCount = 0;

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
                

                verificationTimer ++;
                if(verificationTimer >= verificationTimeout && !onlyVsBot)
                {
                    isVerification = true;
                    verificationTimer = 0;
                    print("Start Verification");
                }
            }

            batchSwitchCount ++;
            if(batchSwitchCount >= batchSwitchSize)
            { 
                batchSwitchCount = 0;
                aiControl.AISaves[currentAISave].oldPolicyNNs.Add(aiControl.AISaves[currentAISave].CreateSerializedCopy(policyNN));
                aiControl.AISaves[currentAISave].policyNN = aiControl.AISaves[currentAISave].CreateSerializedCopy(policyNN);
                aiControl.AISaves[currentAISave].valueNN = aiControl.AISaves[currentAISave].CreateSerializedCopy(valueNN);
                aiControl.SaveFile();
            }
        }

        if(totalMatches > 1000)
        {
            print("Validation: " + totalWins/totalMatches);
            aiControl.AISaves[currentAISave].measurments.Add(totalWins/totalMatches);
            totalMatches = 0;
            totalWins = 0;
        }       

        if(battleCore.isTerminalState)
        {
            leftSpecialCount = -1;
            rightSpecialCount = -1;
            battleCore.isTerminalState = false;
            skipOneFrameTraining = true;
        }
    }         

    private double Reward()
    {
        double reward = 0;
        reward += battleCore.leftTotalReward;

        battleCore.leftTotalReward = 0;

        if(battleCore.isTerminalState)
        {
            totalMatches ++;
            if(reward > 6)
            {
                totalWins ++;
            }
            
        }

        return reward;
    }

    public List<float> GetInput(bool isLeftFighter)
    {
        List<float> leftInfo = new List<float>(){
        (new[] {105, 100}.Contains(battleCore.fighter1.currentActionID)? 1 : 0) + (new[] {115, 110, 105, 100}.Contains(battleCore.fighter1.currentActionID)? 1 : 0),
        (new[] {115, 110, 1, 10}.Contains(battleCore.fighter1.currentActionID)? 1: 0) +  (new[] {2, 11, 301, 305, 306, 350}.Contains(battleCore.fighter1.currentActionID)? -1: 0),

        Math.Clamp(leftTimeSinceNoAttack /100, 0, 1),
        (float)battleCore.fighter1.guardHealth/3f,
        !battleCore.fighter1.isInHitStun ? 1 : 0,
        battleCore.fighter1.isAlwaysCancelable? 1 : (float)battleCore.fighter1.currentActionFrame / (float)battleCore.fighter1.currentActionFrameCount,
        };

        List<float> rightInfo = new List<float>(){
        (new[] {105, 100}.Contains(battleCore.fighter2.currentActionID)? 1 : 0) + (new[] {115, 110, 105, 100}.Contains(battleCore.fighter2.currentActionID)? 1 : 0),
        (new[] {115, 110, 1, 10}.Contains(battleCore.fighter2.currentActionID)? 1: 0) +  (new[] {2, 11, 301, 305, 306, 350}.Contains(battleCore.fighter2.currentActionID)? -1: 0),

        Math.Clamp(rightTimeSinceNoAttack /100, 0, 1),
        (float)battleCore.fighter2.guardHealth/3f,
        !battleCore.fighter2.isInHitStun ? 1 : 0,
        battleCore.fighter2.isAlwaysCancelable? 1 : (float)battleCore.fighter2.currentActionFrame / (float)battleCore.fighter2.currentActionFrameCount,
        };

        List<float> additionalInfo = new List<float>(){
            Math.Abs(battleCore.fighter1.position.x - battleCore.fighter2.position.x)/10,
            (float)battleCore.GetFrameAdvantage(isLeftFighter)/20,
            (isLeftFighter? leftSpecialCount >= 0:rightSpecialCount >= 0)? 1 : 0,
        };

        if(isLeftFighter)
        {
            leftInfo.AddRange(rightInfo);
            leftInfo.AddRange(additionalInfo);

            string stringOut = "Inputs: ";
            leftInfo.ForEach(x => stringOut += x + " ");
            if(displayInfo)
            {
                print(stringOut);
            }

            return leftInfo;
        }
        rightInfo.AddRange(leftInfo);
        rightInfo.AddRange(additionalInfo);
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
