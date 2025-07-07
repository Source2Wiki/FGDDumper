using Sledge.Formats.GameData.Objects;
using Sledge.Formats.GameData;
using Sledge.Formats.FileSystem;
using ValveResourceFormat.ResourceTypes;
using ValveResourceFormat.IO;
using System.Text.Json;

namespace FGDDumper
{
    public static class WikiFilesGenerator
    {
        public static void GenerateMDXFromJSONDump()
        {
            Console.WriteLine("\nGenerating MDX pages from JSON dump!\n");

            var docsDictionary = new Dictionary<string, EntityDocument>();

            string[] overrides = Directory.GetFiles(EntityPageTools.RootOverridesFolder);

            string[] jsonDocs = Directory.GetFiles(EntityPageTools.RootDumpFolder);

            foreach (var jsonDoc in jsonDocs)
            {
                var doc = JsonSerializer.Deserialize<EntityDocument>(File.ReadAllText(jsonDoc), JsonContext.Default.EntityDocument);

                if(doc is null)
                {
                    throw new InvalidDataException("Failed to deserialise json document!");
                }

                docsDictionary.Add(doc.Name, doc);
            }

            HandleOverrides(overrides, docsDictionary);

            Directory.CreateDirectory(EntityPageTools.RootDocsFolder);

            foreach ((string docName, EntityDocument doc) in docsDictionary)
            {
                var docPath = Path.Combine(EntityPageTools.RootDocsFolder, $"{doc.Name}.mdx");

                var docText = doc.GetMDXText();
                WriteFileIfContentsChanged(docPath, docText);

                foreach (var page in doc.Pages)
                {
                    var pagePath = Path.Combine(EntityPageTools.RootPagesFolder, page.GetPageRelativePath());
                    Directory.CreateDirectory(Path.GetDirectoryName(pagePath)!);

                    WriteFileIfContentsChanged(pagePath, page.GetMDXText());
                }
            }
        }

        // the format for overrides filename is 'entityClassname'-'gameFileSystemName'.json or just 'entityClassname'.json
        // if only entityClassname is provided, we treat the override as being global
        // global overrides get processed first, then game specific ones
        public static void HandleOverrides(string[] files, Dictionary<string, EntityDocument> docsDictionary)
        {
            List<(string classname, EntityPage)> globalPageOverrides = [];
            List<(string classname, EntityPage)> gameSpecificPageOverrides = [];

            foreach (var file in files)
            {
                if (Path.GetExtension(file) != ".json")
                {
                    continue;
                }

                var fileName = Path.GetFileNameWithoutExtension(file);
                var splitFilename = fileName.Split("-");
                if(splitFilename.Length > 2)
                {
                    throw new InvalidDataException("Invalid override entity filename! correct format is {entityClassname}.json or {entityClassname}-{gameFileSystemName}.json\n");
                }

                var entityClass = splitFilename[0];
                GameFinder.Game? entityGame = null;

                docsDictionary.TryGetValue(entityClass, out EntityDocument? docToOverride);

                if(docToOverride == null)
                {
                    throw new InvalidDataException($"Invalid override entity class, could not match any entity to '{entityClass}'!\n");
                }

                if (splitFilename.Length == 2)
                {
                    var gameString = splitFilename[1];
                    entityGame = GameFinder.GetGameByFileSystemName(gameString);
                
                    if(entityGame == null)
                    {
                        var error = $"Invalid override entity game '{gameString}'! valid game names are: \n\n";

                        foreach (var game in GameFinder.GameList)
                        {
                            error += $"{game.FileSystemName}\n";
                        }

                        error += "\nIn case you meant to make this a global override for all games, simply remove the - at the end, and make the filename be {entityClassname}.json\n";
                        throw new InvalidDataException(error);
                    }
                }

                if(entityGame == null)
                {
                    var overrideEntitypage = JsonSerializer.Deserialize<EntityPage>(File.ReadAllText(file), JsonContext.Default.EntityPage);
                    globalPageOverrides.Add((entityClass, overrideEntitypage!));
                }
                else
                {
                    foreach (var page in docToOverride.Pages)
                    {
                        if(page.Game == entityGame)
                        {
                            var overrideEntitypage = JsonSerializer.Deserialize<EntityPage>(File.ReadAllText(file), JsonContext.Default.EntityPage);
                            overrideEntitypage!.Game = entityGame;
                            gameSpecificPageOverrides.Add((entityClass, overrideEntitypage!));
                        }
                    }
                }
            }

            foreach ((string globalOverrideClassname, EntityPage globalOverride) in globalPageOverrides)
            {
                docsDictionary.TryGetValue(globalOverrideClassname, out var doc);

                foreach (var page in doc!.Pages)
                {
                    page.OverrideFrom(globalOverride);
                }
            }

            foreach ((string gameSpecificOverrideClassname, EntityPage gameSpecificOverride) in gameSpecificPageOverrides)
            {
                docsDictionary.TryGetValue(gameSpecificOverrideClassname, out var doc);

                foreach (var page in doc!.Pages)
                {   
                    if(page.Game == gameSpecificOverride.Game)
                    {
                        page.OverrideFrom(gameSpecificOverride);
                    }
                }
            }
        }

