using System;
using System.Windows;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.IO;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using ZXing;
using ZXing.Common;

namespace ReadQrCodeFromStream
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }


        private async void ReadFromDesktop_Click(object sender, RoutedEventArgs e)
        {
            //reset the ui
            ResetUI();

            //make sure the UI updates first before scanning the desktop otherwise it will find it's previously scanned qr code
            await Task.Run(() => Task.Delay(1));

            //get the size of the virtual screen (includes all monitors)
            int left = SystemInformation.VirtualScreen.Left;
            int top = SystemInformation.VirtualScreen.Top;
            int width = SystemInformation.VirtualScreen.Width;
            int height = SystemInformation.VirtualScreen.Height;

            //create a bitmap of the entire desktop
            using (var stream = new MemoryStream())
            using (var bmp = new Bitmap(width, height))
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(left, top, 0, 0, bmp.Size);

                //save to disk or a memorystream if you need the screenshot elsewhere
                //bmp.Save(stream, ImageFormat.Jpeg);
                //bmp.Save(@"c:\temp\test.jpg", ImageFormat.Jpeg);

                //try to find a qr code in the screenshot and display result
                TextBlock1.Text = FindQrCodeInImage(bmp);
            }
        }


        private void ReadFromDisk_Click(object sender, EventArgs e)
        {
            //reset the ui
            ResetUI();

            //open the file dialog with only images as the filter
            var dialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "QR Code | *.jpg; *.jpeg; *.gif; *.bmp; *.png"
            };

            //a file is selected
            if (dialog.ShowDialog() == true)
            {
                //read the selected file as a byte array
                var bin = File.ReadAllBytes(dialog.FileName);

                //try cath if the selected file is not an image or is corrupted
                try
                {
                    //make a bmp from the selected file
                    using (var stream = new MemoryStream(bin))
                    using (var bmp = new Bitmap(stream))
                    {
                        //read the qr code and show the result
                        TextBlock1.Text = FindQrCodeInImage(bmp);
                    }
                }
                catch
                {
                }
            }
        }


        private string FindQrCodeInImage(Bitmap bmp)
        {
            //decode the bitmap and try to find a qr code
            var source = new BitmapLuminanceSource(bmp);
            var bitmap = new BinaryBitmap(new HybridBinarizer(source));
            var result = new MultiFormatReader().decode(bitmap);

            //no qr code found in bitmap
            if (result == null)
            {
                System.Windows.MessageBox.Show("No QR Code found!");

                return null;
            }

            //create a new qr code image
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Height = 300,
                    Width = 300
                }
            };

            //write the result to the new qr code bmp image
            var qrcode = writer.Write(result.Text);

            //make the bmp transparent
            qrcode.MakeTransparent();

            //show the found qr code in the app
            var stream = new MemoryStream();
            qrcode.Save(stream, ImageFormat.Png);

            //display the new qr code in the ui
            Image1.Source = BitmapFrame.Create(stream);
            Image1.Visibility = Visibility.Visible;

            //and/or save the new qr code image to disk if needed
            try
            {
                //qrcode.Save($"qr_code_{DateTime.Now.ToString("yyyyMMddHHmmss")}.gif", ImageFormat.Gif);
            }
            catch
            {
                //handle disk write errors here
            }

            //return the found qr code text
            return result.Text;
        }


        private void ResetUI()
        {
            this.Dispatcher.Invoke(() =>
            {
                Image1.Visibility = Visibility.Hidden;
                TextBlock1.Text = "";
            });
        }
    }
}
