using UnityEngine;

public class InGameConsole : MonoBehaviour
{
    string logText = "";
    void OnEnable() => Application.logMessageReceived += HandleLog;

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        logText += logString + "\n";
        if (logText.Length > 5000) logText = logText.Substring(logText.Length - 5000);
    }

    void OnGUI()
    {
        GUI.TextArea(new Rect(10, 10, 500, 300), logText);
    }
}
