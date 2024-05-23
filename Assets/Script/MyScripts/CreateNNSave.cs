using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateNNSave : MonoBehaviour
{
    public AIControl aiControl;

    float mutationFactor;
    float mutationThreshhold;
    int populationCount;

    int layerCount;
    int layerSize;
    int inputCount = 13;
    int outputCount = 2;

    string saveName;

    GameObject AIMenu;

    bool flag1 = false;
    bool flag2 = false;
    bool flag3 = false;
    bool flag4 = false;
    bool flag5 = false;  
    bool flag6 = false;

    void Start()
    {
        AIMenu = GameObject.Find("AIMenu");
    }

    public void ReadMutationFactor(string input)
    {
        mutationFactor = float.Parse(input);
        flag1 = true;
        print("Test");
    }

    public void ReadMutaionThreshhold(string input)
    {
        mutationThreshhold = float.Parse(input);
        flag2 = true;
    }

    public void ReadPopulationCount(string input)
    {
        populationCount = int.Parse(input);
        flag3 = true;
    }

    public void ReadLayerCount(string input)
    {
        layerCount = int.Parse(input);
        flag4 = true;
    }

    public void ReadLayerSize(string input)
    {
        layerSize = int.Parse(input);
        flag5 = true;
    }

    public void ReadName(string input)
    {
        saveName = input;
        flag6 = true;
    }

    public void CreateNewSave()
    {
        if(flag1 & flag2 & flag3 & flag4 & flag5 & flag6)
        {
            aiControl = GameObject.Find("GameMaster").GetComponent<AIControl>();

            aiControl.AISaves.Add(new AISave(mutationFactor, mutationThreshhold, populationCount, layerCount, layerSize, inputCount, outputCount, saveName));
            
            AIMenu.SetActive(true);
            GameObject.Find("CreateNNMenu").SetActive(false);

            aiControl.SaveFile();

            print("New NN Created!");
        }
    }
}
