
using UnityEngine;
using UnityEditor;
using orca.orcavoip;
// using PackageManager = UnityEditor.PackageManager;
#if UNITY_EDITOR
[InitializeOnLoad]
public class OrcaWizard : EditorWindow
{
    string AuthenticationToken = "AuthKey";


    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(OrcaWizard));
    }

    //private void Awake()
    //{
    //    if (OrcaVOIP.AppSettings != null && !string.IsNullOrEmpty(OrcaVOIP.AppSettings.AuthKey))
    //    {
    //        this.AuthenticationToken = OrcaVOIP.AppSettings.AuthKey;
    //    }
    //}

    //private static void HighlightSettings()
    //{
    //    AppSettings appSettings = (AppSettings)Resources.Load(OrcaVOIP.appSettingsFileName, typeof(AppSettings));
    //    Selection.objects = new UnityEngine.Object[] {appSettings};
    //    EditorGUIUtility.PingObject(appSettings);
    //}

    [MenuItem("Orca/Orca Wizard")]
    static void Init()
    {
        EditorWindow window = EditorWindow.CreateWindow<OrcaWizard>();
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
            SetupAuthKey(AuthenticationToken);
            
        }
        if (GUILayout.Button("Cancel"))
        {
            OrcaVOIP.LoadOrCreateSettings();
            this.Close();
        }
        GUILayout.Space(20);
        GUILayout.Label("You can always close this window and setup later from the menu");
    }

    //public override void SaveChanges()
    //{
    //    orca.SetAuthKey(AuthenticationToken);
    //    base.SaveChanges();
    //}

    private void SetupAuthKey(string AuthKey)
    {
        Debug.Log($"Setting up ORCA with key: {AuthKey}");
        var settings = (AppSettings)Resources.Load("OrcaSetting", typeof(AppSettings));
        if (settings != null)
        {
            settings.AuthKey = AuthKey;
            return;
        }
        OrcaVOIP.LoadOrCreateSettings();
        OrcaVOIP.AppSettings.AuthKey = AuthKey;
        return;
    }

    static string MakeProjectSpecificEditorPrefKey(string k)
    {
        int projectPathHash = Application.dataPath.GetHashCode();
        return $"{projectPathHash:X}.{k}";
    }

    public static void SetString(string k, string v) => EditorPrefs.SetString(MakeProjectSpecificEditorPrefKey(k), v);
    public static string GetString(string k) => EditorPrefs.GetString(MakeProjectSpecificEditorPrefKey(k));

}

#endif