        public static void DumpFGD()
        {
            Console.WriteLine("\nDumping FGD to JSON!\n");

            // dictionary from entity classname -> page of that entity in every game it exists in
            var pagesDictionary = new Dictionary<string, List<EntityPage>>();

            foreach (GameFinder.Game game in GameFinder.GameList)
            {
                var gamePath = GameFinder.GetSystemPathForGame(game);

                if (string.IsNullOrEmpty(gamePath))
                {
                    continue;
                }

                var fileResolver = new FGDFilesResolver(RecursiveFileGetter.GetFiles(gamePath, ".fgd"));

                // dont want to just read all fgds, usually fgds will be included by base fgds which sit in the same folder as gameinfo.
                // this is important because stuff like @overrideclass relies on the order of loading, skipping includes is bad.
                List<string> baseFGDPaths = fileResolver.GetBaseFgdPaths(game);
                List<GameDefinition> FGDs = [];

                foreach (var FGDFile in baseFGDPaths)
                {
                    using var stream = File.OpenRead(FGDFile);
                    using var reader = new StreamReader(stream);

                    var fgdFormatter = new FgdFormat(fileResolver);
                    FGDs.Add(fgdFormatter.Read(reader));
                }

                foreach (var fgd in FGDs)
                {
                    foreach (var Class in fgd.Classes)
                    {
                        var page = EntityPage.GetEntityPage(Class, game);

                        if (page is not null)
                        {
                            if (pagesDictionary.ContainsKey(page.Name))
                            {
                                pagesDictionary[page.Name].Add(page);
                            }
                            else
                            {
                                pagesDictionary[page.Name] = new List<EntityPage> { page };
                            }
                        }
                    }
                }
            }

            foreach ((string pageName, List<EntityPage> pages) in pagesDictionary)
            {
                var doc = EntityDocument.GetDocument(pageName, pages);

                Directory.CreateDirectory(EntityPageTools.RootDumpFolder);
                var docPath = Path.Combine(EntityPageTools.RootDumpFolder, $"{doc.Name}.json");

                foreach (var page in doc.Pages)
                {
                    if (!string.IsNullOrEmpty(page.IconPath))
                    {
                        string iconPath = string.Empty;
                        if (page.IconPath.Contains("materials/"))
                        {
                            iconPath = page.IconPath;
                        }
                        else
                        {
                            iconPath = $"materials/{page.IconPath}";
                        }

                        if (!page.IconPath.Contains(".vmat"))
                        {
                            iconPath += ".vmat";
                        }

                        var entityIconVmatResource = page.Game!.LoadVPKFileCompiled(iconPath);

                        if (entityIconVmatResource?.DataBlock != null)
                        {
                            var iconMaterial = (Material)entityIconVmatResource.DataBlock;
                            var iconTexturePath = GetMaterialColorTexture(iconMaterial);

                            if (string.IsNullOrEmpty(iconTexturePath))
                            {
                                throw new InvalidDataException("Failed to get color texture for entity material!");
                            }

                            var iconTexture = page.Game.LoadVPKFileCompiled(iconTexturePath);

                            TextureContentFile textureExtract = (TextureContentFile)new TextureExtract(iconTexture).ToContentFile();
                            using var bitmap = textureExtract.Bitmap;
                            using var data = bitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);

                            page.IconPath = page.GetImageRelativePath();
                            Directory.CreateDirectory(Path.Combine(EntityPageTools.WikiRoot, page.GetImageRelativeFolder()));
                            using var stream = File.OpenWrite(Path.Combine(EntityPageTools.WikiRoot, page.IconPath));
                            data.SaveTo(stream);
                        }
                    }
                }

                var jsonText = JsonSerializer.Serialize(doc, JsonContext.Default.EntityDocument);
                File.WriteAllText(docPath, jsonText);
            }
        }

