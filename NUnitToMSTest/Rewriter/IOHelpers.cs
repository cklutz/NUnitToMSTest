using System;
using System.IO;

namespace NUnitToMSTest.Rewriter
{
    internal class IOHelpers
    {
        public static void BackupFile(string fileName, int maxHistory)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));
            if (maxHistory < 0)
                throw new ArgumentException("Positive value expected", nameof(maxHistory));

            if (File.Exists(fileName))
            {
                for (int i = maxHistory - 1; i > 0; i--)
                {
                    string fileOld = GetName(fileName, i);
                    string fileNew = GetName(fileName, i + 1);
                    if (File.Exists(fileOld))
                    {
                        MoveFile(fileOld, fileNew);
                    }
                }

                string backFile = GetName(fileName, 1);
                MoveFile(fileName, backFile);
            }

            string GetName(string name, int index)
            {
                string baseName = Path.GetFileNameWithoutExtension(name);
                string extension = Path.GetExtension(name);
                string dirName = Path.GetDirectoryName(name);

                return $"{Path.Combine(dirName, baseName)}.{index:000}{extension}";
            }

            void MoveFile(string source, string destination)
            {
                if (File.Exists(destination))
                {
                    try
                    {
                        File.Delete(destination);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        // Ignorieren.  Es kann sein, dass die Datei zwischen der
                        // Exists()-Abfrage und dem eigentlichen Delete() von jemand
                        // anderem gelöscht wurde.
                    }
                }

                File.Move(source, destination);
            }
        }
    }
}