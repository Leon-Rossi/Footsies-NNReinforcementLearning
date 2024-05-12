using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngineInternal;

public class SelectNN : MonoBehaviour
{
    AIControl aiControl;
    List<AISave> aiSaves;
    public int selectedSafe;
    public Text textField;

    int speed;
    
    string textOutput;
    // Start is called before the first frame update
    void Start()
    {
        aiControl = GameObject.Find("GameMaster").GetComponent<AIControl>();
    }

    public void ReadSpeed(string input)
    {
        speed = int.Parse(input);
        aiControl.speed = speed;

    }

        public void ReadGeneration(string input)
    {
        speed = int.Parse(input);
        aiControl.speed = speed;

    }

    public void ReadName(string input)
    {
        foreach(AISave save in aiSaves)
        {
            if(save.saveName == input)
            {
                selectedSafe = aiSaves.IndexOf(save);
            }
        }
        aiControl.currentAISave = selectedSafe;
    }

    public void DisplayAINames()
    {
        aiSaves = aiControl.AISaves;
        textOutput = null;

        foreach(AISave save in aiSaves)
        {
            print(save.saveName);
            textOutput += save.saveName + Environment.NewLine;
        }

        print(textOutput);
        textField.text = textOutput;
        
    }
}
