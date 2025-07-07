
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using ConsoleAppFramework;
using FileWatcherEx;

namespace FGDDumper
{
    public static class EntityPageTools
    {
        private const string Version = "1.0.0";

        public static string WikiRoot { get; private set; } = string.Empty;

        public const  string DocsFolder = "docs\\Entities";
        public static string RootDocsFolder { get; private set; } = string.Empty;

        public const  string PagesFolder = "src\\pages\\Entities";
        public static string RootPagesFolder { get; private set; } = string.Empty;

        public const  string DumpFolder = "fgd_dump";
        public static string RootDumpFolder { get; private set; } = string.Empty;

        public const string OverridesFolder = "fgd_dump_overrides";
        public static string RootOverridesFolder { get; private set; } = string.Empty;

        public static void Main(string[] args)
        {

#if DEBUG
            //test args
            args = ["--root", "D:\\Dev\\Source2Wiki", "--generate_mdx"];
#endif
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
        /// <param name="generate_mdx">Generates the wiki files from the json in \fgd_dump, takes into account the manual overrides from \fgd_dump_overrides.</param>
        /// <param name="dump_json">Attempts to find all source2 games on the system and generate json dumps of their FGDs, 
        /// the dumps get saved into \fgd_dump, you usually want to run this program with --generate_mdx after
        /// to generate the actual wiki pages.</param>
        public static int Run(string root, bool generate_mdx, bool dump_json)
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

            if (!dump_json && !generate_mdx)
            {
                Console.WriteLine("At least one mode argument must be provided!");
                return 1;
            }

            WikiRoot = root;

            RootDocsFolder = Path.Combine(WikiRoot, DocsFolder);
            RootPagesFolder = Path.Combine(WikiRoot, PagesFolder);
            RootDumpFolder = Path.Combine(WikiRoot, DumpFolder);
            RootOverridesFolder = Path.Combine(WikiRoot, OverridesFolder);

            
            Console.WriteLine("Starting...");

            if(dump_json)
            {
                WikiFilesGenerator.DumpFGD();
            }

            if(generate_mdx)
            {
                try
                {
                    WikiFilesGenerator.GenerateMDXFromJSONDump();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nFailed to update MDX files, error: \n{ex.Message}");
                }

                var fileWatcher = new FileSystemWatcherEx(RootOverridesFolder);

                Console.WriteLine($"\nWatching for file changes in '{Path.Combine(RootOverridesFolder)}'");
                fileWatcher.OnChanged += (object? sender, FileChangedEvent e) => {

                    Console.WriteLine($"File '{e.FullPath}' changed, updating MDX.");
                    try
                    {
                        WikiFilesGenerator.GenerateMDXFromJSONDump();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\nFailed to live update {e.FullPath}, error: \n{ex.Message}");
                    }
                };


                fileWatcher.Start();

                while (Console.ReadKey().KeyChar != 'q')
                {
                }
            }

            return 0;
        }
    }
}


