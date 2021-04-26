using Rocket.API.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncreatedWarfare
{
    partial class UCWarfare
    {
        public override TranslationList DefaultTranslations =>
            new TranslationList
            {
                { "entered_cap_radius", "You have entered the capture radius of <color=#{1}>{0}</color>." },
                { "left_cap_radius", "You have left the cap radius of <color=#{1}>{0}</color>." },
                { "capturing", "Your team is capturing this point!" },
                { "team_capturing", "<color=#{1}>{0}</color> is capturing <color=#{3}>{2}</color>: <color=#{1}>{4}/{5}</color>" },
                { "team_clearing", "<color=#{1}>{0}</color> is clearing <color=#{3}>{2}</color>: <color=#{1}>{4}/{5}</color>" },
                { "losing", "Your team is losing this point!" },
                { "contested", "<color=#{1}>{0}</color> is contested! Eliminate all enemies to secure it." },
                { "clearing", "Your team is busy clearing this point." },
                { "secured", "This point is secure for now. Keep up the defense." },
                { "flag_neutralized", "<color=#{1}>{0}</color> has been neutralized!" },
                { "team_1", "USA" },
                { "team_2", "Russia" },
                { "ui_capturing", "CAPTURING" },
                { "ui_losing", "LOSING" },
                { "ui_clearing", "CLEARING" },
                { "ui_contested", "CONTESTED" },
                { "ui_secured", "SECURED" },
                { "ui_nocap", "NOT OBJECTIVE" }
            };
    }
}
