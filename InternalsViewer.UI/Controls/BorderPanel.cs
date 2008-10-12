﻿using System.Windows.Forms;
using System.Drawing;

namespace InternalsViewer.UI.Controls
{
    internal class BorderPanel : Panel
    {
        public BorderPanel()
        {
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle, SystemColors.ControlDark, ButtonBorderStyle.Solid);
        }
    }
}
