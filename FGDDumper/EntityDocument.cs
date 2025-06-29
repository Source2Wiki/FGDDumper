namespace FGDDumper
{
    public class EntityDocument
    {
        public string Name { get; init; } = string.Empty;
        public List<EntityPage> Pages { get; private set; } = new();

        public string GetText()
        {
            string tabImports = string.Empty;
            string tabs = string.Empty;

            foreach (var page in Pages)
            {
                Directory.CreateDirectory("D:\\Dev\\Source2Wiki\\src\\pages\\Entities");

                // i shouldnt have to wonder why this needs .ToUpper() to render the page in the tab, yet here we are!
                tabImports += $"import {page.Game.FileSystemName.ToUpper()}Page from '@site/src/pages/Entities/{page.GetPageRelativePath()}';\n";

                tabs +=
                $"""

                <TabItem value="{page.Game.FileSystemName}" label="{page.Game.Name}">
                    <{page.Game.FileSystemName.ToUpper()}Page />
                </TabItem>

                """;
            }

            var MD =
            $"""    
            ---
            hide_table_of_contents: true
            ---

            import Tabs from '@theme/Tabs';
            import TabItem from '@theme/TabItem';

            # {Name}

            {tabImports}

            <Tabs queryString="game">
                {tabs}
            </Tabs>
            
            """;

            return MD;
        }

        public static EntityDocument GetDocument(string classname, List<EntityPage> pages)
        {
            if(pages.Count == 0)
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
