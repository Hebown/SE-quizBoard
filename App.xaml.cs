using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using QuizNative.Services;
using QuizNative.ViewModels;
using System;
using System.IO;
using System.Diagnostics;

namespace QuizNative;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public Window? MainWindow { get; private set; }

    public App()
    {
        this.InitializeComponent();

        // 配置依赖注入容器
        var services = new ServiceCollection();

        // 注入基础设施服务（单例模式）
        services.AddSingleton<IQuizDataService, QuizDataService>();
        services.AddSingleton<ILocalStateService, LocalStateService>();

        // 注入 ViewModel（瞬时模式：每次请求创建新实例）
        services.AddTransient<StandardViewModel>();
        services.AddTransient<WrongBookViewModel>();
        services.AddTransient<HistoryViewModel>();

        Services = services.BuildServiceProvider();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // 预先加载本地用户存档
        var localState = Services.GetRequiredService<ILocalStateService>();
        await localState.LoadProgressAsync();

        // 预先加载题库数据：从根目录读取 questions.json
        var quizService = Services.GetRequiredService<IQuizDataService>();
        await LoadQuizDataAsync(quizService);

        MainWindow = new MainWindow();
        MainWindow.Activate();
    }

    /// <summary>
    /// 定位并加载根目录的 questions.json 题库文件
    /// </summary>
    private static async Task LoadQuizDataAsync(IQuizDataService quizService)
    {
        try
        {
            // 推测路径：当前工作目录通常是项目目录 QuizNative/QuizNative，
            // 需要向上两级到达根目录 (d:\vscode_files\major_course\se\)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // 尝试多个可能的路径
            string[] candidates =
            {
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "questions.json")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "questions.json")),
                Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", "questions.json")),
                Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "questions.json")),
            };

            string? foundPath = null;
            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    foundPath = path;
                    break;
                }
            }

            if (foundPath != null)
            {
                string json = await File.ReadAllTextAsync(foundPath);
                await quizService.LoadQuizAsync(json);

                // 输出加载统计信息到调试控制台
                var chapters = quizService.GetChapters();
                int totalQuestions = 0;
                foreach (var ch in chapters)
                {
                    int count = quizService.GetQuestionsByChapter(ch).Count;
                    totalQuestions += count;
                    Debug.WriteLine($"  章节 {ch}: {count} 题");
                }
                Debug.WriteLine($"✅ 题库加载完成: 共 {chapters.Count} 个章节, {totalQuestions} 道题目");
            }
            else
            {
                Debug.WriteLine("⚠️ 未找到 questions.json 文件，请确认文件路径");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ 加载题库失败: {ex.Message}");
        }
    }
}
