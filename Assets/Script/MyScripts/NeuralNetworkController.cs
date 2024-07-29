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
                node.Add(new List<float>());

                node[0].Add(RandomValue());
                node[0].Add(0);
                
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


    public (List<float> output, List<List<List<float>>> calculations) RunNNAndSave(List<List<List<List<float>>>> nN, List<float> input, ActivationFunctions selectedActivationFunction)
    {
        List<float> currentInput = new List<float>();
        List<float> nextInput = new List<float>(input);

        List<List<List<float>>> calculations = new List<List<List<float>>>();
        calculations.Add(new List<List<float>>());

        foreach(float inputValue in input)
        {
            calculations[0].Add(new List<float>());
            calculations[^1][^1].Add(inputValue);
            calculations[^1][^1].Add(Sigmoid(inputValue));
        }

        foreach(List<List<List<float>>> layer in nN)
        {
            calculations.Add(new List<List<float>>());

            currentInput = new List<float>(nextInput);

            nextInput.Clear();

            foreach(List<List<float>> node in layer)
            {
                calculations[^1].Add(new List<float>());

                float output = node[1].Zip(currentInput, (x, y) => x * y).Sum() + node[0][0];
                
                calculations[^1][^1].Add(output);
                calculations[^1][^1].Add(Sigmoid(output));
                nextInput.Add(Sigmoid(output));
            }
        }
        return (nextInput, calculations);
    }

    public List<List<List<float>>> SetPartialDerivatives(List<List<List<List<float>>>> nN, List<List<List<float>>> calculations, int relevantOutput = 0, bool softMax = false)
    {
        List<List<List<float>>> derivatives = new List<List<List<float>>>();
        float partialDerivativeOfSoftmax = 1;

        if(softMax)
        {
            List<float> softmaxInput = Enumerable.Range(0, calculations.LastOrDefault().Count()).Select(x => calculations.LastOrDefault()[x][1]).ToList();
            double[] softmaxOutput = SoftMaxFunction(softmaxInput);

            partialDerivativeOfSoftmax = DerivativeOfSoftmax(relevantOutput, (float)softmaxOutput[relevantOutput], relevantOutput, softmaxInput[relevantOutput]);
        }

        foreach(List<List<float>> node in nN.Last())
        {
            node[0][1] = 0;
        }
        nN.Last()[relevantOutput][0][1] = partialDerivativeOfSoftmax;

        for(int i = 0; i <= nN.Count - 1; i++)
        {
            derivatives.Add(new List<List<float>>());
        }
        
        for(int i = nN.Count - 1; i >= 0; i--)
        {
            if(i > 0)
            {
                for(int y = 0; y <= nN[i-1].Count - 1; y++) 
                {
                    nN[i-1][y][0][1] = 0;
                } 
            }

            for(int j = 0; j <= nN[i].Count - 1; j++)
            {
                derivatives[i].Add(new List<float>());

                float postSigmoidDerivative = nN[i][j][0][1];
                float preSigmoidDerivative = postSigmoidDerivative * DerivativeOfSigmoid(calculations[i+1][j][0]);
                
                nN[i][j][2].Clear();
                
                derivatives[i][j].Add(preSigmoidDerivative);

                foreach(List<float> unweightedInput in calculations[i])
                {
                    derivatives[i][j].Add(unweightedInput[1] * preSigmoidDerivative);
                }

                if(i > 0)
                {
                    for(int y = 0; y <= nN[i-1].Count - 1; y++) 
                    {
                        nN[i-1][y][0][1] += nN[i][j][1][y] * preSigmoidDerivative;
                    }
                }

            }
        }

        return derivatives;
    }

    public List<List<List<List<float>>>> GradientAscent(List<List<List<List<float>>>> nN, List<List<List<float>>> derivatives)
    {
        float toBeAddedValue = derivatives.LastOrDefault().LastOrDefault().LastOrDefault();

        for(int i = 0; i <= nN.Count-1; i++)
        {
            for(int j = 0; j <= nN[i].Count - 1; j++)
            {
                nN[i][j][0][0] += toBeAddedValue * derivatives[i][0][1];

                for(int y = 0; y <= nN[i][j][1].Count -1; y++)
                {
                    nN[i][j][1][y] += toBeAddedValue * derivatives[i][0][y++];
                }
            }
        }

        return nN;
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

    //https://gist.github.com/jogleasonjr/55641e503142be19c9d3692b6579f221
    double[] SoftMaxFunction(List<float> input)
    {
        double[] inputArray = Array.ConvertAll(input.ToArray(), x => (double)x);
        var inputArray_exp = inputArray.Select(Math.Exp);
        var sum_inputArray_exp = inputArray_exp.Sum();

        return inputArray_exp.Select(i => i / sum_inputArray_exp).ToArray();
    }

    float DerivativeOfSoftmax(int inRegardsToOutput, float output, int inRegardsToInput, float input)
    {
        if(inRegardsToOutput == inRegardsToInput)
        {
            return output*(1 - input);
        }
        return -output * input;
    }


}