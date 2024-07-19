using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Footsies
{
    public class UIEventAction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        public enum Action
        {
            LoadVsCPU,
            LoadVsPlayer,
            ExitGame,
            BGMToggle,
            SEToggle,
            AIMenu,
            AITraining,
            PlayAI,
            LoadMenu,
        }

        public Action action;

        private void Awake()
        {
            if(action == Action.BGMToggle)
            {
                var toggle = gameObject.GetComponent<Toggle>();
                if (toggle != null)
                {
                    toggle.isOn = SoundManager.Instance.isBGMOn;
                }
            }
        }

        public void InvokeAction()
        {
            switch(action)
            {
                case Action.LoadVsCPU:
                    LoadVsCPU();
                    break;
                case Action.LoadVsPlayer:
                    LoadVsPlayer();
                    break;
                case Action.ExitGame:
                    ExitGame();
                    break;
                case Action.BGMToggle:
                    toggleBGM();
                    break;
                case Action.SEToggle:
                    break;
                case Action.AIMenu:
                    LoadAIMenu();
                    break;
                case Action.AITraining:
                    LoadTrainingScene();
                    break;
                case Action.PlayAI:
                    LoadPlayAIScene();
                    break;
                case Action.LoadMenu:
                    LoadMenu();
                    break;


            }
        }

        public void LoadMenu()
        {
            GameManager.Instance.LoadTitleScene();
        }

        public void LoadVsCPU()
        {
            GameManager.Instance.LoadVsCPUScene();
        }

        public void LoadAIMenu()
        {
            GameManager.Instance.LoadAIMenu();
        }

        public void LoadVsPlayer()
        {
            GameManager.Instance.LoadVsPlayerScene();
        }

        public void LoadTrainingScene()
        {
            GameManager.Instance.LoadTrainingScene();
        }

        public void LoadPlayAIScene()
        {
            GameManager.Instance.LoadPlayAIScene();
        }

        public void ExitGame()
        {
            Application.Quit();
        }

        public void toggleBGM()
        {
            var isOn = SoundManager.Instance.toggleBGM();
            var toggle = gameObject.GetComponent<Toggle>();
            if(toggle != null)
            {
                toggle.isOn = isOn;
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }
    }

}