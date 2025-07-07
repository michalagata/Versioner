using System.Collections.Generic;

namespace AnubisWorks.Tools.Versioner.Interfaces
{
    public interface IFileOperations
    {
        List<string> FindFiles(string pattern, string workingDirectory);
        List<string> FindDockerfiles(string workingDirectory);
        string ReadFileContent(string filePath);
        void WriteFileContent(string filePath, string content);
        bool FileExists(string filePath);
        string GetCurrentDirectory();
        void SetCurrentDirectory(string path);
    }
} 