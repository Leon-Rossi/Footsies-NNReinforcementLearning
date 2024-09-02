using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;
using UnityEngine;

public class Tests : MonoBehaviour
{
    AIControl aiControl;
    NeuralNetworkController neuralNetworkController;
    // Start is called before the first frame update
    void Start()
    {
        aiControl = GameObject.Find("GameMaster").GetComponent<AIControl>();
        neuralNetworkController = GameObject.Find("GameMaster").GetComponent<NeuralNetworkController>();

        var nN = neuralNetworkController.CreateNN(3, 8, 2, 1);
        var count = 0;
        while(count < 200000)
        {
            count++;
            int x = Random.Range(0, 2);
            int y = Random.Range(0, 2);
            var runNNandSave = neuralNetworkController.RunNNAndSave(nN,  new List<float>(){x, y});
            var derivative = neuralNetworkController.SetPartialDerivatives(nN, runNNandSave.calculations, true);
            print(y + " " + x + " " + runNNandSave.output[0]);

            if(x == 1 && y == 1)
            {
                if(runNNandSave.output[0] < 0.5)
                {
                    derivative.LastOrDefault().LastOrDefault().Add(0);
                    print("Correct");
                }
                else
                {
                    derivative.LastOrDefault().LastOrDefault().Add(-0.001f);
                    print("Wrong");
                }
            }

            if((x == 1 | y == 1)&& runNNandSave.output[0] >= 0.5)
            {
                derivative.LastOrDefault().LastOrDefault().Add(0);
                print("Correct");
            }
            else if(x == 0 && y == 0 && runNNandSave.output[0] < 0.5)
            {
                derivative.LastOrDefault().LastOrDefault().Add(0);
                print("Correct");

            }
            else if(x == 1 | y == 1)
            {
                derivative.LastOrDefault().LastOrDefault().Add(0.001f);
                print("Wrong");
            }
            else
            {
                derivative.LastOrDefault().LastOrDefault().Add(-0.001f);
                print("Wrong");
            }
            nN = neuralNetworkController.GradientAscent(nN, derivative);
        }
        print(nN[0][0][1][0]);
        print(nN[0][0][1][1]);
        print(nN[0][0][0][0]);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Save()
    {
        var nN = neuralNetworkController.CreateNN(0, 0, 2, 1);
        var runNN = neuralNetworkController.RunNN(nN, new List<float>(){1, 2, 1, 0, 50, 5, 0});
        var runNNandSave = neuralNetworkController.RunNNAndSave(nN,  new List<float>(){1, 2, 1, 0, 50, 5, 0});
        print(runNN[0] + " " + runNNandSave.output[0]);

        runNN = neuralNetworkController.RunNN(nN, new List<float>(){0, 0, 5, 0, 50, 2, 1});
        runNNandSave = neuralNetworkController.RunNNAndSave(nN,  new List<float>(){0, 0, 5, 0, 50, 2, 1});
        print(runNN[0] + " " + runNNandSave.output[0]);

    }
}
