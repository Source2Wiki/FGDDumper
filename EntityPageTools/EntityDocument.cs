namespace FGDDumper
{
    public class EntityDocument
    {
        public string Name { get; init; } = string.Empty;
        public List<EntityPage> Pages { get; init; } = new();

        public string GetMDXText()
        {
            string tabImports = string.Empty;
            string tabs = string.Empty;
            bool isLegacy = false;

            foreach (var page in Pages)
            {
                // i shouldnt have to wonder why this needs .ToUpper() to render the page in the tab, yet here we are!
                tabImports += $"import {page.Game!.FileSystemName.ToUpper()}Page from '@site/src/pages/Entities/{page.GetPageRelativePath()}';\n";

                tabs +=
                $$"""
                    {{page.Game.FileSystemName}} = {<{{page.Game.FileSystemName.ToUpper()}}Page/>}

                """;

                // treat the document as legacy if any page has the legacy tag
                if (page.Legacy)
                    isLegacy = true;
            }

            var MD =
            $"""   
            ---
            hide_table_of_contents: true
            {(isLegacy ? "sidebar_class_name: legacy_item" : string.Empty)}
            custom_edit_url: /HowToEdit/entity-page-info
            ---

            <!---
            !!!!!!
            THIS PAGE IS AUTOGENERATED FROM GAME FGD DEFINITIONS!
            DO NOT EDIT MANUALLY!
            !!!!!!
            
            In order to make edits, you can make an annotation file in /fgd_dump_overrides
            -->
            
            import GameTabs from '@site/src/components/GameTabs'
            import '@site/src/css/tabs.css';
            
            # {Name}

            {(isLegacy ? new EntityPage.Annotation { Type = EntityPage.Annotation.TypeEnum.legacy }.GetMDXText() : string.Empty)}

            {tabImports}

            <GameTabs
            {tabs}
            />
            
            """;

            return MD;
        }

        public static EntityDocument GetDocument(string classname, List<EntityPage> pages)
        {
            if (pages.Count == 0)
            {
                throw new InvalidDataException("Cant have an entity document with 0 entity pages!");
            }

            return new EntityDocument
            {
                Name = classname,
                Pages = pages
            };
        }
    }
}
