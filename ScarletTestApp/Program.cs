using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Reflection;

using Scarlet.IO;
using Scarlet.Drawing;
using Scarlet.IO.ImageFormats;
using Scarlet.IO.ContainerFormats;
using Scarlet.IO.CompressionFormats;

namespace ScarletTestApp
{
    class Program
    {
        static char[] directorySeparators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        static string defaultOutputDir = "(converted)";

        static int indent = 0, baseIndent = 0;
        static bool keepFiles = false;
        static DirectoryInfo globalOutputDir = null;

        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                var name = (assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false).FirstOrDefault() as AssemblyProductAttribute).Product;
                var version = new Version((assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false).FirstOrDefault() as AssemblyFileVersionAttribute).Version);
                var description = (assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false).FirstOrDefault() as AssemblyDescriptionAttribute).Description;
                var copyright = (assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false).FirstOrDefault() as AssemblyCopyrightAttribute).Copyright;

                IndentWriteLine("{0} v{1}.{2} - {3}", name, version.Major, version.Minor, description);
                IndentWriteLine("{0}", copyright);
                IndentWriteLine();
                IndentWriteLine("Scarlet library information:");
                indent++;
                foreach (AssemblyName referencedAssembly in Assembly.GetExecutingAssembly().GetReferencedAssemblies().Where(x => x.Name.StartsWith("Scarlet")).OrderBy(x => x.Name))
                    IndentWriteLine("{0} v{1}", referencedAssembly.Name, referencedAssembly.Version);
                indent--;
                IndentWriteLine();

                args = CommandLineTools.CreateArgs(Environment.CommandLine);

                if (args.Length < 2)
                    throw new CommandLineArgsException("<input ...> [--keep | --output <directory>]");

                List<DirectoryInfo> inputDirs = new List<DirectoryInfo>();
                List<FileInfo> inputFiles = new List<FileInfo>();

                for (int i = 1; i < args.Length; i++)
                {
                    DirectoryInfo directory = new DirectoryInfo(args[i]);
                    if (directory.Exists)
                    {
                        IEnumerable<FileInfo> files = directory.EnumerateFiles("*", SearchOption.AllDirectories).Where(x => x.Extension != ".png");
                        IndentWriteLine("Adding directory '{0}', {1} file{2} found...", directory.Name, files.Count(), (files.Count() != 1 ? "s" : string.Empty));
                        inputDirs.Add(directory);
                        continue;
                    }

                    FileInfo file = new FileInfo(args[i]);
                    if (file.Exists)
                    {
                        IndentWriteLine("Adding file '{0}'...", file.Name);
                        inputFiles.Add(file);
                        continue;
                    }

                    if (args[i].StartsWith("-"))
                    {
                        switch (args[i].TrimStart('-'))
                        {
                            case "k":
                            case "keep":
                                keepFiles = true;
                                break;

                            case "o":
                            case "output":
                                globalOutputDir = new DirectoryInfo(args[++i]);
                                break;

                            default:
                                IndentWriteLine("Unknown argument '{0}'.", args[i]);
                                break;
                        }
                        continue;
                    }

                    IndentWriteLine("File or directory '{0}' not found.", args[i]);
                }

                if (inputDirs.Count > 0)
                {
                    foreach (DirectoryInfo inputDir in inputDirs)
                    {
                        IndentWriteLine();
                        IndentWriteLine("Parsing directory '{0}'...", inputDir.Name);
                        baseIndent = indent++;

                        DirectoryInfo outputDir = (globalOutputDir != null ? globalOutputDir : new DirectoryInfo(inputDir.FullName + " " + defaultOutputDir));
                        foreach (FileInfo inputFile in inputDir.EnumerateFiles("*", SearchOption.AllDirectories).Where(x => x.Extension != ".png" && !IsSubdirectory(x.Directory, outputDir)))
                            ProcessInputFile(inputFile, inputDir, outputDir);

                        indent--;
                    }
                }

                if (inputFiles.Count > 0)
                {
                    IndentWriteLine();
                    IndentWriteLine("Parsing files...");
                    baseIndent = indent++;

                    foreach (FileInfo inputFile in inputFiles)
                    {
                        DirectoryInfo outputDir = (globalOutputDir != null ? globalOutputDir : inputFile.Directory);
                        ProcessInputFile(inputFile, inputFile.Directory, outputDir);
                    }
                }
            }
#if !DEBUG
            catch (CommandLineArgsException claEx)
            {
                IndentWriteLine("Invalid arguments; expected: {0}.", claEx.ExpectedArgs);
            }
            catch (Exception ex)
            {
                IndentWriteLine("Exception occured: {0}.", ex.Message);
            }
