using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DiskUseViz
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            textBox1.Text = @"C:\Documents and Settings\Sean\My Documents\Dropbox";
            Disposed += Form1_Disposed;
        }

        void Form1_Disposed(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
            }
        }

        private void textBox1_DoubleClick(object sender, EventArgs e)
        {
            using (var dir = new FolderBrowserDialog())
                if (dir.ShowDialog() == DialogResult.OK)
                    textBox1.Text = dir.SelectedPath;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
            }
            var img = RenderImage(new DirectoryInfo(textBox1.Text), pictureBox1.Size);
            pictureBox1.Image = img;
        }

        private void Progress(int i)
        {
            if (progressBar1.InvokeRequired)
                progressBar1.Invoke(new Action<int>(Progress), i);
            else
                progressBar1.Value = Math.Max(progressBar1.Minimum, Math.Min(progressBar1.Maximum, i));
        }

        private static string MakeName(FileSystemInfo obj)
        {
            var sb = new StringBuilder();
            var temp = obj;
            while (temp != null)
            {
                if (sb.Length > 0)
                    sb.Insert(0, '/');
                sb.Insert(0, temp.Name);
                if (temp is DirectoryInfo)
                {
                    temp = ((DirectoryInfo)temp).Parent;
                }
                else if (temp is FileInfo)
                {
                    temp = ((FileInfo)temp).Directory;
                }
            }
            return sb.ToString();
        }

        Dictionary<Color, string> pickKeys = new Dictionary<Color, string>();
        private Image RenderImage(DirectoryInfo root, Size size)
        {
            var dirSizes = new Dictionary<string, double>();
            var dirs = new List<FileSystemInfo>();
            dirs.Add(root);
            int p = 0;
            int len = 1;
            while (dirs.Count > 0)
            {
                Progress(p++ * 100 / len);
                var dir = (DirectoryInfo)dirs[0];
                dirs.RemoveAt(0);
                if (dir.Exists)
                {
                    try
                    {
                        var subDirs = dir.GetDirectories();
                        len += subDirs.Length;
                        dirs.AddRange(subDirs);
                        dirSizes.Add(MakeName(dir), 0);
                        var dirFiles = dir.GetFiles();
                        foreach (var fi in dirFiles)
                        {
                            dirSizes.Add(MakeName(fi), fi.Length);
                            var next = dir;
                            while (next != null && dirSizes.ContainsKey(MakeName(next)))
                            {
                                dirSizes[MakeName(next)] += fi.Length;
                                next = next.Parent;
                            }
                        }
                    }
                    catch { }
                }
                Application.DoEvents();
            }
            var img = new Bitmap(size.Width, size.Height);
            dirs.Add(root);
            var rects = new Dictionary<string, Rectangle>();
            rects.Add(MakeName(root), new Rectangle(0, 0, size.Width, size.Height));
            len = dirs.Count;
            while (dirs.Count > 0)
            {
                Progress(p++ * 100 / len);
                var dir = dirs[0];
                var dirName = MakeName(dir);
                dirs.RemoveAt(0);
                if (dir.Exists && dir is DirectoryInfo)
                {
                    try
                    {
                        var di = (DirectoryInfo)dir;
                        var subDirs = di.GetDirectories()
                            .Cast<FileSystemInfo>()
                            .Union(di.GetFiles().Cast<FileSystemInfo>())
                            .ToArray();
                        Array.Sort(subDirs, (di1, di2) => {
                            var n1 = MakeName(di1);
                            var n2 = MakeName(di2);
                            return dirSizes.ContainsKey(n1) && dirSizes.ContainsKey(n2)
                                ? (int)(dirSizes[n2] - dirSizes[n1])
                                : -1;
                        });
                        dirs.AddRange(subDirs);
                        var flip = false;
                        if (rects.ContainsKey(dirName))
                        {
                            var space = rects[dirName];
                            foreach (var subDir in subDirs)
                            {
                                var subDirName = MakeName(subDir);
                                if (dirSizes.ContainsKey(subDirName) && dirSizes.ContainsKey(dirName))
                                {
                                    var percentage = dirSizes[subDirName] / dirSizes[dirName];
                                    int width = (int)(space.Width * (flip ? 1 : percentage));
                                    int height = (int)(space.Height * (flip ? percentage : 1));
                                    var subRect = new Rectangle(space.X, space.Y, width, height);
                                    rects.Add(subDirName, subRect);
                                    space = new Rectangle(
                                        space.X + (flip ? 0 : width),
                                        space.Y + (flip ? height : 0),
                                        space.Width - (flip ? 0 : width),
                                        space.Height - (flip ? height : 0));
                                    flip = !flip;
                                }
                            }
                        }
                    }
                    catch { }
                }
                Application.DoEvents();
            }
            pickKeys.Clear();
            int c = 0;
            using (var g = Graphics.FromImage(img))
            {
                var rs = rects.Keys.ToArray();
                for (int i = 0; i < rs.Length; ++i)
                {
                    Progress(i * 100 / rs.Length);
                    var rect = rects[rs[i]];
                    if (rect.Width > 0 && rect.Height > 0)
                    {
                        var color = Color.FromArgb((int)(0xff000000 | (uint)c));
                        c += 0x007001;
                        if (!pickKeys.ContainsKey(color))
                            pickKeys.Add(color, rs[i]);
                        g.FillRectangle(new SolidBrush(color), rect);
                    }
                    Application.DoEvents();
                }
            }
            return img;
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            var color = ((Bitmap)pictureBox1.Image).GetPixel(e.X, e.Y);
            if (pickKeys.ContainsKey(color))
                MessageBox.Show(pickKeys[color]);
        }
    }
}
