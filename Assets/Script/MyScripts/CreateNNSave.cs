using UnityEngine;

public class CreateNNSave : MonoBehaviour
{
    public AIControl aiControl;

    private string saveName;
    private bool sigmoid = true;

    private float decayRate;
    private float policyLearningRate;
    private int policyLayerCount;
    private int policyLayerSize;
    private int policyInputCount = 17;
    private int policyOutputCount = 7;
    
    private float valueLearningRate;
    private int valueLayerCount;
    private int valueLayerSize;
    private int valueInputCount = 17;
    private int valueOutputCount = 1;

    GameObject AIMenu;

    bool flag1 = false;
    bool flag2 = false;
    bool flag3 = false;
    bool flag4 = false;
    bool flag5 = false;  
    bool flag6 = false;
    bool flag7 = false;
    bool flag8 = false;
    bool flag9 = false;

    void Start()
    {
        AIMenu = GameObject.Find("AIMenu");
    }

    public void ReadDecayRate(string input)
    {
        decayRate = float.Parse(input);
        flag1 = true;
        print("Test");
    }

    public void ReadPolicyLearningRate(string input)
    {
        policyLearningRate = float.Parse(input);
        flag2 = true;
    }

    public void ReadPolicyLayerCount(string input)
    {
        policyLayerCount = int.Parse(input);
        flag3 = true;
    }

    public void ReadPolicyLayerSize(string input)
    {
        policyLayerSize = int.Parse(input);
        flag4 = true;
    }

    public void ReadValueLearningRate(string input)
    {
        valueLearningRate = float.Parse(input);
        flag5 = true;
    }

    public void ReadValueLayerCount(string input)
    {
        valueLayerCount = int.Parse(input);
        flag6 = true;
    }

    public void ReadValueLayerSize(string input)
    {
        valueLayerSize = int.Parse(input);
        flag7 = true;
    }

    public void ReadName(string input)
    {
        saveName = input;
        flag8 = true;
    }

    public void ReadSigmoid(string input)
    {
        sigmoid = input == "true";
        flag9 = true;
    }

    public void CreateNewSave()
    {
        if(flag1 & flag2 & flag3 & flag4 & flag5 & flag6 & flag7 & flag8) 
        {
            aiControl = GameObject.Find("GameMaster").GetComponent<AIControl>();

            aiControl.AISaves.Add(new AISave(saveName, decayRate, policyLearningRate, policyLayerCount, policyLayerSize, policyInputCount, policyOutputCount, valueLearningRate, valueLayerCount, valueLayerSize, valueInputCount, valueOutputCount, sigmoid));
            
            AIMenu.SetActive(true);
            GameObject.Find("CreateNNMenu").SetActive(false);

            aiControl.SaveFile();

            print("New NN Created!");
        }
        else{
            print("l");
        }
    }
}
