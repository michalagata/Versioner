using AnubisWorks.Lib.ArgsInterceptor;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnubisWorks.Tools.Versioner.Cli.Helper
{
    public static class HelpPrinter
    {
        public static void Print(ArgsInterceptor p)
        {
            p.SetupHelp("?", "help")
                .Callback(text => Console.WriteLine(text));
            p.HelpOption.ShowHelp(p.Options);
        }

        public static void Print<T>(ArgsInterceptor<T> pr) where T : class
        {
            ArgsInterceptor<T> p = new ArgsInterceptor<T>();
            p.SetupHelp("?", "help")
                .Callback(text => Console.WriteLine(text));
            p.HelpOption.ShowHelp(p.Options);
        }
    }
}
