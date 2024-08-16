using System;
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

    void CreateJSONFile()
    {
        if(!File.Exists(saveFilePath))
        {
            File.Create(saveFilePath);
            print(saveFilePath);
            print("New Save File created");
        }
        print(saveFilePath);
        print("Found Save File");
    }

    //Saves the AISaves List into a JSON FLie
    public void SaveFile()
    {
        Time.timeScale = speed;

        json = JsonConvert.SerializeObject(new AISavesClass(AISaves), Formatting.Indented, 
        new JsonSerializerSettings 
        {  
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });

        File.WriteAllText(saveFilePath, json);
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