        private static string? GetMaterialColorTexture(Material material)
        {
            foreach (var textureParam in material.TextureParams)
            {
                if (textureParam.Key == "g_tColor")
                {
                    return textureParam.Value;
                }

                if (textureParam.Key == "g_tColorA")
                {
                    return textureParam.Value;
                }

                if (textureParam.Key == "g_tColorB")
                {
                    return textureParam.Value;
                }

                if (textureParam.Key == "g_tColorC")
                {
                    return textureParam.Value;
                }
            }

            return string.Empty;
        }

        private static void WriteFileIfContentsChanged(string path, string? contents)
        {
            if(File.Exists(path))
            {
                var oldFileText = File.ReadAllText(path);
                if (oldFileText == contents)
                {
                    return;
                }
            }

            File.WriteAllText(path, contents);
        }
    }

    public static class RecursiveFileGetter
    {
        public static List<string> GetFiles(string folder, string filenameFilter)
        {
            if (Directory.Exists(folder))
            {
                return ProcessDirectory(folder, filenameFilter);
            }

            throw new InvalidDataException($"RecursiveFileProcessor: Input path '{folder}' seems to not be a valid directory.");
        }

        public static List<string> ProcessDirectory(string targetDirectory, string filenameFilter)
        {
            List<string> fileList = [];

            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
            {
                var file = ProcessFile(fileName, filenameFilter);

                if (!string.IsNullOrEmpty(file))
                {
                    fileList.Add(file);
                }
            }

            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
            {
                fileList.AddRange(ProcessDirectory(subdirectory, filenameFilter));
            }

            return fileList;
        }

        public static string? ProcessFile(string path, string filenameFilter)
        {
            if (Path.GetFileName(path).Contains(filenameFilter))
                return path;

            return null;
        }
    }

    // the fgd library makes you implement this by yourself from the interface, dont really need the 2 other functions so far for our usecase
    public class FGDFilesResolver(List<string> Paths) : IFileResolver
    {
        Stream IFileResolver.OpenFile(string path)
        {
            foreach (var fullpath in Paths)
            {
                if (File.Exists(fullpath))
                {
                    // checking path against file name is needed for FGD includes, they usually only specify the filename
                    if (Equals(fullpath, path) || fullpath.Contains(path))
                    {
                        return File.Open(fullpath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    }
                }
            }

            throw new InvalidDataException($"Failed to find '{path}'");
        }

        public List<string> GetBaseFgdPaths(GameFinder.Game game)
        {
            List<string> paths = [];

            foreach (var fgdFileName in game.FgdFilesNames)
            {
                foreach (var fgdPath in Paths)
                {
                    if (fgdPath.Contains(Path.Combine(game.PathToGameinfo, fgdFileName)))
                    {
                        paths.Add(fgdPath);
                    }
                }
            }

            return paths;
        }

        IEnumerable<string> IFileResolver.GetFiles(string path)
        {
            return Paths;
        }

        // these are not really needed rn
        bool IFileResolver.FileExists(string path)
        {
            throw new NotImplementedException();
        }

        IEnumerable<string> IFileResolver.GetFolders(string path)
        {
            throw new NotImplementedException();
        }

        public bool FolderExists(string path)
        {
            throw new NotImplementedException();
        }

        public long FileSize(string path)
        {
            throw new NotImplementedException();
        }
    }
}
