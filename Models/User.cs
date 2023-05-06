using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharenite.Models
{
   
    class User
    {
        public Guid id { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string email { get; set; }
    }


}
