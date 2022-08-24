using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharenite.Models
{
    class Game
    {
        public int? id { get; set; }
        public string name { get; set; }
    }

    class Games
    {
        public List<Game> games;
    }
}
