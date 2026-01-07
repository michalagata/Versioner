namespace AnubisWorks.Tools.Versioner.Interfaces
{
    public interface IProcessWrapper
    {
        (string soutput, string err) RunProccess(string filePath, string args);
    }
}

