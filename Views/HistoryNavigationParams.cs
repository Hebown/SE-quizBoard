namespace QuizNative.Views;

/// <summary>
/// 层级2 — 章节明细页导航参数：携带章节ID
/// </summary>
public record ChapterDetailParam(string ChapterId);

/// <summary>
/// 层级3 — 单题履历页导航参数：携带题目ID
/// </summary>
public record QuestionDetailParam(string QuestionId);
