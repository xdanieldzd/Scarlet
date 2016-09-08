using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ScarletWinTest
{
    // TODO: kinda crap, any other way to prevent smear on scroll...?

    public class PanelEx : Panel
    {
        public PanelEx()
        {
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            this.Refresh();
            base.OnScroll(se);
        }
    }
}
