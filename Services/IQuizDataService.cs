using QuizNative.Models;

namespace QuizNative.Services;

public interface IQuizDataService
{
    Task LoadQuizAsync(string jsonContent);
    List<string> GetChapters();
    List<Question> GetQuestionsByChapter(string chapterId);
    Question? GetQuestionById(string id);
}
