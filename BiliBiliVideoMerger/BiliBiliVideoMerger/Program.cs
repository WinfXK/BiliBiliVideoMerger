using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BiliBiliVideoMerger
{
    /// <summary>
    /// 用于反序列化 entry.json 文件，以获取视频标题
    /// 我们只需要 title 和 page_data.part 这两个字段
    /// </summary>
    public class BiliEntry
    {
        // 视频合集的标题
        public string title { get; set; }
        // 单个视频的信息
        public PageData page_data { get; set; }
    }

    /// <summary>
    /// 用于反序列化 page_data 部分
    /// </summary>
    public class PageData
    {
        // 单个视频的分P标题
        public string part { get; set; }
    }


    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "BiliBili 缓存视频合并工具 v5.0 by Winfxk";
            Console.WriteLine("=================================================");
            Console.WriteLine("    BiliBili 缓存视频合并工具");
            Console.WriteLine("=================================================");
            Console.WriteLine();

            // 1. 检查 ffmpeg.exe 是否存在于程序目录
            string ffmpegPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
            if (!File.Exists(ffmpegPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[错误] 必要组件 ffmpeg.exe 未找到！");
                Console.WriteLine($"请下载 FFmpeg，并将其中的 ffmpeg.exe 文件放置到本程序所在的目录下：");
                Console.WriteLine(AppContext.BaseDirectory);
                Console.ResetColor();
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
                return;
            }

            // 2. 获取用户输入的B站缓存文件夹路径
            Console.WriteLine("请将B站手机版的缓存文件夹 'download' 拖到这里，然后按 Enter 键：");
            string sourceDirectory = Console.ReadLine().Trim('\"'); // 处理拖拽进来路径带引号的情况

            // 3. 验证路径有效性
            if (string.IsNullOrEmpty(sourceDirectory) || !Directory.Exists(sourceDirectory))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[错误] 输入的路径无效或文件夹不存在！");
                Console.ResetColor();
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
                return;
            }

            // 4. [路径修改] 创建用于存放输出视频的文件夹
            string parentDirectory = Directory.GetParent(sourceDirectory)?.FullName;
            if (string.IsNullOrEmpty(parentDirectory))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[错误] 无法获取 '{Path.GetFileName(sourceDirectory)}' 文件夹的上级目录！");
                Console.ResetColor();
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
                return;
            }
            string outputDirectory = Path.Combine(parentDirectory, "MergedVideos");
            Directory.CreateDirectory(outputDirectory);
            Console.WriteLine($"视频将被合并到这个文件夹下：{outputDirectory}");
            Console.WriteLine();

            // 5. 开始处理
            ProcessVideos(sourceDirectory, outputDirectory, ffmpegPath);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("所有任务已完成！");
            Console.ResetColor();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        /// <summary>
        /// 核心处理逻辑
        /// </summary>
        static void ProcessVideos(string sourceDir, string outputDir, string ffmpegPath)
        {
            var collectionFolders = Directory.GetDirectories(sourceDir);
            int successCount = 0;
            int failCount = 0;
            Console.WriteLine($"发现 {collectionFolders.Length} 个视频合集，开始处理...");
            Console.WriteLine("-------------------------------------------------");
            foreach (var collectionFolder in collectionFolders)
            {
                var videoFolders = Directory.GetDirectories(collectionFolder);
                foreach (var videoFolder in videoFolders)
                {
                    try
                    {
                        string entryJsonPath = Path.Combine(videoFolder, "entry.json");
                        if (!File.Exists(entryJsonPath)) continue;
                        string jsonContent = File.ReadAllText(entryJsonPath);
                        var entryData = JsonSerializer.Deserialize<BiliEntry>(jsonContent);
                        string videoTitle = entryData?.page_data?.part ?? entryData?.title ?? "未知视频";
                        Console.WriteLine($"[发现视频]: {videoTitle}");
                        string safeFileName = SanitizeFileName(videoTitle) + ".mp4";
                        string outputFilePath = Path.Combine(outputDir, safeFileName);
                        if (File.Exists(outputFilePath))
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"[跳过] 文件 '{safeFileName}' 已存在。");
                            Console.ResetColor();
                            Console.WriteLine("-------------------------------------------------");
                            continue;
                        }
                        var mediaFolders = Directory.GetDirectories(videoFolder);
                        if (mediaFolders.Length == 0) continue;
                        string mediaFolder = mediaFolders[0];
                        string videoPath = Path.Combine(mediaFolder, "video.m4s");
                        string audioPath = Path.Combine(mediaFolder, "audio.m4s");
                        if (File.Exists(videoPath) && File.Exists(audioPath))
                        {
                            Console.WriteLine($"  > 正在合并: {safeFileName}");
                            string arguments = $"-y -i \"{videoPath}\" -i \"{audioPath}\" -c:v copy -c:a copy \"{outputFilePath}\"";
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                FileName = ffmpegPath,
                                Arguments = arguments,
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                RedirectStandardError = true
                            };
                            using (Process process = Process.Start(startInfo))
                            {
                                string errorOutput = process.StandardError.ReadToEnd();
                                process.WaitForExit();
                                if (process.ExitCode == 0)
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine($"  > ✓ 合并成功!");
                                    Console.ResetColor();
                                    successCount++;
                                }
                                else
                                {
                                    failCount++;
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"  > X 合并失败!");
                                    Console.WriteLine($"  > FFmpeg 输出: {errorOutput}");
                                    Console.ResetColor();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"处理文件夹 {videoFolder} 时发生意外错误: {ex.Message}");
                        Console.ResetColor();
                    }
                    finally
                    {
                        Console.WriteLine("-------------------------------------------------");
                    }
                }
            }
            Console.WriteLine($"处理完毕！成功: {successCount} 个, 失败: {failCount} 个。");
        }

        /// <summary>
        /// 移除文件名中的非法字符
        /// </summary>
        public static string SanitizeFileName(string fileName)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars());
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", Regex.Escape(invalidChars));
            return Regex.Replace(fileName, invalidRegStr, "_");
        }
    }
}