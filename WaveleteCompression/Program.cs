using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

namespace WaveleteCompression
{
    class Program
    {
        static void Main(string[] args)
        {

            // Файл с изображением для сжатия (реализация алгоритма позволяет сжимать
            // только квадратные изображения, размер сторон у которого равен степени числа 2)
            string path = "F:\\Projects\\WaveleteCompression\\WaveleteCompression\\bin\\Release\\lenna.png";
            
            // Копрессор
            // Засекаем время
            DateTime startTime = DateTime.Now;
            wvCompress c = new wvCompress();
            byte[] compressed = c.run(path);
            // Расчет и вывод в консоль затраченного времени
            TimeSpan duration = DateTime.Now - startTime;
            Console.Write("Compressed: ");
            Console.WriteLine(duration.Seconds * 1000 + duration.Milliseconds + " ms");

            // Декомпрессор
            // Засекаем время
            startTime = DateTime.Now;
            wvDecompress d = new wvDecompress();
            byte[] decompressed = d.run(compressed);
            duration = DateTime.Now - startTime;
            Console.Write("Decompressed: ");
            Console.WriteLine(duration.Seconds * 1000 + duration.Milliseconds + " ms");

            // Расжатое изображение
            Bitmap bitmap1 = BytesToBitmap(decompressed);
            // Сохранение расжатого изображения
            bitmap1.Save(path + ".bmp", ImageFormat.Bmp);

            // Сохранение в RAW без пост-сжатия (для того, чтобы можно было поэкспериментировать с пост-сжатием)
            FileStream f = new System.IO.FileStream(path + ".bmp.raw", FileMode.Create, FileAccess.Write);
            f.Write(decompressed, 0, decompressed.Length);
            f.Close();

            string ret = Console.ReadLine();

        }

        public unsafe static Bitmap BytesToBitmap(byte[] data)
        {
            Size size = new System.Drawing.Size(512, 512);
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            Bitmap bmp = new Bitmap(size.Width, size.Height, size.Width * 3, PixelFormat.Format24bppRgb, handle.AddrOfPinnedObject());
            handle.Free();
            return bmp;
        }

    }
}










