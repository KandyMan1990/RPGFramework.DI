using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace DiExample.FrontEnd
{
    public class FrontEndModule : IFrontEndModule
    {
        private readonly ICore m_Core;

        private VisualElement m_RootUI;
        private Button m_SceneButton;

        public FrontEndModule(ICore core)
        {
            m_Core = core;
        }

        public Task OnEnterAsync()
        {
            m_RootUI = Object.FindAnyObjectByType<UIDocument>().rootVisualElement;

            m_SceneButton = m_RootUI.Q<Button>("SceneButton");

            m_SceneButton.RegisterCallback<ClickEvent>(OnGameButtonClicked);

            return Task.CompletedTask;
        }

        public Task OnExitAsync()
        {
            m_SceneButton.UnregisterCallback<ClickEvent>(OnGameButtonClicked);

            return Task.CompletedTask;
        }

        private void OnGameButtonClicked(ClickEvent e)
        {
            m_Core.LoadModuleAsync<IGameModule>("Game");
        }
    }
}