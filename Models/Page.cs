using KeplerCMS.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Models
{
    public class Page
    {
        public Pages Details { get; set; }
        public List<Containers> Containers { get; set; }
    }
}
