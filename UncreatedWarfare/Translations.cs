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
                { "ui_capturing", "CAPTURING" },
                { "ui_losing", "LOSING" },
                { "ui_clearing", "CLEARING" },
                { "ui_contested", "CONTESTED" },
                { "ui_secured", "SECURED" },
                { "ui_nocap", "NOT OBJECTIVE" }
            };
    }
}
