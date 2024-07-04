namespace Integrator.Web.Blazor.Shared
{
    public class TemplateCheckViewModel
    {
        public int CountAffected { get; set; }

        public int CountResulted { get; set; }

        public Dictionary<string, string> Errors { get; set; }
    }
}
