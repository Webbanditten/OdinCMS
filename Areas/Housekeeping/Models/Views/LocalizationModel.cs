using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.Housekeeping.Models.Views
{
    public class LocalizationModel
    {
        public string ResourceId { get; set; }
        public string ResourceSet { get; set; }
        public string Value { get; set; }
        public string LocaleId { get; set; }
    }


}
