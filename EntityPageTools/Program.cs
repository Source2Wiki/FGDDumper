
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using ConsoleAppFramework;
using EntityPageTools;
using FileWatcherEx;

namespace FGDDumper
{
    public static class EntityPageTools
    {
        private const string Version = "1.0.6";

        public static string WikiRoot { get; private set; } = string.Empty;

        public const string DocsFolder = "docs\\Entities";
        public static string RootDocsFolder { get; private set; } = string.Empty;

        public const string PagesFolder = "src\\pages\\Entities";
        public static string RootPagesFolder { get; private set; } = string.Empty;

        public const string DumpFolder = "fgd_dump";
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
        /// <param name="dump_fgd">Attempts to find all source2 games on the system and generate json dumps of their FGDs, 
        /// the dumps get saved into \fgd_dump, you usually want to run this program with --generate_mdx after
        /// to generate the actual wiki pages.</param>
        /// <param name="verbose">Enables extra logging which might otherwise be too annoying.</param>
        /// <param name="no_listen">Disables listening for file changes after generate_mdx and quits after first generation.</param>
        public static int Run(string root, bool generate_mdx, bool dump_fgd, bool verbose, bool no_listen)
        {
            if (string.IsNullOrEmpty(root))
            {
                Logging.Log("Docs output path can't be empty");
                return 1;
            }

            if (File.Exists(root))
            {
                Logging.Log("Docs output path can't be a file, it must be a folder");
                return 1;
            }

            if (!Directory.Exists(Path.Combine(root, ".docusaurus")))
            {
                Logging.Log($"Selected folder is not a docusaurus project, this should be the folder containing the .docusaurus folder, root: {root}");
                return 1;
            }

            if (!dump_fgd && !generate_mdx)
            {
                Logging.Log("At least one mode argument must be provided!");
                return 1;
            }

            if (verbose)
            {
                Logging.Verbose = true;
            }

            WikiRoot = root;

            RootDocsFolder = Path.Combine(WikiRoot, DocsFolder);
            RootPagesFolder = Path.Combine(WikiRoot, PagesFolder);
            RootDumpFolder = Path.Combine(WikiRoot, DumpFolder);
            RootOverridesFolder = Path.Combine(WikiRoot, OverridesFolder);

            Logging.Log($"Entity Page Tools, Version {Version}.");
            Logging.Log("Starting...");

            if (dump_fgd)
            {
                WikiFilesGenerator.DumpFGD();
            }

            if (generate_mdx)
            {
                try
                {
                    WikiFilesGenerator.GenerateMDXFromJSONDump();
                }
                catch (Exception ex)
                {
                    Logging.Log($"\nFailed to update MDX files, error: \n{ex.Message}");
                }

                if (!no_listen)
                {
                    var fileWatcher = new FileSystemWatcherEx(RootOverridesFolder);

                    Logging.Log($"\nWatching for file changes in '{Path.Combine(RootOverridesFolder)}'");
                    fileWatcher.OnChanged += UpdateMDX;
                    fileWatcher.OnCreated += UpdateMDX;
                    fileWatcher.OnRenamed += UpdateMDX;

                    fileWatcher.Start();

                    while (Console.ReadKey().KeyChar != 'q')
                    {
                    }
                }
            }

            return 0;
        }

        private static void UpdateMDX(object? sender, FileChangedEvent e)
        {
            Logging.Log($"\nFile '{e.FullPath}' changed, updating MDX.");
            try
            {
                WikiFilesGenerator.GenerateMDXFromJSONDump();
                Logging.Log($"\nWatching for file changes in '{Path.Combine(RootOverridesFolder)}'");
            }
            catch (Exception ex)
            {
                Logging.Log($"\nFailed to live update {e.FullPath}, error: \n{ex.Message}");
            }
        }
    }
}


