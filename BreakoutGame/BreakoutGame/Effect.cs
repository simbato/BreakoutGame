using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreakoutGame
{
    class Effect
    {
        private bool mobile;
        private string description;

        /*
        public Effect(bool mobile, string description)
        {
            this.mobile = mobile;
            this.description = description;
        }
        */
        public bool Mobile
        {
            get { return mobile; }
            set { mobile = value; }
        }

        public string Description
        {
            get { return description; }
            set { description = value; }
        }

    }
}
