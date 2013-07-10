using System;
using System.Drawing;

namespace DiskUseViz
{
    public class ColorAlgorithms
    {
        public static Color FromHSB(float hue, float saturation, float brightness)
        {
            int i = (int)(hue / 60) % 6;
            float f = hue / 60 - i;
            brightness *= 2;
            float p = brightness * (1 - saturation);
            float q = brightness * (1 - f * saturation);
            float t = brightness * (1 - (1 - f) * saturation);
            float r = 0, g = 0, b = 0;
            switch (i)
            {
                case 0:
                    r = brightness;
                    g = t;
                    b = p;
                    break;
                case 1:
                    r = q;
                    g = brightness;
                    b = p;
                    break;
                case 2:
                    r = p;
                    g = brightness;
                    b = t;
                    break;
                case 3:
                    r = p;
                    g = q;
                    b = brightness;
                    break;
                case 4:
                    r = t;
                    g = p;
                    b = brightness;
                    break;
                case 5:
                    r = brightness;
                    g = p;
                    b = q;
                    break;
            }
            return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }
    }
}