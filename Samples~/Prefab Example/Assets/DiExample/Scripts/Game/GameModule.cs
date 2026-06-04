using System.Threading.Tasks;
using RPGFramework.DI;
using UnityEngine;
using UnityEngine.UIElements;

namespace DiExample.Game
{
    public class GameModule : IGameModule
    {
        private readonly ICore m_Core;
        private readonly IDIResolver m_Resolver;

        private VisualElement m_RootUI;
        private Button m_PrefabButton;
        private Button m_SceneButton;

        public GameModule(ICore core, IDIResolver resolver)
        {
            m_Core = core;
            m_Resolver = resolver;
        }

        public Task OnEnterAsync()
        {
            m_RootUI = Object.FindAnyObjectByType<UIDocument>().rootVisualElement;

            m_PrefabButton = m_RootUI.Q<Button>("PrefabButton");
            m_SceneButton = m_RootUI.Q<Button>("SceneButton");

            m_PrefabButton.RegisterCallback<ClickEvent>(OnPrefabButtonClicked);
            m_SceneButton.RegisterCallback<ClickEvent>(OnFrontEndButtonClicked);

            return Task.CompletedTask;
        }

        public Task OnExitAsync()
        {
            m_SceneButton.UnregisterCallback<ClickEvent>(OnFrontEndButtonClicked);
            m_PrefabButton.UnregisterCallback<ClickEvent>(OnPrefabButtonClicked);

            return Task.CompletedTask;
        }

        private void OnFrontEndButtonClicked(ClickEvent e)
        {
            m_Core.LoadModuleAsync<IFrontEndModule>("FrontEnd");
        }

        private void OnPrefabButtonClicked(ClickEvent e)
        {
            IParticle particle = m_Resolver.InstantiatePrefab<IParticle>();
            
            float x = Random.Range(-10f, 10f);
            float y = Random.Range(-10f, 10f);

            particle.SetPosition(new Vector3(x, y, 0));
        }
    }
}