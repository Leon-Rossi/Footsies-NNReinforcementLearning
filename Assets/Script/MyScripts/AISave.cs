using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using System.IO;

[Serializable]
public class AISave
{
    [JsonIgnore] GeneticAlgorithm learningAlgorithm;
    [JsonIgnore] AIControl aIControl;
    [JsonIgnore] NeuralNetworkController neuralNetworkController;

    public string saveName;

    public float decayRate;
    public float policyLearningRate;
    public int policyLayerCount;
    public int policyLayerSize;
    public int policyInputCount;
    public int policyOutputCount;
    
    public float valueLearningRate;
    public int valueLayerCount;
    public int valueLayerSize;
    public int valueInputCount;
    public int valueOutputCount;

    public List<List<List<List<float>>>> policyNN = new List<List<List<List<float>>>>();
    public List<List<List<List<float>>>> valueNN = new List<List<List<List<float>>>>();

    public List<List<List<List<List<float>>>>> oldPolicyNNs = new List<List<List<List<List<float>>>>>();

    public AISave(string nameString, float decayRateInput, float policyLearningRateInput, int policyLayerCountInput, int policyLayerSizeInput, int policyInputCountInput, int policyOutputCountInput, float valueLearningRateInput, int valueLayerCountInput, int valueLayerSizeInput, int valueInputCountInput, int valueOutputCountInput)
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

        decayRate = decayRateInput;
        saveName = nameString;

        learningAlgorithm = GameObject.Find("GameMaster").GetComponent<GeneticAlgorithm>();
        aIControl = GameObject.Find("GameMaster").GetComponent<AIControl>();
        neuralNetworkController = GameObject.Find("GameMaster").GetComponent<NeuralNetworkController>();

        policyNN = neuralNetworkController.CreateNN(policyLayerSize, policyLayerCount, policyInputCount, policyOutputCount);
        valueNN = neuralNetworkController.CreateNN(valueLayerSize, valueLayerCount, valueInputCount, valueOutputCount);
    }

    private void Awake()
    {
        learningAlgorithm = GameObject.Find("GameMaster").GetComponent<GeneticAlgorithm>();
        aIControl = GameObject.Find("GameMaster").GetComponent<AIControl>();
        neuralNetworkController = GameObject.Find("GameMaster").GetComponent<NeuralNetworkController>();
    }

    public void AddCurrentPolicyToOldPolicyNNs()
    {
        oldPolicyNNs.Add(CreateSerializedCopy(policyNN));
    }

    // Deep Cloning Lists based on https://stackoverflow.com/questions/27208411/how-to-clone-multidimensional-array-without-reference-correctly
    /// <summary>
    /// This method clones all of the items and serializable properties of the current collection by 
    /// serializing the current object to memory, then deserializing it as a new object. This will 
    /// ensure that all references are cleaned up.
    /// </summary>
    /// <returns></returns>
    /// <remarks></remarks>
    public T CreateSerializedCopy<T>(T oRecordToCopy)
    {
        // Exceptions are handled by the caller

        if (oRecordToCopy == null)
        {
            return default(T);
        }

        if (!oRecordToCopy.GetType().IsSerializable)
        {
            throw new ArgumentException(oRecordToCopy.GetType().ToString() + " is not serializable");
        }

        var oFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

        using (var oStream = new MemoryStream())
        {
            oFormatter.Serialize(oStream, oRecordToCopy);
            oStream.Position = 0;
            return (T)oFormatter.Deserialize(oStream);
        }
    }
}
