namespace Integrator.Data.Entities
{
    public class Template
    {
        public int Id { get; set; }

        public bool IsRegexp { get; set; }

        public TemplateSearchField SearchField { get; set; }

        public string SearchValue { get; set; }

        public TemplateApplyField ApplyField { get; set; }

        public string? ApplyValue { get; set; }

        public int? Order { get; set; }

        public string? Description { get; set; }


        public List<CardDetailTemplateMatch> CardDetailMatches { get; set; }


        public Func<Card, string> GetCardText
        {
            get
            {
                switch (SearchField)
                {
                    case TemplateSearchField.SourceText:
                        return c => c.TextFileContent;
                    case TemplateSearchField.EngText:
                        return c => c.Translation.ContentEng;
                    case TemplateSearchField.RusText:
                        return c => c.Translation.ContentRus;
                    case TemplateSearchField.SourcePath:
                        return c => c.FolderPath;
                    case TemplateSearchField.EngPath:
                        return c => c.Translation.TitleEng;
                    case TemplateSearchField.RusPath:
                        return c => c.Translation.TitleRus;
                    case TemplateSearchField.Info:
                        return c => c.InfoContent;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }

    public enum TemplateApplyField
    {
        Brand = 1,
        Price = 2,
        Color = 3,
        Size = 4,
        Model = 5,
        Material = 6,
        Category = 7
    }

    public enum TemplateSearchField
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
