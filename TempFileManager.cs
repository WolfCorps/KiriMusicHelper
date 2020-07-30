using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiriMusicHelper
{
    class TempFileManager
    {
        public static TempFileManager Instance = new TempFileManager(Path.Combine(Directory.GetCurrentDirectory(), "temp"));

        private string TempDirectory;
        private List<string> tempFiles = new List<string>();

        private TempFileManager(string directory)
        {
            Directory.CreateDirectory(directory);
            TempDirectory = directory;
        }

        ~TempFileManager()
        {
            tempFiles.ForEach(file => {
                if (File.Exists(file)) File.Delete(file);
            });
        }

        public string GetNewTempFile(string name = null)
        {
            string filename = name ?? Path.GetRandomFileName();

            var newPath = Path.Combine(TempDirectory, filename);

            tempFiles.Add(newPath);
            return newPath;
        }

        public string GetNewTempFileExt(string extension)
        {
            string filename = Path.ChangeExtension(Path.GetRandomFileName(), extension);

            var newPath = Path.Combine(TempDirectory, filename);

            tempFiles.Add(newPath);
            return newPath;
        }

        public void DeleteTempFile(string path)
        {
            tempFiles.Remove(path);

            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
