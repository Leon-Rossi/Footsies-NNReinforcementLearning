using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class AIControl : MonoBehaviour
{
    public List<AISave> AISaves = new List<AISave>();
    public int currentAISave;
    public int speed = 1;

    string saveFilePath;

    string json;

    bool playHuman;
    int generation;



    // Start is called before the first frame update
    void Start()
    {
        saveFilePath = Application.persistentDataPath + "/GameData.json";

        CreateJSONFile();
        LoadFile();
        json = "";
    }

    //Sets up racket for playing against human
    public List<List<List<List<float>>>> SetUpRacket()
    {
        print(generation);
        if(generation > 0)
        {
            return AISaves[currentAISave].bestNeuralNetworks[generation];
        }
        if(generation == -1)
        {
            return AISaves[currentAISave].bestNeuralNetworks.Last();
        }

        //Returns the NN with teh highest fitness Value
        float biggestFitnessValue = 0;
        int fittestEntityIndex = 0;
        foreach(int i in Enumerable.Range(0, AISaves[currentAISave].bestNeuralNetworks.Count()-1))
        {
            if(AISaves[currentAISave].bestNeuralNetworks[i][0][0][0][1] > biggestFitnessValue)
            {
                biggestFitnessValue = AISaves[currentAISave].bestNeuralNetworks[i][0][0][0][1];
                fittestEntityIndex = i;
            }
        }

        return AISaves[currentAISave].bestNeuralNetworks[fittestEntityIndex]; 
    }

    void CreateJSONFile()
    {
        if(!File.Exists(saveFilePath))
        {
            File.Create(saveFilePath);
        }
        print(saveFilePath);
    }

    //Saves the AISaves List into a JSON FLie
    public void SaveFile()
    {
        Time.timeScale = speed;

        if(new AISavesClass(AISaves) == null)
        {
            print("1");
        }

            if(AISaves == null)
        {
            print("2");
        }

            if(new AISavesClass(AISaves) == null)
        {
            print("1");
        }

        json = JsonConvert.SerializeObject(new AISavesClass(AISaves), Formatting.Indented, 
        new JsonSerializerSettings 
        {  
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });

        File.WriteAllText(saveFilePath, json);

        print("try Save File");
    }

    //Loads AISaves from JSON File
    void LoadFile()
    {
        json = File.ReadAllText(saveFilePath);
        if(json != "")
        {
            AISaves = JsonConvert.DeserializeObject<AISavesClass>(json).AISaves;
        }
        
        print("try File Load");
    }

    public class AISavesClass
    {
        public List<AISave> AISaves;

        public void init(List<AISave> AISavesList)
        {
            AISaves = AISavesList;
        }

        public AISavesClass(List<AISave> AISavesList)
        {
            AISaves = AISavesList;
        }
    }
}
