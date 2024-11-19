using System.IO;
using System.Text.Json;

namespace KalugaBus.AdminPanel.Services;

public class OptionsService<T> where T : new()
{
    public T Value { get; set; }
    private string _fileName;
    
    public OptionsService(string fileName)
    {
        _fileName = fileName;
        if (!File.Exists(fileName))
        {
            Value = new T();
            return;
        }
        var json = File.ReadAllText(fileName);
        Value = JsonSerializer.Deserialize<T>(json) ?? new T();
    }
    
    public OptionsService() : this("settings.json") { }

    public void Save()
    {
        var json = JsonSerializer.Serialize(Value);
        File.WriteAllText(_fileName, json);
    }
}