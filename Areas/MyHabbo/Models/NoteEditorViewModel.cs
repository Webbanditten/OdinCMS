using KeplerCMS.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.MyHabbo.Models
{
    public class NoteEditorViewModel
    {
        public string MaxLength { get; set; }
        public int Skin { get; set; }
        public string NoteText { get; set; }
        public string Query { get; set; }
        public string Scope { get; set; }
    }
}
