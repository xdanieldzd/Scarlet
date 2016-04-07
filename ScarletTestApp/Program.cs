using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;

using Scarlet.IO;
using Scarlet.Drawing;
using Scarlet.IO.ImageFormats;
using Scarlet.IO.ContainerFormats;
using Scarlet.IO.CompressionFormats;

namespace ScarletTestApp
{
    // TODO: replace horribly hacky mess of patchwork, hardcoded paths, etc., etc., with proper GXTConvert-/Tharsis-ish tool

    class Program
    {
        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
#if DEBUG
            Console.WriteLine("Running Scarlet demo/test functions, please wait...");
#else
            Console.WriteLine("Scarlet Image Conversion Demo App / 2016 by xdaniel");
            Console.WriteLine("WIP build for Ehm2k - Not for distribution nor 'production use'");
            Console.WriteLine("(Because it's badly hacked together & pretty crappy)");
            Console.WriteLine();
#endif
#if !DEBUG
            try
#endif
            {
#if DEBUG
                //TestDXTBugs();
                //TestArchiveNISPACK();
                //TestArchiveNSAC();
                TestTID();
                //TestMultipleTID();
                //TestCapcomTEX();
                //TestMultipleCapcomTEX();
                //TestNMT();
                //TestFileDetection();
                //TestMultipleTX2();
                //TestMultipleTXP();
                //TestMultipleGXT();
                //TestSTEX();
                //TestTMX();
                //TestMultipleSHTXFS();
                //TestMultipleSHTX();
                //TestSingleIndexed();
                //TestSingleDirect();
                //TestAllFunctionOld();
#else
                args = Scarlet.IO.CommandLineTools.CreateArgs(Environment.CommandLine);
                if (args.Length == 2)
                {
                    Console.WriteLine("Input: {0}", args[1]);
                    Console.WriteLine();

                    DirectoryInfo dirInfo = new DirectoryInfo(args[1]);
                    if (dirInfo.Exists)
                    {
                        List<string> files = dirInfo.EnumerateFiles().Select(x => x.FullName).ToList();
                        DirectoryInfo outDirInfo = dirInfo.CreateSubdirectory("(converted)");
                        DoMultipleFiles(files, outDirInfo.FullName);
                    }

                    FileInfo fileInfo = new FileInfo(args[1]);
                    if (fileInfo.Exists)
                    {
                        List<string> files = new List<string>() { fileInfo.FullName };
                        DirectoryInfo outDirInfo = fileInfo.Directory.CreateSubdirectory("(converted)");
                        DoMultipleFiles(files, outDirInfo.FullName);
                    }
                }
                else
                    Console.WriteLine("Syntax: {0} <input>", AppDomain.CurrentDomain.FriendlyName);
#endif
                Console.WriteLine();
                Console.WriteLine("Done. Any key to exit.");
                Console.ReadKey();
            }
#if !DEBUG
            catch (Exception ex)
            {
                Console.WriteLine("EXCEPTION! {0}: {1}", ex.GetType().Name, ex.Message);
                Console.ReadKey();
            }
#endif
        }

        public static void TestDXTBugs()
        {
            Console.WriteLine("Test DXTx issue debugging stuff...");

            string dir = @"E:\[SSD User Data]\Downloads\_DXT-test_\";
            List<string> files = Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories).ToList();
            DoMultipleFiles(files, @"E:\Temp\scarlet\dxt\");
        }

