using UnityEngine;
using UnityEngine.SceneManagement;

namespace Footsies
{
    public class GameManager : Singleton<GameManager>
    {
        public enum SceneIndex
        {
            Title = 1,
            Battle = 2,
            AIMenu = 3,
            AITraining = 4,
        }

        public AudioClip menuSelectAudioClip;

        public SceneIndex currentScene { get; private set; }
        public bool isVsCPU { get; private set; }
        public bool humanVsNN { get; private set; }
        public bool isNNTraining { get; private set; }

        private void Awake()
        {
            DontDestroyOnLoad(this.gameObject);

            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            LoadTitleScene();
        }

        private void Update()
        {
            if(currentScene == SceneIndex.Battle)
            {
                if(Input.GetButtonDown("Cancel"))
                {
                    LoadTitleScene();
                }
            }
        }

        public void LoadTitleScene()
        {
            SceneManager.LoadScene((int)SceneIndex.Title);
            currentScene = SceneIndex.Title;
        }

        public void LoadVsPlayerScene()
        {
            isVsCPU = false;
            humanVsNN = false;
            isNNTraining = false;
            LoadBattleScene();
        }

        public void LoadVsCPUScene()
        {
            isVsCPU = true;
            humanVsNN = false;
            isNNTraining = false;
            LoadBattleScene();
        }

        public void LoadTrainingScene()
        {
            isVsCPU = false;
            humanVsNN = false;
            isNNTraining = true; 
            LoadBattleScene();
        }

        public void LoadPlayAIScene()
        {
            isVsCPU = false;
            isNNTraining = false;
            humanVsNN = true;
            LoadBattleScene();
            print("Load Play AI");
        }

        private void LoadBattleScene()
        {
            SceneManager.LoadScene((int)SceneIndex.Battle);
            currentScene = SceneIndex.Battle;

            if(menuSelectAudioClip != null)
            {
                SoundManager.Instance.playSE(menuSelectAudioClip);
            }
        }

        public void LoadAIMenu()
        {
            SceneManager.LoadScene((int)SceneIndex.AIMenu);
            currentScene = SceneIndex.AIMenu;

            if(menuSelectAudioClip != null)
            {
                SoundManager.Instance.playSE(menuSelectAudioClip);
            }
        }
    }

}