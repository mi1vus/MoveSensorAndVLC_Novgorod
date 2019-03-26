using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Turn
{
    public partial class VLC : UserControl
    {
        public VLC()
        {
            InitializeComponent();

            if (axVLCPlugin21.playlist.isPlaying)
                axVLCPlugin21.playlist.stop();
            axVLCPlugin21.playlist.items.clear();
            axVLCPlugin21.playlist.add(@"C:\Users\Max\Downloads\IMG_5447.MOV", null, null);
            axVLCPlugin21.playlist.playItem(0);
            axVLCPlugin21.playlist.play();
        }
    }
}
