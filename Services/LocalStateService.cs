using System.IO;
using System.Text.Json;
using Windows.Storage;
using QuizNative.Models;

namespace QuizNative.Services;

public class LocalStateService : ILocalStateService
{
    private readonly string _filePath;
    public UserProgress Progress { get; private set; } = new();

    public LocalStateService()
    {
        // 纯正 WinUI 3 本地沙盒路径：AppData\Local\Packages\[Package_Id]\LocalState\
        _filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "user_progress.json");
    }

    public async Task LoadProgressAsync()
    {
        if (!File.Exists(_filePath))
        {
            Progress = new UserProgress();
            return;
        }

        try
        {
            string json = await File.ReadAllTextAsync(_filePath);
            Progress = JsonSerializer.Deserialize<UserProgress>(json) ?? new UserProgress();
        }
        catch
        {
            // 容错处理：如果文件损坏，初始化空数据
            Progress = new UserProgress();
        }
    }

    public async Task SaveProgressAsync()
    {
        try
        {
            string json = JsonSerializer.Serialize(Progress, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch
        {
            // 生产环境可记录日志
        }
    }

    public void RecordCorrectAnswer(string questionId)
    {
        Progress.CorrectCounts[questionId] = Progress.CorrectCounts.GetValueOrDefault(questionId) + 1;
        _ = SaveProgressAsync();
    }

    public void RecordWrongAnswer(string questionId)
    {
        Progress.WrongCounts[questionId] = Progress.WrongCounts.GetValueOrDefault(questionId) + 1;
        _ = SaveProgressAsync();
    }

    public void RemoveFromWrongQuestions(string questionId)
    {
        if (Progress.WrongCounts.Remove(questionId))
        {
            _ = SaveProgressAsync();
        }
    }
}
