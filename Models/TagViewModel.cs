using System.Collections.Generic;

namespace KeplerCMS.Models
{
    public class TagFightViewModel
    {
        public string Tag1 { get; set; }
        public string Tag2 { get; set; }
        public int Tag1Count { get; set; }
        public int Tag2Count { get; set; }
        public int Winner { get; set; }
    }

    public class TagSearchViewModel {
        public List<TagSearchResult> SearchResult { get; set; }
        public List<TagCloud> Cloud { get; set; }
        public int PageNumber { get; set; }
    }
}
