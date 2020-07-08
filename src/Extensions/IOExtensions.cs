using System.IO;
using System.Collections.Generic;
using System.Linq;

public static class IOExtensions
{

    public static bool IsFileLocked(this FileInfo file)
    {
        FileStream stream = null;

        try
        {
            //Don't change FileAccess to ReadWrite, 
            //because if a file is in readOnly, it fails.
            stream = file.Open
            (
                FileMode.Open,
                FileAccess.Read,
                FileShare.None
            );
        }
        catch (IOException)
        {
            //the file is unavailable because it is:
            //still being written to
            //or being processed by another thread
            //or does not exist (has already been processed)
            return true;
        }
        finally
        {
            if (stream != null)
                stream.Close();
        }

        //file is not locked
        return false;
    }


    public static class Searcher
    {
        public static List<string> GetDirectories(string path, string searchPattern = "*",
            SearchOption searchOption = SearchOption.AllDirectories)
        {
            if (searchOption == SearchOption.TopDirectoryOnly)
                return Directory.GetDirectories(path, searchPattern).ToList();

            var directories = new List<string>(GetDirectories(path, searchPattern));

            for (var i = 0; i < directories.Count; i++)
                directories.AddRange(GetDirectories(directories[i], searchPattern));

            directories.Add(path);

            return directories;
        }

        private static List<string> GetDirectories(string path, string searchPattern)
        {
            try
            {
                return Directory.GetDirectories(path, searchPattern).ToList();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}