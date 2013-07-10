﻿using System;
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
        }

        private void textBox1_DoubleClick(object sender, EventArgs e)
        {
            using (var dir = new FolderBrowserDialog())
                if (dir.ShowDialog() == DialogResult.OK)
                    textBox1.Text = dir.SelectedPath;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var img = RenderImage(textBox1.Text, pictureBox1.Size);
            pictureBox1.Image = img;
        }

        private void Progress(int i)
        {
            if (progressBar1.InvokeRequired)
                progressBar1.Invoke(new Action<int>(Progress), i);
            else
                progressBar1.Value = i;
        }
        Dictionary<Color, string> pickKeys = new Dictionary<Color, string>();
        private Image RenderImage(string root, Size size)
        {
            var dirSizes = new Dictionary<string, double>();
            var dirs = new List<string>();
            var files = new List<string>();
            dirs.Add(root);
            int p = 0;
            while (dirs.Count > 0)
            {
                Progress(p / 10);
                p = (p + 1) % 1000;
                var dir = dirs[0];
                dirs.RemoveAt(0);
                var di = new DirectoryInfo(dir);
                if (di.Name != "hive76" && di.Exists)
                {
                    dirs.AddRange(Directory.GetDirectories(dir));
                    dirSizes.Add(dir, 0);
                    foreach (var f in Directory.GetFiles(dir))
                    {
                        var fi = new FileInfo(f);
                        dirSizes.Add(f, fi.Length);
                        var next = di;
                        while (dirSizes.ContainsKey(next.FullName))
                        {
                            dirSizes[next.FullName] += fi.Length;
                            next = next.Parent;
                        }
                    }
                }
                Application.DoEvents();
            }
            var img = new Bitmap(size.Width, size.Height);
            dirs.Add(root);
            var rects = new Dictionary<string, Rectangle>();
            rects.Add(root, new Rectangle(0, 0, size.Width, size.Height));
            while (dirs.Count > 0)
            {
                Progress(p / 10);
                p = (p + 1) % 1000;
                var dir = dirs[0];
                dirs.RemoveAt(0);
                if (Directory.Exists(dir))
                {
                    var subDirs = Directory.GetDirectories(dir)
                        .Where(d=>!d.EndsWith("hive76"))
                        .Union(Directory.GetFiles(dir))
                        .ToArray();
                    Array.Sort(subDirs, (di1, di2) => (int)(dirSizes[di2] - dirSizes[di1]));
                    dirs.AddRange(subDirs);
                    var flip = false;
                    if (rects.ContainsKey(dir))
                    {
                        var space = rects[dir];
                        foreach (var subDir in subDirs)
                        {
                            var percentage = dirSizes[subDir] / dirSizes[dir];
                            int width = (int)(space.Width * (flip ? 1 : percentage));
                            int height = (int)(space.Height * (flip ? percentage : 1));
                            var subRect = new Rectangle(space.X, space.Y, width, height);
                            rects.Add(subDir, subRect);
                            space = new Rectangle(
                                space.X + (flip ? 0 : width),
                                space.Y + (flip ? height : 0),
                                space.Width - (flip ? 0 : width),
                                space.Height - (flip ? height : 0));
                            flip = !flip;
                        }
                    }
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
                        c+=0x007001;
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
