using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnubisWorks.Tools.Versioner.Interfaces;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Model
{
    public class FileOperations : IFileOperations
    {
        private readonly ILogger _logger;

        public FileOperations(ILogger logger)
        {
            _logger = logger;
        }

        public List<string> FindFiles(string pattern, string workingDirectory)
        {
            try
            {
                return Directory.GetFiles(workingDirectory, pattern, SearchOption.AllDirectories).ToList();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error finding files with pattern {pattern} in {directory}", pattern, workingDirectory);
                return new List<string>();
            }
        }

        public List<string> FindDockerfiles(string workingDirectory)
        {
            return FindFiles("Dockerfile", workingDirectory);
        }

        public string ReadFileContent(string filePath)
        {
            try
            {
                return File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error reading file {filePath}", filePath);
                throw;
            }
        }

        public void WriteFileContent(string filePath, string content)
        {
            try
            {
                File.WriteAllText(filePath, content);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error writing to file {filePath}", filePath);
                throw;
            }
        }

        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public string GetCurrentDirectory()
        {
            return Directory.GetCurrentDirectory();
        }

        public void SetCurrentDirectory(string path)
        {
            try
            {
                Directory.SetCurrentDirectory(path);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error setting current directory to {path}", path);
                throw;
            }
        }
    }
} 