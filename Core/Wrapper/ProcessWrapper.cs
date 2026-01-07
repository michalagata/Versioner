using AnubisWorks.Tools.Versioner.Interfaces;

namespace AnubisWorks.Tools.Versioner
{
    public static class ProcessWrapper
    {
        private static IProcessWrapper _instance = new ProcessWrapperImplementation();

        public static void SetInstance(IProcessWrapper instance)
        {
            _instance = instance;
        }

        public static void ResetInstance()
        {
            _instance = new ProcessWrapperImplementation();
        }

        public static (string soutput, string err) RunProccess(string filePath, string args)
        {
            return _instance.RunProccess(filePath, args);
        }
    }
}