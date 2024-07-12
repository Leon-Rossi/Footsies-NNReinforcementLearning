using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEditor.Experimental.GraphView;

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
            node.Add(new List<float>());

            node[0].Add(RandomValue());
            node[0].Add(0);
            node[0].Add(0);
            
            foreach(int i in Enumerable.Range(1, layerSize))
            {
                node[1].Add(RandomValue());
            }

        }

        return nN;
    }

    public List<float> RunNN(List<List<List<List<float>>>> nN, List<float> input, ActivationFunctions selectedActivationFunction)
    {
        List<float> currentInput = new List<float>();
        List<float> nextInput = new List<float>(input); 

        foreach(List<List<List<float>>> layer in nN)
        {
            currentInput = new List<float>(nextInput);

            nextInput.Clear();

            foreach(List<List<float>> node in layer)
            {
                float output = node[1].Zip(currentInput, (x, y) => x * y).Sum() + node[0][0];
                
                nextInput.Add(Sigmoid(output));
            }
        }
        return nextInput;
    }


    public List<float> NNForwardPass(List<List<List<List<float>>>> nN, List<float> input, ActivationFunctions selectedActivationFunction)
    {
        List<float> currentInput = new List<float>();
        List<float> nextInput = new List<float>(input); 

        foreach(List<List<List<float>>> layer in nN)
        {
            currentInput = new List<float>(nextInput);

            nextInput.Clear();

            foreach(List<List<float>> node in layer)
            {
                float output = node[1].Zip(currentInput, (x, y) => x * y).Sum() + node[0][0];
                
                node[0][2] = output;
                node[0][3] = Sigmoid(output);
                nextInput.Add(Sigmoid(output));
            }
        }
        return nextInput;
    }

    public List<List<List<List<float>>>> SetPartialDerivatives(List<List<List<List<float>>>> nN, float learningRate, int relevantOutput = 0)
    {
        foreach(List<List<float>> node in nN.Last())
        {
            node[0][1] = 0;
        }
        nN.Last()[relevantOutput][0][1] = 1;
        
        for(int i = nN.Count - 1; i >= 0; i--)
        {
            foreach(List<List<float>> node in nN[i])
            {
                float postSigmoidDerivative = node[0][1];
                float preSigmoidDerivative = postSigmoidDerivative * DerivativeOfSigmoid(node[0][2]);
                
                node[2].Clear();

                if(i > 0)
                {
                    foreach(List<List<float>> targetNode in nN[i-1])
                    {
                        node[2].Add(targetNode[0][3] * preSigmoidDerivative);
                    }

                    for(int j = 0; j <= nN[i-1].Count - 1; i++) 
                    {
                        nN[i][j][0][1] += node[2][j] * preSigmoidDerivative;
                    }
                }

                node[0][1] = preSigmoidDerivative;
            }
        }

        return nN;
    }

    public float OneStopMethod(List<List<List<List<float>>>> nN, float toBeAddedValue)
    {
        foreach(List<List<List<float>>> layer in nN)
        {
            foreach(List<List<float>> node in layer)
            {
                for(int i = 0; i < node.Count; i++)
                {
                    weight += 
                }
            }
        }
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

    float DerivativeOfSigmoid(float input)
    {
        return Sigmoid(input) * (1 - Sigmoid(input));
    }

    float Sigmoid(float input)
    {
        return (float)(1 /(1+Math.Exp(-input)));
    }
}