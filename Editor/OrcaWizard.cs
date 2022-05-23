
using UnityEngine;
using UnityEditor;
using orca.orcavoip;
using PackageManager = UnityEditor.PackageManager;

public class OrcaWizard : EditorWindow
{
    string AuthenticationToken = "AuthKey";


    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(OrcaWizard));
    }

    [MenuItem("Orca/Orca Wizard")]
    [InitializeOnLoadMethod]
    static void Init()
    {
        // var request = PackageManager.Client.Add("https://github.com/endel/NativeWebSocket.git#upm");
        // if (request.IsCompleted)
        // {
        //     if (request.Status == PackageManager.StatusCode.Success)
        //     {
        //         Debug.Log("Package imported!");
        //     }
        // }
        var packages = PackageManager.PackageInfo.GetAllRegisteredPackages();
        foreach (var package in packages)
        {
            Debug.Log(package.packageId);
        }
        EditorWindow window = EditorWindow.CreateInstance<OrcaWizard>();
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("ORCA Wizard Setup", EditorStyles.boldLabel);
        GUILayout.Space(20);
        GUILayout.Label("This wizard will help you to setup ORCA for your project.\n", EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);
        GUILayout.Label("Enter the Authentication Key provided from the Dashboard.\n", EditorStyles.wordWrappedLabel);

        AuthenticationToken = EditorGUILayout.TextField("", AuthenticationToken);
        if (GUILayout.Button("Setup ORCA"))
        {
            Debug.Log("Setting up ORCA");
            SetupAuthKey(AuthenticationToken);
        }
        if (GUILayout.Button("Cancel"))
        {
            this.Close();
        }
        GUILayout.Space(20);
        GUILayout.Label("You can always close this window and setup later from the menu");
    }

    private void SetupAuthKey(string AuthKey)
    {
        Debug.Log($"Setting up ORCA with key: {AuthKey}");
        OrcaVOIP.SetAuthKey(AuthKey);
    }

}
