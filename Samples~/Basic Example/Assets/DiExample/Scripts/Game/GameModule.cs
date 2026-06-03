using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace DiExample.Game
{
    public class GameModule : IGameModule
    {
        private readonly ICore m_Core;
        private readonly IScoreManager m_ScoreManager;

        private VisualElement m_RootUI;
        private Button m_ScoreButton;
        private Button m_SceneButton;

        public GameModule(ICore core, IScoreManager scoreManager)
        {
            m_Core = core;
            m_ScoreManager = scoreManager;
        }

        public Task OnEnterAsync()
        {
            m_RootUI = Object.FindAnyObjectByType<UIDocument>().rootVisualElement;

            m_ScoreButton = m_RootUI.Q<Button>("ScoreButton");
            m_SceneButton = m_RootUI.Q<Button>("SceneButton");

            m_ScoreButton.RegisterCallback<ClickEvent>(OnScoreButtonClicked);
            m_SceneButton.RegisterCallback<ClickEvent>(OnFrontEndButtonClicked);

            return Task.CompletedTask;
        }

        public Task OnExitAsync()
        {
            m_SceneButton.UnregisterCallback<ClickEvent>(OnFrontEndButtonClicked);
            m_ScoreButton.UnregisterCallback<ClickEvent>(OnScoreButtonClicked);

            return Task.CompletedTask;
        }

        private void OnFrontEndButtonClicked(ClickEvent e)
        {
            m_Core.LoadModuleAsync<IFrontEndModule>("FrontEnd");
        }

        private void OnScoreButtonClicked(ClickEvent e)
        {
            m_ScoreManager.IncreaseScore();
        }
    }
}