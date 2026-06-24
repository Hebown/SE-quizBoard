using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizNative.Models;
using QuizNative.Services;

namespace QuizNative.ViewModels;

public partial class WrongBookViewModel : ObservableObject
{
    private readonly IQuizDataService _quizDataService;
    private readonly ILocalStateService _localStateService;

    public ObservableCollection<Question> WrongQuestions { get; } = new();

    [ObservableProperty]
    private Question? _currentQuestion;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private int _currentIndex = -1;

    [ObservableProperty]
    private string? _selectedOption;

    [ObservableProperty]
    private bool _isAnswerRevealed;

    [ObservableProperty]
    private string _feedbackText = string.Empty;

    [ObservableProperty]
    private bool _isCurrentAnswerCorrect;

    public int TotalQuestions => WrongQuestions.Count;
    public int CurrentQuestionNumber => CurrentIndex + 1;
    public bool HasWrongQuestions => WrongQuestions.Count > 0;
    public bool CanNext => CurrentIndex >= 0 && CurrentIndex < WrongQuestions.Count - 1;

    public WrongBookViewModel(IQuizDataService quizDataService, ILocalStateService localStateService)
    {
        _quizDataService = quizDataService;
        _localStateService = localStateService;
        RefreshWrongQuestions();
    }

    /// <summary>从本地存储刷新错题列表（读取 WrongCounts 字典的 Key）</summary>
    public void RefreshWrongQuestions()
    {
        WrongQuestions.Clear();
        var wrongIds = _localStateService.Progress.WrongCounts.Keys;
        foreach (var id in wrongIds)
        {
            var q = _quizDataService.GetQuestionById(id);
            if (q != null) WrongQuestions.Add(q);
        }

        OnPropertyChanged(nameof(HasWrongQuestions));
        OnPropertyChanged(nameof(TotalQuestions));

        CurrentIndex = WrongQuestions.Count > 0 ? 0 : -1;
    }

    partial void OnCurrentIndexChanged(int value)
    {
        if (value >= 0 && value < WrongQuestions.Count)
        {
            CurrentQuestion = WrongQuestions[value];
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
    private void JudgeWrongAnswer(string optionText)
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
            FeedbackText = "🎉 太棒了，这次做对了！该题已从错题本中移除。";

            _localStateService.RecordCorrectAnswer(CurrentQuestion.Id);
            _localStateService.RemoveFromWrongQuestions(CurrentQuestion.Id);

            int nextIndex = CurrentIndex;
            RefreshWrongQuestions();

            if (WrongQuestions.Count > 0 && nextIndex < WrongQuestions.Count)
            {
                CurrentIndex = nextIndex;
            }
        }
        else
        {
            IsCurrentAnswerCorrect = false;
            FeedbackText = $"❌ 仍然错误。标准答案是：{correctLetter}。继续加油！";
        }
    }

    [RelayCommand(CanExecute = nameof(CanNext))]
    private void Next() => CurrentIndex++;
}
