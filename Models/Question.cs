namespace QuizNative.Models;

/// <summary>
/// 单道题目数据模型。
/// Id 格式为 "Chapter_Index"，如 "1_0" 表示第一章第0题，
/// 由 QuizDataService 在解析 JSON 时自动生成，保证全局唯一。
/// </summary>
public record Question
{
    /// <summary>自动生成的唯一标识，如 "1_0"（第一章第0题）</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>所属章节 ID，如 "1", "2"</summary>
    public string ChapterId { get; init; } = string.Empty;

    /// <summary>题目文本</summary>
    public string Topic { get; init; } = string.Empty;

    /// <summary>正确答案（通常为选项字母，如 "A"）</summary>
    public string Answer { get; init; } = string.Empty;

    /// <summary>选项列表，如 ["A. 选项一", "B. 选项二"]</summary>
    public List<string> Options { get; init; } = new();
}
