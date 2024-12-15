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

    public bool displayInfo;

    string saveFilePath;

    string json;

    bool playHuman;
    int generation;



    // Start is called before the first frame update
    void Start()
    {
        saveFilePath = Application.persistentDataPath + "/GameData";

        CreateJSONFile();
        LoadFile();
        json = "";
    }

    void Update()
    {
        if(speed != Time.timeScale)
        {
            Time.timeScale = speed;
        }
    }

    void CreateJSONFile()
    {
        if(!Directory.Exists(saveFilePath))
        {
            Directory.CreateDirectory(saveFilePath);
            print(saveFilePath);
            print("New Save File created");
        }
        print(saveFilePath);
        print("Found Save File");
    }

    void LoadFile()
    {
        foreach(string filePath in Directory.GetFiles(saveFilePath))
        {
            json = File.ReadAllText(filePath);
            print(JsonConvert.DeserializeObject<AISaveClass>(json).AISave.saveName);
            AISaves.Add(JsonConvert.DeserializeObject<AISaveClass>(json).AISave);
        }
        
        print("try File Load");
    }

    public void SaveFile()
    {
        Time.timeScale = speed;
        foreach(AISave aiSave in AISaves)
        {
            json = JsonConvert.SerializeObject(new AISaveClass(aiSave), Formatting.Indented, 
            new JsonSerializerSettings 
            {  
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            print(saveFilePath + "/" + aiSave.saveName + ".json");
            if(!File.Exists(saveFilePath + "/" + aiSave.saveName + ".json"))
            {
                File.Create(saveFilePath + "/" + aiSave.saveName + ".json").Close();
                print(saveFilePath + "/" + aiSave.saveName);
                print("New Save File created");
            }
            File.WriteAllText(saveFilePath + "/" + aiSave.saveName + ".json", json);
        }

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

        public class AISaveClass
    {
        public AISave AISave;

        public void init(AISave AISaveInit)
        {
            AISave = AISaveInit;
        }

        public AISaveClass(AISave AISaveInit)
        {
            AISave = AISaveInit;
        }
    }
}
