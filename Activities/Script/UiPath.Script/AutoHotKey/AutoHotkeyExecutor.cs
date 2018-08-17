using AutoHotkey.Interop;
using System.IO;
using System.Linq;

namespace UiPath.Script.AutoHotKey
{
    public class AutoHotkeyExecutor
    {
        AutoHotkeyEngine ahk;

        public AutoHotkeyExecutor()
        {
            ahk = new AutoHotkeyEngine();
        }

        public void Load(string filePath)
        {
            ahk.Load(filePath);
        }

        public void Load(FileInfo info)
        {
            Load(info.FullName);
        }

        public string ExecuteFunction(string functionName, params string[] parameters)
        {
            var tempList = parameters.ToList();

            while (tempList.Count < 10)
            {
                tempList.Add(null);
            }

            return ahk.ExecFunction(functionName,
                tempList[0], tempList[1], tempList[2], tempList[3], tempList[4],
                tempList[5], tempList[6], tempList[7], tempList[8], tempList[9]);
        }

        public void Terminate()
        {
            ahk.Terminate();
        }

        public string ExecuteRaw(string code)
        {
            ahk.ExecRaw(code);
            return string.Empty;
        }

        public string ExecuteRaw(FileInfo info)
        {
            return ExecuteRaw(File.ReadAllText(info.FullName));
        }

        public void SetVariable(string variableName, string value)
        {
            ahk.SetVar(variableName, value);
        }

        public string GetVariable(string variableName)
        {
            return ahk.GetVar(variableName);
        }

        public bool FunctionExists(string functionName)
        {
            return ahk.FunctionExists(functionName);
        }
    }
}
