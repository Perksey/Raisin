using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Tallinn.Models;
using Tallinn.Visitors;

namespace Tallinn
{
    public class ProjectHandler
    {
        private static readonly ILogger Logger = Program.LoggerProvider.CreateLogger("ProjectHandler");
        public Documentation Documentation { get; }

        public ProjectHandler(Documentation documentation)
        {
            Documentation = documentation;
        }
        
        public async Task<bool> HandleProjectAsync(Project project)
        {
            Logger.LogInformation($"Job started for project \"{project.Name}\", obtaining compilation...");
            var compilation = await project.GetCompilationAsync();
            if (compilation is null)
            {
                Logger.LogError("Compilation was not successfully obtained.");
                return false;
            }
            
            Logger.LogInformation("Compilation obtained.");
            var visitor = new CSharpVisitor(Documentation, project, compilation);
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                visitor.Visit(await syntaxTree.GetRootAsync());
            }

            Logger.LogInformation("Job complete.");
            return true;
        }
    }
}