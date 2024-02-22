namespace Integrator.Data.Entities
{
    public class Template
    {
        public int Id { get; set; }

        public required string Search { get; set; }

        public required string Value { get; set; }

        public TemplateType Type { get; set; }

        public TemplateSource Source { get; set; }

        public int Order { get; set; }

        public string? Description { get; set; }


        public List<CardDetailTemplateMatch> CardDetailMatches { get; set; }


        public Func<Card, string> GetCardText
        {
            get
            {
                switch (Source)
                {
                    case TemplateSource.SourceText:
                        return c => c.TextFileContent;
                    case TemplateSource.EngText:
                        return c => c.CardTranslation.ContentEng;
                    case TemplateSource.RusText:
                        return c => c.CardTranslation.ContentRus;
                    case TemplateSource.SourcePath:
                        return c => c.FolderPath;
                    case TemplateSource.EngPath:
                        return c => c.CardTranslation.TitleEng;
                    case TemplateSource.RusPath:
                        return c => c.CardTranslation.TitleRus;
                    case TemplateSource.Info:
                        return c => c.InfoContent;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }

    public enum TemplateType
    {
        Brand = 1,
        Price = 2,
        Color = 3,
        Size = 4
    }

    public enum TemplateSource
    {
        SourceText = 1,
        EngText = 2,
        RusText = 3,
        
        Info = 4,
        
        SourcePath = 5,
        EngPath = 6, 
        RusPath = 7
    }
}
