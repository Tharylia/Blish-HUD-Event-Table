namespace Estreya.BlishHUD.EventTable.UI.Views
{
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class EmptySettingsLineView : View
    {
        private int Height {  get; set; }  
        public EmptySettingsLineView(int height)
        {
            this.Height = height;
        }

        protected override void Build(Container buildPanel)
        {
            new Panel()
            {
                Parent = buildPanel,
                Height = this.Height,
            };
        }
    }
}
