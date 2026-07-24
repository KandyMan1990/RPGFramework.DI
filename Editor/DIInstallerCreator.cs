using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Reflection;
using System.Text;

namespace RPGFramework.DI.Editor
{
    internal static class DIInstallerCreator
    {
        internal const string DI_CONTAINER_CLASS_NAME = "DIContainer_ClassName";
        internal const string DI_CONTAINER_ASSET_PATH = "DIContainer_AssetPath";

        [MenuItem("Assets/Create/RPG Framework/DI/Global Installer", priority = 0)]
        internal static void CreateGlobalInstaller()
        {
            CreateInstaller(true, "NewGlobalInstaller", "GlobalInstallerBase");
        }

        [MenuItem("Assets/Create/RPG Framework/DI/Scene Installer", priority = 1)]
        internal static void CreateSceneInstaller()
        {
            CreateInstaller(false, "NewSceneInstaller", "SceneInstallerBase");
        }

        private static void CreateInstaller(bool isGlobalInstaller, string defaultName, string baseClass)
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Installer", defaultName, "cs", "Choose Location");

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            string className     = Path.GetFileNameWithoutExtension(path);
            string scriptContent = GenerateScriptCode(isGlobalInstaller, className, baseClass);

            File.WriteAllText(path, scriptContent);

            EditorPrefs.SetString(DI_CONTAINER_CLASS_NAME, className);
            EditorPrefs.SetString(DI_CONTAINER_ASSET_PATH, Path.ChangeExtension(path, ".asset"));

            AssetDatabase.Refresh();
        }

        private static string GenerateScriptCode(bool isGlobalInstaller, string className, string baseClass)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("using RPGFramework.DI;");
            sb.AppendLine();
            sb.AppendLine($"public class {className} : {baseClass}");
            sb.AppendLine("{");
            sb.AppendLine("\tpublic override void InstallBindings(IDIContainer container)");
            sb.AppendLine("\t{");
            sb.AppendLine("\t\t// TODO: add your bindings here");
            sb.AppendLine("\t\t// container.BindSingleton<IFoo, Foo>();");
            sb.AppendLine("\t\t// container.BindSingletonFromInstance<IFoo, Foo>(m_Foo);");
            sb.AppendLine("\t\t// container.BindTransient<IFoo, Foo>();");
            sb.AppendLine("\t\t// container.BindPrefab<IEnemy>(m_EnemyPrefab);");
            sb.AppendLine("\t}");
            if (isGlobalInstaller)
            {
                sb.AppendLine();
                sb.AppendLine("\tpublic override void Bootstrap(IDIResolver resolver)");
                sb.AppendLine("\t{");
                sb.AppendLine("\t\t// TODO: init any services that can't be setup via its constructor");
                sb.AppendLine("\t\t// This method can be deleted if there is nothing to bootstrap");
                sb.AppendLine();
                sb.AppendLine("\t\t// IInventoryDatabase inventoryDb = resolver.Resolve<IInventoryDatabase>();");
                sb.AppendLine("\t\t// inventoryDb.Init();");
                sb.AppendLine("\t}");
            }
            sb.AppendLine("}");

            return sb.ToString();
        }
    }

    [InitializeOnLoad]
    internal static class InstallerCompilationHook
    {
        static InstallerCompilationHook()
        {
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        private static void OnAfterAssemblyReload()
        {
            string className = EditorPrefs.GetString(DIInstallerCreator.DI_CONTAINER_CLASS_NAME, null);
            string assetPath = EditorPrefs.GetString(DIInstallerCreator.DI_CONTAINER_ASSET_PATH, null);

            if (string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            EditorPrefs.DeleteKey(DIInstallerCreator.DI_CONTAINER_CLASS_NAME);
            EditorPrefs.DeleteKey(DIInstallerCreator.DI_CONTAINER_ASSET_PATH);

            Type type = GetTypeByName(className);

            ScriptableObject so = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(so, assetPath);
            AssetDatabase.SaveAssets();
        }

        private static Type GetTypeByName(string className)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(className);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}