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
        public DateTime? added { get; set; }
        public int? community_score { get; set; }
        public int? critic_score { get; set; }
        public string description { get; set; }
        public bool favorite { get; set; }
        public string game_id { get; set; }
        public string game_started_script { get; set; }
        public bool hidden { get; set; }
        public bool include_library_plugin_action { get; set; }
        public string install_directory { get; set; }
        public bool is_custom_game { get; set; }
        public bool is_installed { get; set; }
        public bool is_installing { get; set; }
        public bool is_launching { get; set; }
        public bool is_running { get; set; }
        public bool is_uninstalling { get; set; }
        public DateTime? last_activity { get; set; }
        public string manual { get; set; }
        public DateTime? modified { get; set; }
        public string notes { get; set; }
        public UInt64 play_count { get; set; }
        public UInt64 playtime { get; set; }
        public Guid plugin_id { get; set; }
        public string post_script { get; set; }
        public string pre_script { get; set; }
        public DateTime release_date { get; set; }
        public string sorting_name { get; set; }
        public bool use_global_game_started_script { get; set; }
        public bool use_global_post_script { get; set; }
        public bool use_global_pre_script { get; set; }
        public int? user_score { get; set; }
        public string version { get; set; }
    }

    class Games
    {
        public List<Game> games;
    }
}
