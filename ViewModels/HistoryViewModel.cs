using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using QuizNative.Services;

namespace QuizNative.ViewModels;

/// <summary>
/// 章节进度数据项（用于 XAML 列表绑定）
/// 升级版：增加 CorrectTimes / WrongTimes 多维履历统计
/// </summary>
public record ChapterProgressItem(
    string ChapterId,
    string Title,
    int PracticedCount,
    int TotalCount,
    int CorrectTimes,
    int WrongTimes)
{
    /// <summary>供原生 ProgressBar 绑定的百分比值 (0.0 ~ 100.0)</summary>
    public double Percentage => TotalCount > 0 ? ((double)PracticedCount / TotalCount) * 100 : 0;

    /// <summary>供文本显示的字符串 (例如: "5 / 8 题")</summary>
    public string RatioText => $"{PracticedCount} / {TotalCount} 题";
}

public partial class HistoryViewModel : ObservableObject
{
    private readonly IQuizDataService _quizDataService;
    private readonly ILocalStateService _localStateService;

    [ObservableProperty]
    private int _totalQuestionsCount;

    [ObservableProperty]
    private int _totalPracticedCount;

    [ObservableProperty]
    private double _totalProgressPercentage;

    [ObservableProperty]
    private int _currentWrongBookCount;

    public ObservableCollection<ChapterProgressItem> ChapterProgresses { get; } = new();

    public HistoryViewModel(IQuizDataService quizDataService, ILocalStateService localStateService)
    {
        _quizDataService = quizDataService;
        _localStateService = localStateService;
        CalculateStatistics();
    }

    /// <summary>
    /// 重算全部统计指标。每次从本地存储读取最新数据，确保绝对实时。
    /// 升级版：使用 CorrectCounts / WrongCounts 精确统计。
    /// </summary>
    public void CalculateStatistics()
    {
        ChapterProgresses.Clear();

        var chapters = _quizDataService.GetChapters();
        var correctCounts = _localStateService.Progress.CorrectCounts;
        var wrongCounts = _localStateService.Progress.WrongCounts;

        int globalTotal = 0;
        int globalPracticed = 0;

        foreach (var ch in chapters)
        {
            var questions = _quizDataService.GetQuestionsByChapter(ch);
            int chTotal = questions.Count;

            int chPracticed = 0;
            int chCorrectTimes = 0;
            int chWrongTimes = 0;

            foreach (var q in questions)
            {
                bool hasCorrect = correctCounts.ContainsKey(q.Id);
                bool hasWrong = wrongCounts.ContainsKey(q.Id);

                if (hasCorrect || hasWrong)
                    chPracticed++;

                chCorrectTimes += correctCounts.GetValueOrDefault(q.Id, 0);
                chWrongTimes += wrongCounts.GetValueOrDefault(q.Id, 0);
            }

            globalTotal += chTotal;
            globalPracticed += chPracticed;

            ChapterProgresses.Add(new ChapterProgressItem(
                ch, $"第 {ch} 章节", chPracticed, chTotal,
                chCorrectTimes, chWrongTimes
            ));
        }

        TotalQuestionsCount = globalTotal;
        TotalPracticedCount = globalPracticed;
        TotalProgressPercentage = globalTotal > 0 ? ((double)globalPracticed / globalTotal) * 100 : 0;
        CurrentWrongBookCount = _localStateService.Progress.WrongCounts.Count;
    }

    public string FormattedPercentage(double value) => $"{value:F0}%";
    public string TotalProgressText(int practiced, int total) => $"{practiced} / {total} 题";
}
