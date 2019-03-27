using AxAXVLC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Turn
{
    /// <summary>
    /// Interaction logic for Video.xaml
    /// </summary>
    public partial class Video : Window
    {
        [DllImport("user32.dll", EntryPoint = "GetSystemMenu")]
        private static extern IntPtr GetSystemMenu(IntPtr hwnd, int revert);

        [DllImport("user32.dll", EntryPoint = "GetMenuItemCount")]
        private static extern int GetMenuItemCount(IntPtr hmenu);

        [DllImport("user32.dll", EntryPoint = "RemoveMenu")]
        private static extern int RemoveMenu(IntPtr hmenu, int npos, int wflags);

        [DllImport("user32.dll", EntryPoint = "DrawMenuBar")]
        private static extern int DrawMenuBar(IntPtr hwnd);

        private const int MF_BYPOSITION = 0x0400;
        private const int MF_DISABLED = 0x0002;

        //VLC VLСControl;
        AxVLCPlugin2 VLСControl;
        public Video()
        {
            try
            {
                InitializeComponent();
                this.SourceInitialized += new EventHandler(Window1_SourceInitialized);

                //VLСControl = new VLC();
                VLСControl = new AxVLCPlugin2();
                WinFormsHost.Child = VLСControl;

                VLСControl.Dock = System.Windows.Forms.DockStyle.Fill;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void PlayVideo(string path)
        {
            VLСControl.video.toggleFullscreen();
            if (VLСControl.playlist.isPlaying)
                VLСControl.playlist.stop();
            VLСControl.playlist.items.clear();


            var uri = new Uri(path);
            var convertedURI = uri.AbsoluteUri;

            VLСControl.playlist.add(convertedURI, null, null);
            VLСControl.AutoLoop = true;
            VLСControl.playlist.play();
        }

        public void StopVideo()
        {
            VLСControl.video.fullscreen = false;

            if (VLСControl.playlist.isPlaying)
                VLСControl.playlist.stop();
            VLСControl.playlist.items.clear();
        }

        void Window1_SourceInitialized(object sender, EventArgs e)
        {
            WindowInteropHelper helper = new WindowInteropHelper(this);
            IntPtr windowHandle = helper.Handle; //Get the handle of this window
            IntPtr hmenu = GetSystemMenu(windowHandle, 0);
            int cnt = GetMenuItemCount(hmenu);
            //remove the button
            RemoveMenu(hmenu, cnt - 1, MF_DISABLED | MF_BYPOSITION);
            //remove the extra menu line
            RemoveMenu(hmenu, cnt - 2, MF_DISABLED | MF_BYPOSITION);
            DrawMenuBar(windowHandle); //Redraw the menu bar
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //VLСControl.video.toggleFullscreen();

            //var uri = new Uri(@"C:\Users\Max\Downloads\IMG_5447.MOV");
            //var convertedURI = uri.AbsoluteUri;

            //VLСControl.playlist.add(convertedURI);
            //VLСControl.playlist.play();
        }
    }
}
