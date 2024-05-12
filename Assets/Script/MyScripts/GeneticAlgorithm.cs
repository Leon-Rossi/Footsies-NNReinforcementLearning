using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using System;

public class GeneticAlgorithm : MonoBehaviour
{

    public List<List<List<List<List<float>>>>> CreateNewPopulation(int populationCount, float mutationFactor, float mutationThreshhold, List<List<List<List<List<float>>>>> list)
    {
        List<List<List<List<List<float>>>>> output = new List<List<List<List<List<float>>>>>();
        list = SortListByFitness(list);

        foreach(List<List<List<List<float>>>> i in list)
        {
            Debug.Log(Math.Sqrt(i[0][0][0][1]));
        }

        float allFitnessValues = 0;

        foreach(List<List<List<List<float>>>> i in list)
        {
            allFitnessValues += i[0][0][0][1];
        }

        output.Add(list.Last());
        
        foreach(int i in Enumerable.Range(1, populationCount-1))
        {
            output.Add(Mutate(mutationFactor, mutationThreshhold, CrossoverUniform(SelectionFromList(list, allFitnessValues), SelectionFromList(list, allFitnessValues))));
        }

        return output;
    }

    public List<List<List<List<float>>>> SelectionFromList(List<List<List<List<List<float>>>>> list, float allFitnessValues)
    {
        allFitnessValues = 0;

        foreach(List<List<List<List<float>>>> i in list)
        {
            allFitnessValues += i[0][0][0][1];
        }
        
        float selectedInt = UnityEngine.Random.value * allFitnessValues;

        foreach(List<List<List<List<float>>>> i in list)
        {
            selectedInt -= i[0][0][0][1];
            
            if(selectedInt <= 0)
            {
                List<List<List<List<float>>>> output = CreateSerializedCopy<List<List<List<List<float>>>>>(i);
                return output;
            }

        }

        print("SelectionFromListError");      
        return list.Last();
    }

    public List<List<List<List<float>>>> CrossoverUniform(List<List<List<List<float>>>> list1, List<List<List<List<float>>>> list2)
    {
        List<List<List<List<float>>>> output = list1;

        foreach(int x in Enumerable.Range(0, output.Count()-1))
        {
            foreach(int y in Enumerable.Range(0, output[x].Count()-1))
            {
                if(UnityEngine.Random.value > 0.5)
                {
                    output[x][y][0][0] = list2[x][y][0][0];
                }
                
                foreach(int z in Enumerable.Range(0, output[x][y][1].Count()-1))
                {
                    if(UnityEngine.Random.value > 0.5)
                    {
                        output[x][y][1][z] = list2[x][y][1][z];
                    }
                }
            }
        }

        output[0][0][0][1] = 0;

        return output;

    }

    public List<List<List<List<float>>>> Mutate(float mutationFactor, float mutationThreshhold, List<List<List<List<float>>>> list)
    {
        double this_MutationConstant = UnityEngine.Random.value * mutationFactor;

        foreach(int x in Enumerable.Range(0, list.Count()-1))
        {
            foreach(int y in Enumerable.Range(0, list[x].Count()-1))
            {
                if(UnityEngine.Random.value * this_MutationConstant > mutationThreshhold)
                {
                    list[x][y][0][0] += RandomValue();
                }

                foreach(int z in Enumerable.Range(0, list[x][y][1].Count()-1))
                {
                    if(UnityEngine.Random.value * this_MutationConstant > mutationThreshhold)
                    {
                        list[x][y][1][z] += RandomValue();
                    }

                    if(list[x][y][1][z] > 20)
                    {
                        list[x][y][1][z] = 20;
                    }
                    else if(list[x][y][1][z] < -20)
                    {
                        list[x][y][1][z] = -20;
                    }
                }
            }
        }

        return list;
    }

    public List<List<List<List<List<float>>>>> SortListByFitness(List<List<List<List<List<float>>>>> list)
    {
        List<List<List<List<List<float>>>>> output = new List<List<List<List<List<float>>>>>();

        while(list.Count() != 0)
        {
            int smallest = 0;

            foreach (int i in Enumerable.Range(0, list.Count()))
            {
                if(list[i][0][0][0][1] < list[smallest][0][0][0][1])
                {
                    smallest = i;
                }

            }
            output.Add(list[smallest]);
            list.RemoveAt(smallest);
            
        }

        return output;
    }

    float RandomValue()
    {
        if(UnityEngine.Random.value > 0.5)
        {
            return UnityEngine.Random.value * 3;
        }
        else
        {
            return -UnityEngine.Random.value * 3;
        }
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