        public static void TestArchiveNSAC()
        {
            Console.WriteLine("Test archive, type NSAC...");

            string arc = @"E:\[SSD User Data]\Downloads\disgaea4-vita\files\Data\DATA_EN\database_EN.dat";
            string output = @"E:\temp\scarlet\container\nsac\d4v-database";

            using (FileStream archiveStream = new FileStream(arc, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var instance = FileFormat.FromFile<ContainerFormat>(archiveStream);
                var elements = instance.GetElements(archiveStream);

                foreach (var element in elements)
                {
                    using (Stream elementStream = element.GetStream(archiveStream))
                    {
                        string elementOutput = Path.Combine(output, element.GetName());

                        using (FileStream fileStream = new FileStream(elementOutput, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                        {
                            elementStream.CopyTo(fileStream);
                        }
                    }
                }
            }
        }

        public static void TestArchiveNISPACK()
        {
            Console.WriteLine("Test archive, type NISPACK...");

            string arc = @"E:\[SSD User Data]\Downloads\disg-BLUS30727\BLUS30727\PS3_GAME\USRDIR\Data\START.dat";
            string output = @"E:\temp\scarlet\container\nispack\d4-start";

            arc = @"E:\[SSD User Data]\Downloads\disgaea3-vita\Data\START.dat";
            output = @"E:\temp\scarlet\container\nispack\d3v-start";

            //arc = @"E:\[SSD User Data]\Desktop\Misc Stuff\ZHP\USRDIR\SYSTEM.dat";
            //output = @"E:\temp\scarlet\container\nispack\zhp-system";

            using (FileStream archiveStream = new FileStream(arc, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var instance = FileFormat.FromFile<ContainerFormat>(archiveStream);
                var elements = instance.GetElements(archiveStream);

                foreach (var element in elements)
                {
                    using (Stream elementStream = element.GetStream(archiveStream))
                    {
                        string elementOutput = Path.Combine(output, element.GetName());

                        using (FileStream fileStream = new FileStream(elementOutput, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                        {
                            elementStream.CopyTo(fileStream);
                        }

                        var compressionInstance = FileFormat.FromFile<CompressionFormat>(elementOutput);
                        if (compressionInstance != null)
                        {
                            elementOutput = Path.Combine(output, Path.GetFileNameWithoutExtension(element.GetName()) + " (Decompressed)." + compressionInstance.GetNameOrExtension());

                            using (Stream decompressedStream = compressionInstance.GetDecompressedStream())
                            {
                                using (FileStream fileStream = new FileStream(elementOutput, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                                {
                                    decompressedStream.CopyTo(fileStream);
                                }
                            }
                        }

                        using (FileStream fileStream = new FileStream(elementOutput, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            fileStream.Seek(0, SeekOrigin.Begin);
                            var elementInstance = FileFormat.FromFile<ImageFormat>(fileStream);
                            if (elementInstance != null)
                            {
                                for (int i = 0; i < elementInstance.GetImageCount(); i++)
                                {
                                    string imageOutputDir = Path.GetDirectoryName(elementOutput);
                                    string imageOutputFile = (Path.GetFileNameWithoutExtension(elementOutput) + " (Texture " + i.ToString());

                                    Directory.CreateDirectory(imageOutputDir);

                                    if (elementInstance.GetPaletteCount() != 0)
                                    {
                                        for (int p = 0; p < elementInstance.GetPaletteCount(); p++)
                                        {
                                            Bitmap image = elementInstance.GetBitmap(i, p);
                                            image.Save(Path.Combine(imageOutputDir, (imageOutputFile + ", Palette " + p.ToString() + ").png")));
                                        }
                                    }
                                    else
                                    {
                                        Bitmap image = elementInstance.GetBitmap(i, 0);
                                        image.Save(Path.Combine(imageOutputDir, (imageOutputFile + ").png")));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void TestCapcomTEX()
        {
            Console.WriteLine("Test Capcom TEX...");

            var instance = FileFormat.FromFile<ImageFormat>(@"E:\[SSD User Data]\Downloads\DefaultCube_CM.tex");
            Bitmap image = instance.GetBitmap();
            if (image != null) image.Save(@"E:\temp\scarlet\specific\tex\ssf4-cube.png");
        }

        public static void TestMultipleCapcomTEX()
        {
            Console.WriteLine("Test multiple Capcom TEX...");

            string dir = @"E:\[SSD User Data]\Downloads\CapcomTEX\__test__\";
            List<string> files = Directory.EnumerateFiles(dir, "*.tex", SearchOption.AllDirectories).ToList();
            DoMultipleFiles(files, @"E:\Temp\scarlet\capcom-tex\");
        }

        public static void TestTID()
        {
            Console.WriteLine("Test TID...");

            TID instance = new TID();
            instance.Open(@"E:\[SSD User Data]\Downloads\neptunia-rb1-vita\__system\global\parts00.tid", Endian.LittleEndian);
            Bitmap image = instance.GetBitmap();
            if (image != null) image.Save(@"E:\temp\scarlet\specific\tid\VITA_parts00.png");

            instance.Open(@"E:\[SSD User Data]\Steam Library\steamapps\common\Neptunia Rebirth1\data\SYSTEM00000\global\parts00.tid", Endian.LittleEndian);
            image = instance.GetBitmap();
            if (image != null) image.Save(@"E:\temp\scarlet\specific\tid\PC_parts00.png");
        }

        public static void TestMultipleTID()
        {
            Console.WriteLine("Test multiple TID...");

            string dir = @"E:\[SSD User Data]\Downloads\neptunia-rb1-vita\data\GAME.cpk_unpacked\";
            List<string> files = Directory.EnumerateFiles(dir, "*.tid", SearchOption.AllDirectories).ToList();
            DoMultipleFiles(files, @"E:\Temp\scarlet\specific\tid\GAME.cpk_unpacked\", dir, true);
        }

        public static void TestNMT()
        {
            Console.WriteLine("Test NMT/NisMultiTexForm...");
            NMT instance = new NMT();
            instance.Open(@"E:\[SSD User Data]\Downloads\disgaea4-vita\extract\LOGO\logo.nmt", Endian.LittleEndian);
            Bitmap image = instance.GetBitmap();
            if (image != null) image.Save(@"E:\temp\scarlet\specific\logo.png");
        }

        public static void TestFileDetection()
        {
            Console.WriteLine("Test filetype auto-detection...");
            List<string> files = new List<string>()
            {
                @"E:\[SSD User Data]\Downloads\EO2U\dec_okay\stex\event\opening\ig_eve_title_logo.stex",
                @"E:\[SSD User Data]\Desktop\Misc Stuff\PB-PS2\start-dat\title.tx2",
                @"E:\[SSD User Data]\Downloads\GXT\GXT\CyberSleuth\DSDB.psp2.mvgl\images\ui_btlcommand2.pvr",
                @"E:\Translations\3DS Etrian Odyssey 4\Original & Dumps\original-eur\Mori4stex\Event\Opening\ig_eve_title.stex",
                @"E:\[SSD User Data]\Downloads\GXT\GXT\SAO\basevita\adv\bg\0aa00",
                @"E:\[SSD User Data]\Downloads\TMX\c_card00.tmx",
                @"E:\[SSD User Data]\Downloads\disgaea4-vita\extract\LOGO\logo.nmt",
                @"E:\[SSD User Data]\Downloads\neptunia-rb1-vita\__system\global\parts00.tid",
            };
            DoMultipleFiles(files, @"E:\Temp\scarlet\autodetect");

            files = Directory.EnumerateFiles(@"E:\[SSD User Data]\Desktop\Misc Stuff\ZHP\bustup", "*.txp").Concat(Directory.EnumerateFiles(@"E:\[SSD User Data]\Desktop\Misc Stuff\ZHP\system", "*.txp")).ToList();
            DoMultipleFiles(files, @"E:\Temp\scarlet\autodetect\zhp");
        }

        private static void DoMultipleFiles(List<string> files, string outPath, string basePath = "", bool ignoreExisting = false)
        {
            foreach (string file in files)
            {
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                string relPath = (basePath != string.Empty ? Path.GetDirectoryName(file.Replace(basePath, string.Empty)) : string.Empty).TrimStart('\\');
                string partOutput = Path.Combine(outPath, relPath, Path.GetFileNameWithoutExtension(file));

                Console.Write("File '{0}'...", Path.Combine(relPath, Path.GetFileName(file)));

                if (ignoreExisting && Directory.Exists(Path.GetDirectoryName(partOutput)) && Directory.EnumerateFiles(Path.GetDirectoryName(partOutput), Path.GetFileNameWithoutExtension(partOutput) + "*").Count() != 0)
                {
                    Console.WriteLine("skipping.");
                    continue;
                }

                var instance = FileFormat.FromFile<ImageFormat>(file);
                if (instance != null)
                {
                    for (int i = 0; i < instance.GetImageCount(); i++)
                    {
                        if (instance.GetPaletteCount() != 0)
                        {
                            for (int p = 0; p < instance.GetPaletteCount(); p++)
                            {
                                Bitmap image = instance.GetBitmap(i, p);
                                string output = (partOutput + " (Texture " + i.ToString() + ", Palette " + p.ToString() + ").png");
                                Directory.CreateDirectory(Path.GetDirectoryName(output));
                                image.Save(output);
                            }
                        }
                        else
                        {
                            Bitmap image = instance.GetBitmap(i, 0);
                            string output = (partOutput + " (Texture " + i.ToString() + ").png");
                            Directory.CreateDirectory(Path.GetDirectoryName(output));
                            image.Save(output);
                        }
                    }
                }
                sw.Stop();

                Console.WriteLine("done in {0}.", sw.Elapsed);
            }
        }

        public static void TestMultipleTX2()
        {
            Console.WriteLine("Test multiple TX2...");
            foreach (FileInfo fi in new DirectoryInfo(@"E:\[SSD User Data]\Desktop\Misc Stuff\PB-PS2\start-dat").EnumerateFiles("*.tx2"))
            {
                TX2 instance = new TX2();
                instance.Open(fi.FullName);

                for (int i = 0; i < instance.GetPaletteCount(); i++)
                {
                    Bitmap image = instance.GetBitmap(0, i);
                    string output = (@"E:\temp\scarlet\tx2\" + Path.GetFileNameWithoutExtension(fi.FullName) + " (Palette " + i.ToString() + ").png");
                    image.Save(output);
                }
            }
        }

        public static void TestMultipleTXP()
        {
            Console.WriteLine("Test multiple TXP...");
            foreach (FileInfo fi in new DirectoryInfo(@"E:\[SSD User Data]\Desktop\Misc Stuff\ZHP").EnumerateFiles("*.txp"))
            {
                TXP instance = new TXP();
                instance.Open(fi.FullName);

                for (int i = 0; i < instance.GetPaletteCount(); i++)
                {
                    Bitmap image = instance.GetBitmap(0, i);
                    string output = (@"E:\temp\scarlet\txp\" + Path.GetFileNameWithoutExtension(fi.FullName) + " (Palette " + i.ToString() + ").png");
                    image.Save(output);
                }
            }
        }

        public static void TestMultipleGXT()
        {
            Console.WriteLine("Test multiple GXT...");
            foreach (FileInfo fi in new DirectoryInfo(@"E:\[SSD User Data]\Downloads\GXT\__test__").EnumerateFiles("*.*"))
            {
                GXT instance = new GXT();
                instance.Open(fi.FullName, Endian.LittleEndian);

                for (int i = 0; i < instance.GetImageCount(); i++)
                {
                    Bitmap image = instance.GetBitmap(i, 0);
                    string output = (@"E:\temp\scarlet\gxt\" + Path.GetFileNameWithoutExtension(fi.FullName) + " (Texture " + i.ToString() + ").png");
                    image.Save(output);
                }

                if (instance.BUVChunk != null)
                {
                    List<Bitmap> buvImages = instance.GetBUVBitmaps().ToList();
                    for (int i = 0; i < buvImages.Count; i++)
                    {
                        string output = (@"E:\temp\scarlet\gxt\" + Path.GetFileNameWithoutExtension(fi.FullName) + " (Block " + i.ToString() + ").png");
                        buvImages[i].Save(output);
                    }
                }
            }
        }

        public static void TestSTEX()
        {
            Console.WriteLine("Test STEX...");
            STEX instance = new STEX();
            instance.Open(@"E:\[SSD User Data]\Downloads\EOIV\romfs\Mori4stex\Battle\CharaData\ig_bat_cha01_08.stex", Endian.LittleEndian);
            Bitmap image = instance.GetBitmap();
            if (image != null) image.Save(@"E:\temp\scarlet\specific\ig_bat_cha01_08.png");
        }

        public static void TestTMX()
        {
            Console.WriteLine("Test TMX...");
            TMX instance = new TMX();
            instance.Open(@"E:\[SSD User Data]\Downloads\_tmx-test\c_k01_a.tmx", Endian.LittleEndian);
            Bitmap image = instance.GetBitmap();
            if (image != null) image.Save(@"E:\temp\scarlet\specific\c_k01_a.png");
        }

        public static void TestMultipleSHTXFS()
        {
            Console.WriteLine("Test multiple SHTXFS...");
            foreach (FileInfo fi in new DirectoryInfo(@"E:\[SSD User Data]\Downloads\DRAE\__test2__").EnumerateFiles("*.btx"))
            {
                string output = Path.ChangeExtension(fi.FullName, ".png");

                SHTXFS instance = new SHTXFS();
                instance.Open(fi.FullName, Endian.LittleEndian);
                Bitmap image = instance.GetBitmap();
                if (image != null) image.Save(output);
            }
        }

        public static void TestMultipleSHTX()
        {
            Console.WriteLine("Test multiple SHTX...");
            foreach (FileInfo fi in new DirectoryInfo(@"E:\[SSD User Data]\Downloads\DRAE\__test3__").EnumerateFiles("*.btx"))
            {
                string output = Path.ChangeExtension(fi.FullName, ".png");

                SHTX instance = new SHTX();
                instance.Open(fi.FullName, Endian.LittleEndian);
                Bitmap image = instance.GetBitmap();
                if (image != null) image.Save(output);
            }
        }

        public static void TestSingleIndexed()
        {
            Console.WriteLine("Test single indexed (manual)...");
            ImageBinary image = new ImageBinary();
            image.Width = 1024;
            image.Height = 1024;
            image.InputPixelFormat = PixelDataFormat.FormatIndexed8;
            image.InputPaletteFormat = PixelDataFormat.FormatAbgr8888;
            image.InputEndianness = Endian.LittleEndian;

            using (FileStream stream = new FileStream(@"E:\[SSD User Data]\Downloads\DRAE\SHTXFS\bustup_00_01.btx", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                BinaryReader reader = new BinaryReader(stream);

                stream.Seek(0xC, SeekOrigin.Begin);
                image.AddInputPalette(reader.ReadBytes(0x400));

                stream.Seek(0x40C, SeekOrigin.Begin);
                image.AddInputPixels(reader.ReadBytes(0x100000));
            }

            image.GetBitmap().Save(@"E:\temp\scarlet\bustup_00_01.png");
        }

        public static void TestSingleDirect()
        {
            Console.WriteLine("Test single direct (manual)...");
            ImageBinary image = new ImageBinary();
            image.Width = 384;
            image.Height = 216;
            image.InputPixelFormat = PixelDataFormat.FormatArgb1555;
            image.InputEndianness = Endian.BigEndian;
            image.OutputFormat = PixelDataFormat.FormatArgb1555 | PixelDataFormat.FilterOrderedDither;
            image.OutputEndianness = Endian.LittleEndian;

            using (FileStream stream = new FileStream(@"E:\[SSD User Data]\Downloads\disg-BLUS30727\START\item.txf", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                BinaryReader reader = new BinaryReader(stream);
                stream.Seek(0x10, SeekOrigin.Begin);
                image.AddInputPixels(reader.ReadBytes(0x28800));
            }

            image.GetBitmap().Save(@"E:\temp\scarlet\item.png");
        }

        public static void TestAllFunctionOld()
        {
            Console.WriteLine("Test various (manual; OLD)...");

            FileStream stream = null;
            ImageBinary image = null;

            PixelDataFormat outputFormat = PixelDataFormat.FormatArgb1555;
            //outputFormat |= PixelDataFormat.FilterOrderedDither;

            using (stream = new FileStream(@"E:\[SSD User Data]\Desktop\l4-test.bin", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                image = new ImageBinary(16, 32, PixelDataFormat.FormatLuminance4, Endian.BigEndian, stream, 0x0, 0x100);
                image.OutputFormat = outputFormat;
                image.GetBitmap().Save(@"E:\temp\scarlet\l4.png");
            }

            using (stream = new FileStream(@"E:\[SSD User Data]\Downloads\disg-BLUS30727\START\item.txf", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                image = new ImageBinary(384, 216, PixelDataFormat.FormatArgb1555, Endian.BigEndian, stream, 0x10, 0x28800);
                image.OutputFormat = outputFormat;
                image.GetBitmap().Save(@"E:\temp\scarlet\item.png");
            }

            using (stream = new FileStream(@"E:\[SSD User Data]\Downloads\EOIV\romfs\Mori4stex\Camp\Chara_Cam\ig_cus_cha01_01.stex", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                image = new ImageBinary(128, 32, PixelDataFormat.FormatRgb565 | PixelDataFormat.PostProcessUntile_3DS, Endian.LittleEndian, stream, 0x80, 0x2000);
                image.OutputFormat = outputFormat;
                image.GetBitmap().Save(@"E:\temp\scarlet\ig_cus_cha01_01.png");
            }

            using (stream = new FileStream(@"E:\[SSD User Data]\Downloads\GXT\__test__\criware960x544sw.gxt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                image = new ImageBinary(1024, 1024, PixelDataFormat.FormatArgb8888 | PixelDataFormat.PostProcessUnswizzle_Vita, Endian.LittleEndian, stream, 0x40, 0x400000);
                image.OutputFormat = outputFormat;
                image.GetBitmap().Save(@"E:\temp\scarlet\criware960x544sw.png");
            }

            using (stream = new FileStream(@"E:\[SSD User Data]\Downloads\GXT\__test__\ui_mapsel_thumb_01.pvr", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                image = new ImageBinary(256, 128, PixelDataFormat.FormatPVRT4_Vita, Endian.LittleEndian, stream, 0x40, 0x5570);
                image.OutputFormat = outputFormat;
                image.GetBitmap().Save(@"E:\temp\scarlet\ui_mapsel_thumb_01.png");
            }

            using (stream = new FileStream(@"E:\[SSD User Data]\Downloads\GXT\__test__\ui_title_logo_bg.pvr", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                image = new ImageBinary(1024, 1024, PixelDataFormat.FormatDXT5 | PixelDataFormat.PostProcessUnswizzle_Vita, Endian.LittleEndian, stream, 0x40, 0x155570);
                image.OutputFormat = outputFormat;
                image.GetBitmap().Save(@"E:\temp\scarlet\ui_title_logo_bg.png");
            }

            using (stream = new FileStream(@"E:\[SSD User Data]\Downloads\EOIV\romfs\Mori4stex\Debug\ig_deb_title.stex", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                image = new ImageBinary(512, 256, PixelDataFormat.FormatETC1A4_3DS, Endian.LittleEndian, stream, 0x80, 0x20000);
                image.OutputFormat = outputFormat;
                image.GetBitmap().Save(@"E:\temp\scarlet\ig_deb_title.png");
            }
        }
    }
}
