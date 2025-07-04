
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using ConsoleAppFramework;

namespace FGDDumper
{
    public static class FGDDumper
    {
        private const string Version = "1.0.0";

        public static string WikiRoot { get; private set; } = string.Empty;

        public const  string DocsFolder = "docs\\Entities";
        public static string RootDocsFolder { get; private set; } = string.Empty;

        public const  string PagesFolder = "src\\pages\\Entities";
        public static string RootPagesFolder { get; private set; } = string.Empty;

        public const  string DumpFolder = "fgd_dump";
        public static string RootDumpFolder { get; private set; } = string.Empty;

        public static void Main(string[] args)
        {
            //test args
            args = ["--root", "D:\\Dev\\Source2Wiki"];

            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            // https://github.com/Cysharp/ConsoleAppFramework
            ConsoleApp.Version = GetVersion();

            // Go to definition on this method to see the generated source code
            ConsoleApp.Run(args, Run);
        }

        private static string GetVersion()
        {
            var info = new StringBuilder();
            info.Append($"Version: {Version}");
            info.AppendLine($"OS: {RuntimeInformation.OSDescription}");
            return info.ToString();
        }

        /// <summary>
        /// An automatic entity documentation page generator for the Source2 Wiki.
        /// </summary>
        /// <param name="root">Folder path for the root of the docusaurus project.</param>
        public static int Run(string root)
        {
            if(string.IsNullOrEmpty(root))
            {
                Console.WriteLine("Docs output path can't be empty");
                return 1;
            }

            if (File.Exists(root))
            {
                Console.WriteLine("Docs output path can't be a file, it must be a folder");
                return 1;
            }

            if (!Directory.Exists(Path.Combine(root, ".docusaurus")))
            {
                Console.WriteLine("Selected folder is not a docusaurus project, this should be the folder containing the .docusaurus folder");
                return 1;
            }

            WikiRoot = root;

            RootDocsFolder = Path.Combine(WikiRoot, DocsFolder);
            RootPagesFolder = Path.Combine(WikiRoot, PagesFolder);
            RootDumpFolder= Path.Combine(WikiRoot, DumpFolder);

            Console.WriteLine("Starting...");

            //WikiFilesGenerator.DumpFGD();
            WikiFilesGenerator.GenerateMDXFromJSONDump();

            return 0;
        }
    }
}


