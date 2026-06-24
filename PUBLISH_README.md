# QuizNative 发布指南

## 最终 Git 提交历史

```
94fdc0a chore: update package manifest with proper publisher identity
a144cfb feat: add HistoryPage with three-level drill-down navigation
e7abefc feat: add WrongQuestionsPage with wrong answer elimination flow
1faf281 feat: implement StandardPage with chapter selection and card-style quiz UI
9ab1fd9 feat: add MainWindow with NavigationView shell and MVVM ViewModels
9f7c981 chore: configure dependency injection container and app launch logic
e68b43d feat: add local state persistence service with AppData sandbox storage
25a263a feat: implement quiz data loading and parsing service
54940ba feat: add Question and UserProgress data models
7bc783e chore: scaffold project with .csproj, manifests and assets
```

---

## 方式一：免打包分发（推荐）

不需要 MSIX 签名证书，生成一个完整的文件夹，可复制给任何人运行。

在终端执行：

```bash
cd QuizNative\QuizNative
dotnet publish -c Release -p:Platform=x64 -p:WindowsPackageType=None -p:SelfContained=true -o E:\QuizNative_Dist
```

**参数说明**：
| 参数 | 含义 |
|------|------|
| `-c Release` | 发布优化版本 |
| `-p:Platform=x64` | 64 位目标 |
| `-p:WindowsPackageType=None` | 跳过 MSIX 打包，直接生成 exe |
| `-p:SelfContained=true` | 自带 .NET 运行时，目标机器无需安装 .NET |
| `-o E:\QuizNative_Dist` | 输出路径（可改成你想要的路径） |

发布完成后，把整个 `E:\QuizNative_Dist` 文件夹压缩成 zip 发给别人即可。

**注意**：免打包模式下 Mica 背景可能不生效，因为 WinUI 3 的 Mica 需要 MSIX 容器权限。这是微软的设计限制。

---

## 方式二：MSIX 安装包（Mica 完美支持）

如果你需要 Mica 云母材质正常工作，必须用 MSIX 打包。

### 第 1 步：生成自签名证书

以 **管理员身份** 打开 PowerShell，执行：

```powershell
$thumb = (New-SelfSignedCertificate -Type Custom `
  -Subject "CN=QuizNative" `
  -KeyUsage DigitalSignature `
  -FriendlyName "QuizNativeCert" `
  -CertStoreLocation "Cert:\CurrentUser\My").Thumbprint

Write-Host "证书指纹: $thumb"

# 导出证书文件（分发给用户时需要）
Export-Certificate -Cert "Cert:\CurrentUser\My\$thumb" -FilePath "QuizNative.cer"
```

记下输出的 `证书指纹`（一串字母数字）。

### 第 2 步：配置发布配置

打开 `Properties\PublishProfiles\win-x64.pubxml`，填入你的证书指纹：

```xml
<PackageCertificateThumbprint>你的证书指纹在这里</PackageCertificateThumbprint>
<AppxPackageSigningEnabled>true</AppxPackageSigningEnabled>
```

### 第 3 步：打包

```bash
cd QuizNative\QuizNative
dotnet publish -c Release -p:Platform=x64
```

打包成功后，MSIX 文件在：

```
QuizNative\QuizNative\AppPackages\QuizNative_1.0.0.0_x64.msix
```

### 第 4 步：安装

双击 `.msix` 文件 → 点击「安装」。

如果提示「无法验证发布者」，先安装证书：

在目标机器上以管理员身份运行：

```powershell
Import-Certificate -FilePath "QuizNative.cer" -CertStoreLocation "Cert:\LocalMachine\TrustedPeople"
```

然后再双击 `.msix` 安装。

---

## 方式三：Microsoft Store 上架

1. 注册微软合作伙伴中心账号（需 $19 一次性费用）
2. 使用方式二打包出 `.msix`
3. 上传到合作伙伴中心的「应用提交」页面
4. 填写描述、截图、隐私政策等
5. 提交审核（通常 1-3 天）

---

## 常见问题

### Q: `System.Security.Permissions` 错误

如果遇到：
```
error MSB4018: Could not load file or assembly 'System.Security.Permissions, Version=8.0.0.0'
```

这是 .NET 10 + Windows App SDK 2.2 的已知兼容性问题。解决方案：

**方案 A（推荐）**：使用方式一免打包发布

```bash
dotnet publish -c Release -p:Platform=x64 -p:WindowsPackageType=None -p:SelfContained=true
```

**方案 B**：手动打包 MSIX

1. 用方式一先发布出 unpkg 文件夹
2. 在该文件夹中添加 `AppxManifest.xml`
3. 使用 `makeappx.exe` 手动打包
4. 使用 `signtool.exe` 签名

具体步骤可参考微软官方文档：
https://learn.microsoft.com/windows/msix/package/manual-packaging

### Q: Mica 背景不生效

确认是否使用了 MSIX 打包方式。免打包模式下 Mica 需额外配置。

### Q: 如何更新版本

修改 `Package.appxmanifest` 中的 `Version`（如 `1.0.0.0` → `1.0.1.0`），重新打包即可。

---

## 项目文件结构

```
QuizNative/
└── QuizNative/
    ├── QuizNative.csproj          # 项目文件
    ├── App.xaml / App.xaml.cs     # 应用入口 + DI 容器
    ├── MainWindow.xaml/.cs        # NavigationView 外壳
    ├── Models/
    │   ├── Question.cs            # 题目模型 (record)
    │   └── UserProgress.cs        # 用户进度模型
    ├── Services/
    │   ├── IQuizDataService.cs    # 题库加载接口
    │   ├── QuizDataService.cs     # JSON 解析实现
    │   ├── ILocalStateService.cs  # 本地持久化接口
    │   └── LocalStateService.cs   # AppData 读写实现
    ├── ViewModels/
    │   ├── StandardViewModel.cs   # 刷题状态机
    │   ├── WrongBookViewModel.cs  # 错题本状态机
    │   └── HistoryViewModel.cs    # 历史统计
    ├── Views/
    │   ├── StandardPage.xaml/.cs          # 刷题页
    │   ├── WrongQuestionsPage.xaml/.cs    # 错题页
    │   ├── HistoryPage.xaml/.cs           # 履历容器
    │   ├── HistoryOverviewSubPage.xaml/.cs
    │   ├── HistoryChapterDetailsSubPage.xaml/.cs
    │   └── HistoryQuestionDetailSubPage.xaml/.cs
    ├── Assets/                    # 应用图标
    └── Properties/PublishProfiles/  # 发布配置
```

## 运行调试

```bash
cd QuizNative\QuizNative
dotnet run --configuration Debug
```

你的 `questions.json` 放在项目根目录（`QuizNative/` 文件夹下），程序启动时自动读取。
