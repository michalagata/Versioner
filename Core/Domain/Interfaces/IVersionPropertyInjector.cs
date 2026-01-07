using System.Xml.Linq;

namespace AnubisWorks.Tools.Versioner.Domain.Interfaces
{
    /// <summary>
    /// Interface for injecting version properties into project files when they are missing.
    /// </summary>
    public interface IVersionPropertyInjector
    {
        /// <summary>
        /// Ensures that version properties exist in the project file.
        /// If properties are missing, they are added with default values.
        /// </summary>
        /// <param name="project">The XDocument representing the project file</param>
        /// <param name="projectType">The type of project (SDK or Props)</param>
        /// <param name="defaultVersion">The default version to use if properties are missing</param>
        /// <returns>True if properties were added, false if they already existed</returns>
        bool EnsureVersionPropertiesExist(XDocument project, ProjectType projectType, string defaultVersion = "1.0.0.0");

        /// <summary>
        /// Checks if version properties exist in the project file.
        /// </summary>
        /// <param name="project">The XDocument representing the project file</param>
        /// <param name="projectType">The type of project (SDK or Props)</param>
        /// <returns>True if version properties exist, false otherwise</returns>
        bool HasVersionProperties(XDocument project, ProjectType projectType);
    }
}
