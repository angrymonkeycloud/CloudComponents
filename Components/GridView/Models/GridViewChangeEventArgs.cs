using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{
    public class GridViewRequestDataEventArgs:EventArgs
    {
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}
