using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class NeuralNetworkController : MonoBehaviour
{
    public enum ActivationFunctions
    {
        binary,
        nonLinear,
        Sigmoid
    }

    //Creates a neural Network in form of a 4D List (Layers, neurons, List, List with a single bias and later the fitness function ([0][0][0][1]) + List with weights)
    public List<List<List<List<float>>>> CreateNN(int layerCount, int layerSize, int inputCount,int outputCount)
    {
        List<List<List<List<float>>>> nN = new List<List<List<List<float>>>>();

        foreach(int i in Enumerable.Range(1, layerCount))
            {
                nN.Add(new List<List<List<float>>>());
            }

        foreach(List<List<List<float>>> layer in nN)
        {
            foreach(int i in Enumerable.Range(1, layerSize))
            {
                layer.Add(new List<List<float>>());
            }

            foreach(List<List<float>> node in layer)
            {
                node.Add(new List<float>());
                node.Add(new List<float>());

                node[0].Add(RandomValue());
                
                if(layer != nN[0])
                {
                    foreach(int i in Enumerable.Range(1, layerSize))
                    {
                        node[1].Add(RandomValue());
                    }
                }
                else
                {
                    foreach(int i in Enumerable.Range(1, inputCount))
                    {
                        node[1].Add(RandomValue());
                    }
                }

            }
        }

        
        nN.Add(new List<List<List<float>>>());

        foreach(int i in Enumerable.Range(1, outputCount))
        {
            nN.Last().Add(new List<List<float>>());
        }

        foreach(List<List<float>> node in nN.Last())
        {
            node.Add(new List<float>());
            node.Add(new List<float>());

            node[0].Add(RandomValue());
            
            foreach(int i in Enumerable.Range(1, layerSize))
            {
                node[1].Add(RandomValue());
            }
        }
                
        nN[0][0][0].Add(0);
        return nN;
    }

    public List<float> RunNN(List<List<List<List<float>>>> nN, List<float> input, ActivationFunctions selectedActivationFunction)
    {
        List<float> currentInput = new List<float>();
        List<float> nextInput = new List<float>(input);

        int Count = 0;   

        foreach(List<List<List<float>>> layer in nN)
        {
            Count +=1;

            currentInput = new List<float>(nextInput);

            nextInput.Clear();

            foreach(List<List<float>> node in layer)
            {
                float output = node[1].Zip(currentInput, (x, y) => x * y).Sum() + node[0][0];
                //nextInput.Add(ActivationFunction(output, selectedActivationFunction));
                
                nextInput.Add((float)(Math.Exp(output)/(1+Math.Exp(output))));
            }
        }
        return nextInput;
    }

    public float ActivationFunction(float input, ActivationFunctions selectedActivationFunction)
    {
        switch(selectedActivationFunction)
        {
            case ActivationFunctions.binary:
            {
                if(input > 1)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            case ActivationFunctions.nonLinear:
            {
                break;
            }
            case ActivationFunctions.Sigmoid:
            {
                return (float)(Math.Exp(input)/(1+Math.Exp(input)));
            }
        }
        return 0;
    }

    float RandomValue()
    {
        if(UnityEngine.Random.value > 0.5)
        {
            return UnityEngine.Random.value * 6;
        }
        else
        {
            return -UnityEngine.Random.value * 6;
        }
    }
}