using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Scarlet.IO
{
    internal enum VerifyResult { NoMagicNumber, VerifyOkay, WrongMagicNumber }

    internal class IdentificationMatch
    {
        public Type Type { get; private set; }
        public uint Weight { get; private set; }

        public IdentificationMatch(Type type, uint weight)
        {
            Type = type;
            Weight = weight;
        }
    }

    public abstract class FileFormat
    {
        bool isLoaded;

        public bool IsLoaded { get { return isLoaded; } }

        protected FileFormat()
        {
            isLoaded = false;
        }

        /// <summary>
        /// Tries to automatically detect the format of the given file, and creates an instance of it
        /// </summary>
        /// <typeparam name="T">Base type of file (i.e. image, archive)</typeparam>
        /// <param name="filename">Name of file to open</param>
        /// <returns>Instance of file; null if no instance was created</returns>
        public static T FromFile<T>(string filename) where T : FileFormat
        {
            return FromFile<T>(filename, EndianBinaryReader.NativeEndianness);
        }

        /// <summary>
        /// Tries to automatically detect the format of the given file, and creates an instance of it
        /// </summary>
        /// <typeparam name="T">Base type of file (i.e. image, archive)</typeparam>
        /// <param name="filename">Name of file to open</param>
        /// <param name="endianness">Endianness of the file data</param>
        /// <returns>Instance of file; null if no instance was created</returns>
        public static T FromFile<T>(string filename, Endian endianness) where T : FileFormat
        {
            using (FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return FromFile<T>(fileStream, endianness);
            }
        }

        /// <summary>
        /// Tries to automatically detect the format of the given file, and creates an instance of it
        /// </summary>
        /// <typeparam name="T">Base type of file (i.e. image, archive)</typeparam>
        /// <param name="fileStream">File stream with data to use</param>
        /// <returns>Instance of file; null if no instance was created</returns>
        public static T FromFile<T>(FileStream fileStream) where T : FileFormat
        {
            return FromFile<T>(fileStream, EndianBinaryReader.NativeEndianness);
        }

        /// <summary>
        /// Tries to automatically detect the format of the given file, and creates an instance of it
        /// </summary>
        /// <typeparam name="T">Base type of file (i.e. image, archive)</typeparam>
        /// <param name="fileStream">File stream with data to use</param>
        /// <param name="endianness">Endianness of the file data</param>
        /// <returns>Instance of file; null if no instance was created</returns>
        public static T FromFile<T>(FileStream fileStream, Endian endianness) where T : FileFormat
        {
            // TODO: rework identification system?

            List<IdentificationMatch> matchedTypes = new List<IdentificationMatch>();

            EndianBinaryReader reader = new EndianBinaryReader(fileStream, endianness);
            {
                foreach (var assembly in AssemblyHelpers.GetNonSystemAssemblies())
                {
                    foreach (var type in assembly.GetExportedTypes().Where(x => x == typeof(T) || x.InheritsFrom(typeof(T))))
                    {
                        IdentificationMatch magicMatch = null, patternMatch = null;

                        var customAttribs = type.GetCustomAttributes(false);
                        bool requireMagicAndPattern = customAttribs.Any(x => x is MagicNumberAttribute) && customAttribs.Any(x => x is FilenamePatternAttribute);
                        var magicNumberAttrib = customAttribs.FirstOrDefault(x => x is MagicNumberAttribute);

                        var verifyMethod = type.GetMethod("VerifyMagicNumber", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                        if (verifyMethod == null) throw new NullReferenceException("Reflection error on method fetch for file verification");
                        VerifyResult verifyResult = ((VerifyResult)verifyMethod.Invoke(null, new object[] { reader, type }));

                        if (verifyResult == VerifyResult.VerifyOkay)
                        {
                            uint weight = int.MaxValue;
                            if (magicNumberAttrib != null) weight += (uint)(magicNumberAttrib as MagicNumberAttribute).MagicNumber.Length;
                            magicMatch = new IdentificationMatch(type, weight);
                        }
                        else if (verifyResult == VerifyResult.WrongMagicNumber)
                        {
                            continue;
                        }

                        foreach (var fnPatternAttrib in type.GetCustomAttributes(typeof(FilenamePatternAttribute), false))
                        {
                            string pattern = (fnPatternAttrib as FilenamePatternAttribute).Pattern;
                            Regex regEx = new Regex(pattern, RegexOptions.IgnoreCase);
                            if (regEx.IsMatch(fileStream.Name))
                                patternMatch = new IdentificationMatch(type, (uint)pattern.Length);
                        }

                        if (requireMagicAndPattern)
                        {
                            if (magicMatch != null && patternMatch != null)
                            {
                                matchedTypes.Add(magicMatch);
                                matchedTypes.Add(patternMatch);
                            }
                        }
                        else
                        {
                            if (magicMatch != null) matchedTypes.Add(magicMatch);
                            if (patternMatch != null) matchedTypes.Add(patternMatch);
                        }
                    }

                    if (matchedTypes.Count > 0)
                    {
                        T fileInstance = (T)Activator.CreateInstance(matchedTypes.OrderByDescending(x => x.Weight).FirstOrDefault().Type);
                        fileInstance.Open(reader);
                        return fileInstance;
                    }
                }
            }

            return default(T);
        }

        public void Open(string filename)
        {
            Open(filename, EndianBinaryReader.NativeEndianness);
        }

        public void Open(string filename, Endian endianness)
        {
            using (EndianBinaryReader reader = new EndianBinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), endianness))
            {
                Open(reader);
            }
        }

        public void Open(Stream stream)
        {
            Open(stream, EndianBinaryReader.NativeEndianness);
        }

        public void Open(Stream stream, Endian endianness)
        {
            using (EndianBinaryReader reader = new EndianBinaryReader(stream, endianness))
            {
                Open(reader);
            }
        }

        public void Open(EndianBinaryReader reader)
        {
            VerifyResult verifyResult = VerifyMagicNumber(reader, this.GetType());
            if (verifyResult != VerifyResult.WrongMagicNumber)
            {
                OnOpen(reader);
                isLoaded = true;
            }
            else
                throw new Exception("Invalid magic number");
        }

        protected abstract void OnOpen(EndianBinaryReader reader);

        internal static VerifyResult VerifyMagicNumber(EndianBinaryReader reader, Type type)
        {
            VerifyResult result = VerifyResult.NoMagicNumber;
            long lastPosition = reader.BaseStream.Position;

            foreach (MagicNumberAttribute magicNumberAttrib in type.GetCustomAttributes(typeof(MagicNumberAttribute), false))
            {
                reader.BaseStream.Seek(lastPosition + magicNumberAttrib.Position, SeekOrigin.Begin);
                if (reader.ReadBytes(magicNumberAttrib.MagicNumber.Length).SequenceEqual(magicNumberAttrib.MagicNumber))
                {
                    result = VerifyResult.VerifyOkay;
                    break;
                }
                else
                    result = VerifyResult.WrongMagicNumber;
            }

            reader.BaseStream.Position = lastPosition;
            return result;
        }

        // TODO: better place to move to? some IO helper class?
        internal static void CopyStream(Stream input, Stream output, int bytes)
        {
            byte[] buffer = new byte[32768];
            int read;
            while (bytes > 0 && (read = input.Read(buffer, 0, Math.Min(buffer.Length, bytes))) > 0)
            {
                output.Write(buffer, 0, read);
                bytes -= read;
            }
        }

        internal static string ReadNullTermString(Stream input)
        {
            StringBuilder builder = new StringBuilder();
            char read;
            while ((read = (char)input.ReadByte()) != 0) builder.Append(read);
            return builder.ToString();
        }
    }
}
