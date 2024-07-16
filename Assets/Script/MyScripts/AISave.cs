using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Numerics;
using System;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

[Serializable]
public class AISave
{
    [JsonIgnore] GeneticAlgorithm learningAlgorithm;
    [JsonIgnore] AIControl aIControl;
    [JsonIgnore] NeuralNetworkController neuralNetworkController;

    public string saveName;

    public float policyLearningRate;
    public int policyLayerCount;
    public int policyLayerSize;
    public int policyInputCount;
    public int policyoutputCount;
    
    public float valueLearningRate;
    public int valueLayerCount;
    public int valueLayerSize;
    public int valueInputCount;
    public int valueOutputCount;

    public List<List<List<List<float>>>> policyNN = new List<List<List<List<float>>>>();
    public List<List<List<List<float>>>> valueNN = new List<List<List<List<float>>>>();

    public List<List<List<List<List<float>>>>> oldPolicyNNs = new List<List<List<List<List<float>>>>>();

    public AISave(string nameString, float policyLearningRateInput, int policyLayerCountInput, int policyLayerSizeInput, int policyInputCountInput, int policyoutputCountInput, float valueLearningRateInput, int valueLayerCountInput, int valueLayerSizeInput, int valueInputCountInput, int valueOutputCountInput)
    {
        policyLearningRate = policyLearningRateInput;
        policyLayerSize = policyLayerSizeInput;
        policyLayerCount = policyLayerCountInput;
        policyInputCount = policyInputCountInput;
        policyOutputCount = policyOutputCountInput;
        
        valueLearningRate = valueLearningRateInput;
        valueLayerSize = valueLayerSizeInput;
        valueLayerCount = valueLayerCountInput;
        valueInputCount = valueInputCountInput;
        valueOutputCount = valueOutputCountInput;

        SaveName = nameString;

        learningAlgorithm = GameObject.Find("GameMaster").GetComponent<GeneticAlgorithm>();
        aIControl = GameObject.Find("GameMaster").GetComponent<AIControl>();
        neuralNetworkController = GameObject.Find("GameMaster").GetComponent<NeuralNetworkController>();

        policyNN = neuralNetworkController.CreateNN(policyLayerSize, policyLayerCount, policyInputCount, policyOutputCount, valueLayerSize, valueLayerCount, valueInputCount, valueOutputCount);
        valueNN = neuralNetworkController.CreateNN(valueLayerSize, valueLayerCount, valueInputCount, valueOutputCount, policyLayerSize, policyLayerCount, policyInputCount, policyOutputCount);
    }

    private void Awake()
    {
        learningAlgorithm = GameObject.Find("GameMaster").GetComponent<GeneticAlgorithm>();
        aIControl = GameObject.Find("GameMaster").GetComponent<AIControl>();
        neuralNetworkController = GameObject.Find("GameMaster").GetComponent<NeuralNetworkController>();
    }

}
