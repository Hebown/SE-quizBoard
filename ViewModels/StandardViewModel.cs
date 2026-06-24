using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizNative.Models;
using QuizNative.Services;

namespace QuizNative.ViewModels;

/// <summary>
/// 带显示文本的章节项
/// </summary>
public record ChapterItem(string Id, string DisplayText);

public partial class StandardViewModel : ObservableObject
{
    private readonly IQuizDataService _quizDataService;
    private readonly ILocalStateService _localStateService;

    /// <summary>所有可供选择的章节列表</summary>
    public ObservableCollection<ChapterItem> Chapters { get; } = new();

    /// <summary>当前选中章节下的全部题目</summary>
    public ObservableCollection<Question> CurrentQuestions { get; } = new();

    [ObservableProperty]
    private string? _selectedChapter;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviousCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private int _currentIndex = -1;

    [ObservableProperty]
    private Question? _currentQuestion;

    [ObservableProperty]
    private string? _selectedOption;

    [ObservableProperty]
    private bool _isAnswerRevealed;

    [ObservableProperty]
    private string _feedbackText = string.Empty;

    [ObservableProperty]
    private bool _isCurrentAnswerCorrect;

    public int TotalQuestions => CurrentQuestions.Count;
    public int CurrentQuestionNumber => CurrentIndex + 1;
    public bool HasQuestions => CurrentQuestions.Count > 0;

    public StandardViewModel(IQuizDataService quizDataService, ILocalStateService localStateService)
    {
        _quizDataService = quizDataService;
        _localStateService = localStateService;
        LoadChapters();
    }

    private void LoadChapters()
    {
        Chapters.Clear();
        foreach (var ch in _quizDataService.GetChapters())
        {
            Chapters.Add(new ChapterItem(ch, $"第 {ch} 章节"));
        }
    }

    partial void OnSelectedChapterChanged(string? value)
    {
        CurrentQuestions.Clear();
        if (!string.IsNullOrEmpty(value))
        {
            var questions = _quizDataService.GetQuestionsByChapter(value);
            foreach (var q in questions) CurrentQuestions.Add(q);
            OnPropertyChanged(nameof(HasQuestions));
            OnPropertyChanged(nameof(TotalQuestions));
            // 强制归零：先 -1 再 0，确保彻底触发
            CurrentIndex = -1;
            CurrentIndex = CurrentQuestions.Count > 0 ? 0 : -1;
        }
        else
        {
            CurrentIndex = -1;
        }
    }

    partial void OnCurrentIndexChanged(int value)
    {
        if (value >= 0 && value < CurrentQuestions.Count)
        {
            CurrentQuestion = CurrentQuestions[value];
            OnPropertyChanged(nameof(CurrentQuestionNumber));
            ResetAnswerState();
        }
        else
        {
            CurrentQuestion = null;
        }
    }

    private void ResetAnswerState()
    {
        SelectedOption = null;
        IsAnswerRevealed = false;
        FeedbackText = string.Empty;
        IsCurrentAnswerCorrect = false;
    }

    [RelayCommand]
    private void JudgeAnswer(string optionText)
    {
        if (IsAnswerRevealed || CurrentQuestion == null || string.IsNullOrEmpty(optionText))
            return;

        SelectedOption = optionText;
        IsAnswerRevealed = true;

        string chosenLetter = optionText.Trim().Split('.')[0].Trim();
        string correctLetter = CurrentQuestion.Answer.Trim();

        if (chosenLetter.Equals(correctLetter, StringComparison.OrdinalIgnoreCase))
        {
            IsCurrentAnswerCorrect = true;
            FeedbackText = "🎉 回答正确！";
            _localStateService.RecordCorrectAnswer(CurrentQuestion.Id);
        }
        else
        {
            IsCurrentAnswerCorrect = false;
            FeedbackText = $"❌ 回答错误。标准答案是：{correctLetter}";
            _localStateService.RecordWrongAnswer(CurrentQuestion.Id);
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private void Previous() => CurrentIndex--;
    private bool CanGoPrevious() => CurrentIndex > 0;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next() => CurrentIndex++;
    private bool CanGoNext() => CurrentIndex >= 0 && CurrentIndex < CurrentQuestions.Count - 1;
}
