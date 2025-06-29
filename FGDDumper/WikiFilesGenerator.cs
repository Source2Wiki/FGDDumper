using Sledge.Formats.GameData.Objects;
using Sledge.Formats.GameData;
using Sledge.Formats.FileSystem;
using ValveResourceFormat;
using SteamDatabase.ValvePak;
using ValveResourceFormat.ResourceTypes;
using ValveResourceFormat.IO;
using System.IO;
using System.Security.Claims;

namespace FGDDumper
{
    public class WikiFilesGenerator
    {
        // dictionary from entity classname -> page of that entity in every game it exists in
        private Dictionary<string, List<EntityPage>> PagesDictionary = new();

        private string DocsFolder = string.Empty;
        private string PagesFolder = string.Empty;
        private string EntityImagesFolder = string.Empty;

        public WikiFilesGenerator(string docsFolder, string pagesFolder, string entityImagesFolder)
        {
            DocsFolder = docsFolder;
            PagesFolder = pagesFolder;
            EntityImagesFolder = entityImagesFolder;

            foreach (GameFinder.Game game in GameFinder.GameList)
            {
                var gamePath = GameFinder.GetSystemPathForGame(game);

                if(string.IsNullOrEmpty(gamePath))
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
                            AddToDict(page.Name, page);
                        }
                    }
                }
            }
        }

        public void SaveFilesToDisk()
        {
            foreach ((string pageName, List<EntityPage> pages) in PagesDictionary)
            {
                var doc = EntityDocument.GetDocument(pageName, pages);

                Directory.CreateDirectory(DocsFolder);

                var docPath = Path.Combine(DocsFolder, $"{doc.Name}.mdx");
                File.WriteAllText(docPath, doc.GetText());

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

                        var entityIconVmatResource = page.Game.LoadVPKFileCompiled(iconPath);

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

                            var texturePath = Path.Combine(EntityImagesFolder, page.Game.FileSystemName);
                            Directory.CreateDirectory(texturePath);
                            using var stream = File.OpenWrite(Path.Combine(texturePath, $"{page.Name}.png"));
                            data.SaveTo(stream);
                        }
                    }

                    var pagePath = Path.Combine(PagesFolder, page.GetPageRelativePath());
                    Directory.CreateDirectory(Path.GetDirectoryName(pagePath));
                    File.WriteAllText(pagePath, page.GetText());
                }
            }
        }

        private string? GetMaterialColorTexture(Material material)
        {
            foreach (var textureParam in material.TextureParams)
            {
                if(textureParam.Key == "g_tColor")
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

        private void AddToDict(string key, EntityPage value)
        {
            if (PagesDictionary.ContainsKey(key))
            {
                PagesDictionary[key].Add(value);
            }
            else
            {
                PagesDictionary[key] = new List<EntityPage> { value };
            }
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
