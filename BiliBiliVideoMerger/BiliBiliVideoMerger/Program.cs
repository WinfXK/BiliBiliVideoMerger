/*
 * Copyright Notice
 * © [2024 - 2025] Winfxk. All rights reserved.
 * The software, its source code, and all related documentation are the intellectual property of Winfxk.
 * Any reproduction or distribution of this software or any part thereof must be clearly attributed to Winfxk and the original author.
 * Unauthorized copying, reproduction, or distribution without proper attribution is strictly prohibited.
 * For inquiries, support, or to request permission for use, please contact us at:
 * Email: admin@winfxk.cn
 * QQ: 2508543202
 * Visit our homepage for more information: http://Winfxk.cn
 *
 * --------- Create message ---------
 * Created by Visual Studio
 * Author： Winfxk
 * Web: http://winfxk.com
 * Created Date: 2025/12/20 09:52
 */

using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BiliBiliVideoMerger
{
    /// <summary>
    /// 用于反序列化 entry.json 文件，以获取视频标题
    /// </summary>
    public class BiliEntry
    {
        public string? title { get; set; }
        public PageData? page_data { get; set; }
    }

    /// <summary>
    /// 用于反序列化 page_data 部分
    /// </summary>
    public class PageData
    {
        public string? part { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "BiliBili 缓存视频合并工具 v5.2 - Winfxk";
            PrintHeader();

            // 1. 寻找 FFmpeg 路径
            string ffmpegPath = FindFFmpeg();
            if (string.IsNullOrEmpty(ffmpegPath))
            {
                PrintError("[错误] 未找到 ffmpeg.exe！");
                Console.WriteLine("请将 FFmpeg 放入程序所在目录，或者将其路径添加到系统环境变量 PATH 中。");
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
                return;
            }

            // 2. 获取源目录路径
            string sourceDirectory = "";
            if (args.Length > 0)
            {
                // 兼容用户直接把文件夹拖拽到 exe 图标上打开的情况
                sourceDirectory = args[0].Trim('\"');
            }
            else
            {
                Console.WriteLine("请拖入 B 站缓存文件夹 (例如手机端的 'download' 目录)，然后按 Enter 键：");
                sourceDirectory = Console.ReadLine()?.Trim('\"') ?? "";
            }

            // 3. 验证路径
            if (string.IsNullOrEmpty(sourceDirectory) || !Directory.Exists(sourceDirectory))
            {
                PrintError("[错误] 输入的路径无效或文件夹不存在！");
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
                return;
            }

            // 4. 确定输出目录 (优先在源目录上级创建，若无权限则在程序根目录创建)
            string? parentPath = Directory.GetParent(sourceDirectory)?.FullName;
            string outputDirectory = Path.Combine(parentPath ?? AppContext.BaseDirectory, "MergedVideos");

            try
            {
                Directory.CreateDirectory(outputDirectory);
            }
            catch (Exception ex)
            {
                PrintError($"[提示] 无法在源目录创建输出文件夹: {ex.Message}，将改用程序目录。");
                outputDirectory = Path.Combine(AppContext.BaseDirectory, "MergedVideos");
                Directory.CreateDirectory(outputDirectory);
            }

            Console.WriteLine($"\n[配置] 输出目录: {outputDirectory}");
            Console.WriteLine("[执行] 正在递归扫描所有视频缓存，请稍候...");
            Console.WriteLine("-------------------------------------------------");

            // 5. 执行批量合并
            ProcessVideos(sourceDirectory, outputDirectory, ffmpegPath);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[完成] 所有任务已处理。");
            Console.ResetColor();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        /// <summary>
        /// 递归处理所有包含 entry.json 的视频文件夹
        /// </summary>
        static void ProcessVideos(string sourceDir, string outputDir, string ffmpegPath)
        {
            // 采用递归搜索，自动兼容手机端和 PC 端的不同深度的层级结构
            string[] entryFiles = Directory.GetFiles(sourceDir, "entry.json", SearchOption.AllDirectories);
            int successCount = 0;
            int failCount = 0;
            int skipCount = 0;

            foreach (var jsonPath in entryFiles)
            {
                try
                {
                    string videoFolder = Path.GetDirectoryName(jsonPath)!;
                    string jsonContent = File.ReadAllText(jsonPath);
                    var entryData = JsonSerializer.Deserialize<BiliEntry>(jsonContent);

                    // 提取标题逻辑：优先使用分P标题 (part)，其次使用合集总标题 (title)
                    string rawTitle = entryData?.page_data?.part ?? entryData?.title ?? "未知视频_" + Path.GetFileName(videoFolder);
                    string safeFileName = SanitizeFileName(rawTitle) + ".mp4";
                    string outputFilePath = Path.Combine(outputDir, safeFileName);

                    // 跳过逻辑
                    if (File.Exists(outputFilePath))
                    {
                        Console.WriteLine($"[跳过] '{safeFileName}' 已存在。");
                        skipCount++;
                        continue;
                    }

                    // 自动搜索当前 entry.json 所在目录下的所有 m4s 文件
                    var m4sFiles = Directory.GetFiles(videoFolder, "*.m4s", SearchOption.AllDirectories);
                    string? videoPath = m4sFiles.FirstOrDefault(f => Path.GetFileName(f) == "video.m4s");
                    string? audioPath = m4sFiles.FirstOrDefault(f => Path.GetFileName(f) == "audio.m4s");

                    if (videoPath != null && audioPath != null)
                    {
                        Console.Write($"[合并] {safeFileName} ...");
                        if (ExecuteFFmpeg(ffmpegPath, videoPath, audioPath, outputFilePath))
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(" 成功!");
                            Console.ResetColor();
                            successCount++;
                        }
                        else
                        {
                            PrintError(" 失败!");
                            failCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    PrintError($"\n[异常] 处理缓存时发生错误: {ex.Message}");
                }
            }

            Console.WriteLine("\n-------------------------------------------------");
            Console.WriteLine($"统计: 成功: {successCount} | 失败: {failCount} | 跳过: {skipCount}");
        }

        /// <summary>
        /// 调用 FFmpeg 执行音视频流拷贝合并
        /// </summary>
        static bool ExecuteFFmpeg(string ffmpeg, string video, string audio, string output)
        {
            // -y: 覆盖输出; -c copy: 不重新编码(极速); -loglevel error: 只显示错误日志
            string arguments = $"-y -i \"{video}\" -i \"{audio}\" -c:v copy -c:a copy -loglevel error \"{output}\"";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = ffmpeg,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            using (Process? process = Process.Start(startInfo))
            {
                process?.WaitForExit();
                return process?.ExitCode == 0;
            }
        }

        /// <summary>
        /// 智能定位 FFmpeg
        /// </summary>
        static string FindFFmpeg()
        {
            // 1. 本地目录优先
            string localPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
            if (File.Exists(localPath)) return localPath;

            // 2. 检查环境变量
            try
            {
                using (Process? p = Process.Start(new ProcessStartInfo("where", "ffmpeg") { RedirectStandardOutput = true, CreateNoWindow = true }))
                {
                    string? result = p?.StandardOutput.ReadLine();
                    if (!string.IsNullOrEmpty(result) && File.Exists(result)) return result;
                }
            }
            catch { /* Ignore */ }

            return string.Empty;
        }

        /// <summary>
        /// 清理文件名中的非法字符
        /// </summary>
        static string SanitizeFileName(string fileName)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            return Regex.Replace(fileName, invalidRegStr, "_").Trim();
        }

        static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("=================================================");
            Console.WriteLine("     BiliBili 缓存批量合并工具 v5.2 (智能版)");
            Console.WriteLine("     作者: Winfxk | 网站: winfxk.com");
            Console.WriteLine("=================================================");
            Console.ResetColor();
            Console.WriteLine();
        }

        static void PrintError(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
            Console.ResetColor();
        }
    }
}