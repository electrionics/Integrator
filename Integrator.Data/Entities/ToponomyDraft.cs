using Integrator.Data.Helpers;

namespace Integrator.Data.Entities
{
    public abstract class ToponomyDraft
    {
        #region ctors

        public ToponomyDraft() 
        { 
        }

        public ToponomyDraft(Card card)
        {
            SetNames(card);
        }

        #endregion

        public int Id { get; set; }

        public string Name { get; set; }

        public string FullName { get; set; }

        public int CardId { get; set; }


        #region Abstract Members

        protected abstract bool IsFirstPathNameToSelect { get; } // otherwise - last

        protected abstract string ToponomyParentString { get; }

        #endregion


        #region Processing Properties

        public bool IsAcceptable => !string.IsNullOrEmpty(Name) && LongFullName.Contains(ToponomyParentString);

        public string LongFullName { get; set; }

        #endregion


        #region Logic Methods

        private void SetNames(Card card)
        {
            CardId = card.Id;

            var pathBeforeChildFolder = StringHelper.GetParentFolder(card.FolderPath, card.FolderName);
            LongFullName = StringHelper.RemoveExtraSymbols(pathBeforeChildFolder, string.Empty);

            FullName = LongFullName.Replace(ToponomyParentString, string.Empty).TrimEnd('\\');

            SetRecommendedName();
        }

        private void SetRecommendedName()
        {
            var childFolderIndex = FullName.LastIndexOf('\\') + 1;
            var parentFolderIndex = FullName.IndexOf('\\');

            var nameFromEnd = FullName.Substring(childFolderIndex, FullName.Length - childFolderIndex).Trim();
            var nameFromBeginning = parentFolderIndex > 0
                ? FullName.Substring(0, parentFolderIndex).Trim()
                : FullName.Trim();

            Name = StringHelper.RemoveExtraSymbols(IsFirstPathNameToSelect ? nameFromBeginning : nameFromEnd, string.Empty);
        }

        #endregion
    }
}
