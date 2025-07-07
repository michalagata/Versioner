using System;
using System.Linq;
using System.Reflection;

public static class CommitInfoTypeExtensions
{
    public class CommitInformation
    {
        public string Commit { get; set; } = "";
        public string BuildLabel { get; set; } = "";
        public string Description { get; set; } = "";
        public string AssemblyVersion { get; set; } = "";
        public string FileVersion { get; set; } = "";
        public string InformationalVersion { get; set; } = "";
    }

    public static CommitInformation GetCommitInformation(this Type type)
    {
#if NETSTANDARD1_6
        return type.GetTypeInfo().Assembly.GetCommitInformation();
#else
        return type.Assembly.GetCommitInformation();
#endif
    }
    public static CommitInformation GetCommitInformation(this Assembly asm)
    {
        CommitInformation commitInformation = new CommitInformation();
        object[] attributes;
#if NETSTANDARD1_6
        attributes = asm.GetCustomAttributes<AssemblyDescriptionAttribute>().ToArray();
#else
        attributes = asm.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
#endif

        if (attributes.Length > 0)
        {
            AssemblyDescriptionAttribute descriptionAttribute = (AssemblyDescriptionAttribute)attributes[0];

            string descr = descriptionAttribute.Description;
            var strings = descr.Split(new[] { '|' });
            string rdesc = string.Join("|", strings.Skip(1).ToArray());
            var commitinfo = strings
                ?.FirstOrDefault()
                ?.Split(new[] { ':' });
            if (commitinfo?.Length == 2)
            {
                commitInformation.Commit = commitinfo[0];
                commitInformation.BuildLabel = commitinfo[1];
            }
            commitInformation.Description = rdesc.Trim();
        }

        try
        {

#if NETSTANDARD1_6
            commitInformation.AssemblyVersion = asm.GetName().Version.ToString()??"";// ((AssemblyVersionAttribute)asm.GetCustomAttribute(typeof(AssemblyVersionAttribute)))?.Version ?? "";
            commitInformation.FileVersion = ((AssemblyFileVersionAttribute)asm.GetCustomAttribute(typeof(AssemblyFileVersionAttribute)))?.Version ?? "";
            commitInformation.InformationalVersion =((AssemblyInformationalVersionAttribute)asm.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute)))?.InformationalVersion ?? "";
 #else
            commitInformation.AssemblyVersion = ((AssemblyVersionAttribute)asm.GetCustomAttributes(typeof(AssemblyVersionAttribute), false)?.FirstOrDefault())?.Version ?? "";
            commitInformation.FileVersion = ((AssemblyFileVersionAttribute)asm.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false)?.FirstOrDefault())?.Version ?? "";
            commitInformation.InformationalVersion = ((AssemblyInformationalVersionAttribute)asm.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)?.FirstOrDefault())?.InformationalVersion ?? "";

#endif
        }
        catch (Exception)
        {
        }

        return commitInformation;
    }
    internal static string GetAttribute<T>(this Assembly asm, Func<T, string> resolveVal) where T : Attribute
    {
        T attribute;
#if NETSTANDARD1_6
        attribute = (T)asm.GetCustomAttribute(typeof(T));
#else
        attribute = (T)asm.GetCustomAttributes(typeof(T), false).FirstOrDefault();
#endif
        if (attribute != null)
        {
            return resolveVal(attribute);
        }
        return "";
    }
}