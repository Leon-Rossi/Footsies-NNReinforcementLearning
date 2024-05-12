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

    public string saveName;
    public float mutationFactor;
    public float mutationThreshhold;
    public int populationCount;

    public int layerCount;
    public int layerSize;
    public int inputCount;
    public int outputCount;

    public int[] currentNN = new int[2];

    public List<List<List<List<List<List<float>>>>>> allNeuralNetworks = new List<List<List<List<List<List<float>>>>>>();
    public List<List<List<List<List<float>>>>> bestNeuralNetworks = new List<List<List<List<List<float>>>>>();

    public AISave(float mutationFactorFloat, float mutationThreshholdFloat, int populationCountInt, int layerCountInt, int layerSizeInt, int inputCountInt, int outputCountInt, string NameString)
    {
        mutationFactor = mutationFactorFloat;
        mutationThreshhold = mutationThreshholdFloat;
        populationCount = populationCountInt;
        layerSize = layerSizeInt;
        layerCount = layerCountInt;
        inputCount = inputCountInt;
        outputCount = outputCountInt;
        saveName = NameString;

        learningAlgorithm = GameObject.Find("GameMaster").GetComponent<GeneticAlgorithm>();
        aIControl = GameObject.Find("GameMaster").GetComponent<AIControl>();
        SetUpFirstGeneration();

    }

    public void SetUpFirstGeneration()
    {
        allNeuralNetworks.Add(new List<List<List<List<List<float>>>>>());

        foreach(int i in Enumerable.Range(1, populationCount))
        {
            allNeuralNetworks[0].Add(GameObject.Find("GameMaster").GetComponent<NeuralNetworkController>().CreateNN(layerCount, layerSize, inputCount, outputCount));
        }
    }

    public void SetUpNextGeneration()
    {
        allNeuralNetworks[currentNN[0]] = learningAlgorithm.SortListByFitness(allNeuralNetworks[currentNN[0]]);
        bestNeuralNetworks.Add(learningAlgorithm.CreateSerializedCopy<List<List<List<List<float>>>>>(allNeuralNetworks.Last().Last()));

        allNeuralNetworks.Add(learningAlgorithm.CreateNewPopulation(populationCount, mutationFactor, mutationThreshhold, allNeuralNetworks.Last()));

        allNeuralNetworks[allNeuralNetworks.Count()-2].Clear();
        currentNN[0] = allNeuralNetworks.Count()-1;
        currentNN[1] = 0;

        Debug.Log("Current Gen nr.: " + currentNN[0]);
    }

    public int[] GiveNN()
    {
        return currentNN;
    }

    public int[] GiveNextNN()
    {
        if(currentNN[1] >= populationCount-1)
        {
            Debug.Log("Next Gen");
            SetUpNextGeneration();
            aIControl.SaveFile();
        }
        else
        {
            currentNN[1] += 1; 
        }

        Debug.Log("Current Population nr.: " + currentNN[1]);

        return currentNN;
    }

    public List<List<List<List<float>>>> GiveNNFromIndex(int[] index)
    {
        return allNeuralNetworks[index[0]][index[1]];
    }

    public void SetFitnessScore(int[] index, float fitnessScore)
    {
        allNeuralNetworks[index[0]][index[1]][0][0][0][1] = fitnessScore;
    }
}