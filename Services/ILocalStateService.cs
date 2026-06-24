using QuizNative.Models;

namespace QuizNative.Services;

public interface ILocalStateService
{
    UserProgress Progress { get; }
    Task LoadProgressAsync();
    Task SaveProgressAsync();

    /// <summary>记录某道题做对了一次</summary>
    void RecordCorrectAnswer(string questionId);

    /// <summary>记录某道题做错了一次（同时加入错题本）</summary>
    void RecordWrongAnswer(string questionId);

    /// <summary>从错题本中斩杀移除某道题</summary>
    void RemoveFromWrongQuestions(string questionId);
}