#endif
            finally
            {
                stopwatch.Stop();

                indent = baseIndent = 0;

                IndentWriteLine();
                IndentWriteLine("Operation completed in {0}.", GetReadableTimespan(stopwatch.Elapsed));
                IndentWriteLine();
                IndentWriteLine("Press any key to exit.");
                Console.ReadKey();
            }
        }

        private static void ProcessInputFile(FileInfo inputFile, DirectoryInfo inputDir, DirectoryInfo outputDir)
        {
            try
            {
                if (!outputDir.Exists) Directory.CreateDirectory(outputDir.FullName);

                string displayPath = inputFile.FullName.Replace(inputDir.FullName, string.Empty).TrimStart(directorySeparators);
                IndentWrite("File '{0}'... ", displayPath);
                baseIndent = indent++;

                string relativeDirectory = inputFile.DirectoryName.TrimEnd(directorySeparators).Replace(inputDir.FullName.TrimEnd(directorySeparators), string.Empty).TrimStart(directorySeparators);

                if (keepFiles)
                {
                    string existenceCheckPath = Path.Combine(outputDir.FullName, relativeDirectory);
                    string existenceCheckPattern = Path.GetFileNameWithoutExtension(inputFile.Name) + "*";
                    if (Directory.Exists(existenceCheckPath) && Directory.EnumerateFiles(existenceCheckPath, existenceCheckPattern).Any())
                    {
                        Console.WriteLine("already exists.");
                        return;
                    }
                }

                using (FileStream inputStream = new FileStream(inputFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var instance = FileFormat.FromFile<FileFormat>(inputStream);
                    if (instance != null)
                    {
                        if (instance is ImageFormat)
                        {
                            var imageInstance = (instance as ImageFormat);

                            int imageCount = imageInstance.GetImageCount();
                            int paletteCount = imageInstance.GetPaletteCount();
                            int blockCount = ((imageInstance is GXT && (imageInstance as GXT).BUVChunk != null) ? (imageInstance as GXT).BUVChunk.Entries.Length : -1);

                            if (blockCount != -1)
                                Console.WriteLine("{0} image{1}, {2} palette{3}, {4} block{5} found.", imageCount, (imageCount != 1 ? "s" : string.Empty), paletteCount, (paletteCount != 1 ? "s" : string.Empty), blockCount, (blockCount != 1 ? "s" : string.Empty));
                            else
                                Console.WriteLine("{0} image{1}, {2} palette{3} found.", imageCount, (imageCount != 1 ? "s" : string.Empty), paletteCount, (paletteCount != 1 ? "s" : string.Empty));

                            for (int i = 0; i < imageCount; i++)
                            {
                                string imageName = imageInstance.GetImageName(i);

                                string outputFilename;
                                FileInfo outputFile;

                                if (paletteCount < 2)
                                {
                                    Bitmap image = imageInstance.GetBitmap(i, 0);
                                    if (imageName == null)
                                    {
                                        outputFilename = string.Format("{0} (Image {1}).png", Path.GetFileNameWithoutExtension(inputFile.Name), i);
                                        outputFile = new FileInfo(Path.Combine(outputDir.FullName, relativeDirectory, outputFilename));
                                    }
                                    else
                                    {
                                        outputFilename = string.Format("{0}.png", Path.GetFileNameWithoutExtension(imageName));
                                        outputFile = new FileInfo(Path.Combine(outputDir.FullName, relativeDirectory, Path.GetFileNameWithoutExtension(inputFile.Name), outputFilename));
                                    }

                                    Directory.CreateDirectory(outputFile.Directory.FullName);
                                    image.Save(outputFile.FullName, System.Drawing.Imaging.ImageFormat.Png);
                                }
                                else
                                {
                                    for (int p = 0; p < paletteCount; p++)
                                    {
                                        Bitmap image = imageInstance.GetBitmap(i, p);

                                        if (imageName == null)
                                        {
                                            outputFilename = string.Format("{0} (Image {1}, Palette {2}).png", Path.GetFileNameWithoutExtension(inputFile.Name), i, p);
                                            outputFile = new FileInfo(Path.Combine(outputDir.FullName, relativeDirectory, outputFilename));
                                        }
                                        else
                                        {
                                            outputFilename = string.Format("{0} (Palette {1}).png", Path.GetFileNameWithoutExtension(imageName), p);
                                            outputFile = new FileInfo(Path.Combine(outputDir.FullName, relativeDirectory, Path.GetFileNameWithoutExtension(inputFile.Name), outputFilename));
                                        }

                                        Directory.CreateDirectory(outputFile.Directory.FullName);
                                        image.Save(outputFile.FullName, System.Drawing.Imaging.ImageFormat.Png);
                                    }
                                }
                            }

                            if (imageInstance is GXT && (imageInstance as GXT).BUVChunk != null)
                            {
                                var gxtInstance = (imageInstance as GXT);

                                List<Bitmap> buvImages = gxtInstance.GetBUVBitmaps().ToList();
                                for (int b = 0; b < buvImages.Count; b++)
                                {
                                    Bitmap image = buvImages[b];
                                    string outputFilename = string.Format("{0} (Block {1}).png", Path.GetFileNameWithoutExtension(inputFile.Name), b);
                                    FileInfo outputFile = new FileInfo(Path.Combine(outputDir.FullName, relativeDirectory, outputFilename));

                                    Directory.CreateDirectory(outputFile.Directory.FullName);
                                    image.Save(outputFile.FullName, System.Drawing.Imaging.ImageFormat.Png);
                                }
                            }
                        }
                        else if (instance is ContainerFormat)
                        {
                            var containerInstance = (instance as ContainerFormat);

                            int elementCount = containerInstance.GetElementCount();
                            Console.WriteLine("{0} element{1} found.", elementCount, (elementCount != 1 ? "s" : string.Empty));

                            foreach (var element in containerInstance.GetElements(inputStream))
                            {
                                string outputFilename = element.GetName();
                                FileInfo outputFile = new FileInfo(Path.Combine(outputDir.FullName, relativeDirectory, Path.GetFileNameWithoutExtension(inputFile.Name), outputFilename));

                                IndentWrite("File '{0}'... ", outputFilename);

                                Directory.CreateDirectory(outputFile.Directory.FullName);
                                using (FileStream outputStream = new FileStream(outputFile.FullName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                                {
                                    using (Stream elementStream = element.GetStream(inputStream))
                                    {
                                        elementStream.CopyTo(outputStream);
                                    }

                                    // TODO: clean way to auto-decompress files, convert images, etc. inside containers, if applicable?
                                    //       otherwise, rescan and process the output directory/file(s) after all inputs are finished?
                                }

                                Console.WriteLine("extracted.");
                            }
                        }
                        else if (instance is CompressionFormat)
                        {
                            var compressedInstance = (instance as CompressionFormat);

                            Console.WriteLine("decompressed {0}.", compressedInstance.GetType().Name);

                            // TODO: less naive way of determining target filename; see also CompressionFormat class in Scarlet.IO.CompressionFormats
                            bool isFullName = compressedInstance.GetNameOrExtension().Contains('.');
                            string outputFilename = (isFullName ? compressedInstance.GetNameOrExtension() : Path.GetFileNameWithoutExtension(inputFile.Name) + "." + compressedInstance.GetNameOrExtension());
                            FileInfo outputFile = new FileInfo(Path.Combine(outputDir.FullName, relativeDirectory, outputFilename));

                            using (FileStream outputStream = new FileStream(outputFile.FullName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                            {
                                using (Stream decompressedStream = compressedInstance.GetDecompressedStream())
                                {
                                    decompressedStream.CopyTo(outputStream);
                                }
                            }
                        }
                        else
                            Console.WriteLine("unhandled file.");
                    }
                    else
                        Console.WriteLine("unsupported file.");
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                IndentWriteLine("Exception occured: {0}.", ex.Message);
            }
#endif
            finally
            {
                indent = baseIndent;
            }
        }

        private static void IndentWrite(string format = "", params object[] param)
        {
            Console.Write(format.Insert(0, new string(' ', indent)), param);
        }

        private static void IndentWriteLine(string format = "", params object[] param)
        {
            Console.WriteLine(format.Insert(0, new string(' ', indent)), param);
        }

        /* Slightly modified from https://stackoverflow.com/a/4423615 */
        private static string GetReadableTimespan(TimeSpan span)
        {
            string formatted = string.Format("{0}{1}{2}{3}{4}",
            span.Duration().Days > 0 ? string.Format("{0:0} day{1}, ", span.Days, span.Days == 1 ? string.Empty : "s") : string.Empty,
            span.Duration().Hours > 0 ? string.Format("{0:0} hour{1}, ", span.Hours, span.Hours == 1 ? string.Empty : "s") : string.Empty,
            span.Duration().Minutes > 0 ? string.Format("{0:0} minute{1}, ", span.Minutes, span.Minutes == 1 ? string.Empty : "s") : string.Empty,
            span.Duration().Seconds > 0 ? string.Format("{0:0} second{1}, ", span.Seconds, span.Seconds == 1 ? string.Empty : "s") : string.Empty,
            span.Duration().Milliseconds > 0 ? string.Format("{0:0} millisecond{1}", span.Milliseconds, span.Milliseconds == 1 ? string.Empty : "s") : string.Empty);
            if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);
            if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";
            return formatted;
        }

        private static bool IsSubdirectory(DirectoryInfo childDir, DirectoryInfo parentDir)
        {
            if (parentDir.FullName == childDir.FullName) return true;

            DirectoryInfo child = childDir.Parent;
            while (child != null)
            {
                if (child.FullName == parentDir.FullName) return true;
                child = child.Parent;
            }

            return false;
        }
    }
}
