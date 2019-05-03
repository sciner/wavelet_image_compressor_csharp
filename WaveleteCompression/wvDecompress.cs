using System;
using System.Collections.Generic;
using System.Text;

namespace WaveleteCompression
{
    class wvDecompress
    {

        // Константы
        public const int WV_LEFT_TO_RIGHT = 0;
        public const int WV_TOP_TO_BOTTOM = 1;

        public byte[] run(byte[] compressed)
        {
            int z;
            int dwDepth = 6; // количество уровней свертки вейвлета (чем их больше, тем лучше жмется)
            // жестко забитые размеры изображения
            int w = 512;
            int h = 512;
            // по сути размер изображения и вот эти коэффициенты должны браться из заголовка(header) сжатого файла
            int[] dwDiv = { 48, 32, 16, 16, 24, 24, 1, 1 }, dwTop = { 24, 32, 24, 24, 24, 24, 32, 32 };
            int SamplerDiv = 2, YPerec = 100, crPerec = 85, cbPerec = 85;

            double[,,] yuv = doUnPack(compressed, w, h, dwDepth);

            // Развертка вейвлета
            for(z = 0; z < 2; z++)
            {
                for(int dWave = dwDepth - 1; dWave >= 0; dWave--)
                {
                    int w2 = Convert.ToInt32(w / Math.Pow(2, dWave));
                    int h2 = Convert.ToInt32(h / Math.Pow(2, dWave));
                    WaveleteUnPack(yuv, z, w2, h2, dwDiv[dWave] * SamplerDiv);
                }
            }
            z = 2;
            for(int dWave = dwDepth - 1; dWave >= 0; dWave--)
            {
                int w2 = Convert.ToInt32(w / Math.Pow(2, dWave));
                int h2 = Convert.ToInt32(h / Math.Pow(2, dWave));
                WaveleteUnPack(yuv, z, w2, h2, dwDiv[dWave]);
            }
            // YCrCb декодирование + разложение изображения в плоский массив
            byte[]  rgb_flatened = this.YCrCbDecode(yuv, w, h, YPerec, crPerec, cbPerec);
            return rgb_flatened;
        }

        // Данная процедура является обратной процедуре DoPack в классе wvCompress.
        // Она обратно переводит его в (short)double-тип из типа byte[]
        private static double[, ,] doUnPack(byte[] Bytes, int cW, int cH, int dwDepth)
        {
            int lPos = 0;
            byte Value;
            int intIndex = 0;
            // размер итогового изображения в байтах
            int size = cW * cH * 3;
            // временный массив для результирующих коэффициентов свернутого вейвлета
            double[,,] ImgData = new double[3, cW, cH];

            int shortsLength = Bytes.Length - size;
            short[] shorts = new short[shortsLength / 2];
            Buffer.BlockCopy(Bytes, size, shorts, 0, shortsLength);

            for (int d = dwDepth - 1; d >= 0; d--)
            {
                int wSize = (int)Math.Pow(2, d);
                int W = cW / wSize;
                int H = cH / wSize;
                int w2 = W / 2;
                int h2 = H / 2;
                // левый верхний
                if (d == dwDepth - 1)
                {
                    for (int z = 0; z < 3; z++)
                    {
                        for (int j = 0; j < h2; j++)
                        {
                            for (int i = 0; i < w2; i++)
                            {
                                Value = Bytes[lPos++];
                                if(Value == 255)
                                {
                                    ImgData[z, i, j] = shorts[intIndex++];
                                }
                                else
                                {
                                    ImgData[z, i, j] = Value - 127;
                                }
                            }
                        }
                    }
                }
                // верхний правый + нижний правый
                for (int z = 0; z < 3; z++)
                {
                    for (int j = 0; j < H; j++)
                    {
                        for (int i = w2; i < W; i++)
                        {
                            Value = Bytes[lPos++];
                            if(Value == 255)
                            {
                                ImgData[z, i, j] = shorts[intIndex++];
                            } else {
                                ImgData[z, i, j] = Value - 127;
                            }
                        }
                    }
                }
                // левый нижний
                for (int z = 0; z < 3; z++)
                {
                    for (int j = h2; j < H; j++)
                    {
                        for (int i = 0; i < w2; i++)
                        {
                            Value = Bytes[lPos++];
                            if (Value == 255)
                            {
                                ImgData[z, i, j] = shorts[intIndex++];
                            }
                            else
                            {
                                ImgData[z, i, j] = Value - 127;
                            }
                        }
                    }
                }
            }
            // возвращаем результат
            return ImgData;
        }

        // Функция развертки вейвлета
        private void WaveleteUnPack(double[,,] ImgArray, int Component, int cW, int cH, int dwDevider)
        {
            int cw2 = cW / 2, ch2 = cH / 2;
            double dbDiv = 1f / dwDevider;
            // деквантование значений
            for(int i = 0; i < cW; i++)
            {
                for(int j = 0; j < cH; j++)
                {
                    if ((i >= cw2) || (j >= ch2))
                    {
                        if (ImgArray[Component, i, j] != 0)
                        {
                            ImgArray[Component, i, j] /= dbDiv;
                        }
                    }
                }
            }
            // Развертка вейвлета
            for(int i = 0; i < cW; i++)
            {
                reWv(ref ImgArray, cH, Component, i, WV_LEFT_TO_RIGHT);
            }
            for(int j = 0; j < cH; j++)
            {
                reWv(ref ImgArray, cW, Component, j, WV_TOP_TO_BOTTOM);
            }
        }

        // Процедура обратного быстрого лифтинга дискретного биортогонального CDF 9/7 вейвлета
        private void reWv(ref double[, ,] shorts, int n, int z, int dwPos, int Side)
        {

            double a;
            double[] xWavelet = new double[n];
            double[] tempbank = new double[n];

            if(Side == WV_LEFT_TO_RIGHT)
            {
                for(int j = 0; j < n; j++)
                {
                    xWavelet[j] = shorts[z, dwPos, j];
                }
            }
            else if (Side == WV_TOP_TO_BOTTOM)
            {
                for(int  i = 0; i < n; i++)
                {
                    xWavelet[i] = shorts[z, i, dwPos];
                }
            }

            for(int i = 0; i < n / 2; i++)
            {
                tempbank[i * 2] = xWavelet[i];
                tempbank[i * 2 + 1] = xWavelet[i + n / 2];
            }
            for(int i = 0; i < n; i++)
            {
                xWavelet[i] = tempbank[i];
            }

            // Undo scale
            a = 1.149604398f;
            for(int  i = 0; i < n; i++)
            {
                if(i % 2 != 0)
                {
                    xWavelet[i] = xWavelet[i] * a;
                } else {
                    xWavelet[i] = xWavelet[i] / a;
                }
            }

            // Undo update 2
            a = -0.4435068522f;
            for (int i = 2; i < n; i += 2)
            {
                xWavelet[i] = xWavelet[i] + a * (xWavelet[i - 1] + xWavelet[i + 1]);
            }
            xWavelet[0] = xWavelet[0] + 2 * a * xWavelet[1];

            // Undo predict 2
            a = -0.8829110762f;
            for (int i = 1; i < n - 1; i += 2)
            {
                xWavelet[i] = xWavelet[i] + a * (xWavelet[i - 1] + xWavelet[i + 1]);
            }
            xWavelet[n - 1] = xWavelet[n - 1] + 2 * a * xWavelet[n - 2];

            // Undo update 1
            a = 0.05298011854f;
            for (int i = 2; i < n; i += 2)
            {
                xWavelet[i] = xWavelet[i] + a * (xWavelet[i - 1] + xWavelet[i + 1]);
            }
            xWavelet[0] = xWavelet[0] + 2 * a * xWavelet[1];

            // Undo predict 1
            a = 1.586134342f;
            for (int i = 1; i < n - 1; i += 2)
            {
                xWavelet[i] = xWavelet[i] + a * (xWavelet[i - 1] + xWavelet[i + 1]);
            }
            xWavelet[n - 1] = xWavelet[n - 1] + 2 * a * xWavelet[n - 2];

            if(Side == WV_LEFT_TO_RIGHT)
            {
                for (int j = 0; j < n; j++)
                {
                    shorts[z, dwPos, j] = xWavelet[j];
                }
            }
            else if(Side == WV_TOP_TO_BOTTOM)
            {
                for(int i = 0; i < n; i++)
                {
                    shorts[z, i, dwPos] = xWavelet[i];
                }
            }
        }

        // Метод перекодирования YCrCb в RGB
        private byte[] YCrCbDecode(double[, ,] yuv, int w, int h, double Ydiv, double Udiv, double Vdiv)
        {
            byte[] bytes_flat = new byte[3 * w * h];
            double vr, vg, vb;
            double vY, vCb, vCr;
            Ydiv = Ydiv / 100f;
            Udiv = Udiv / 100f;
            Vdiv = Vdiv / 100f;
            for(int j = 0; j < h; j++)
            {
                for (int i = 0; i < w ; i++)
                {
                    vCr = yuv[0, i, j] / Vdiv;
                    vCb = yuv[1, i, j] / Udiv;
                    vY = yuv[2, i, j] / Ydiv;
                    vr = vY + 1.402f * (vCr - 128f);
                    vg = vY - 0.34414f * (vCb - 128f) - 0.71414f * (vCr - 128f);
                    vb = vY + 1.722f * (vCb - 128f);
                    if (vr > 255) {vr = 255;}
                    if (vg > 255) {vg = 255;}
                    if (vb > 255) {vb = 255;}
                    if (vr < 0) {vr = 0;}
                    if (vg < 0) {vg = 0;}
                    if (vb < 0) {vb = 0;}
                    bytes_flat[j * w * 3 + i * 3 + 0] = (byte)vb;
                    bytes_flat[j * w * 3 + i * 3 + 1] = (byte)vg;
                    bytes_flat[j * w * 3 + i * 3 + 2] = (byte)vr;
                }
            }
            return bytes_flat;
        }

    }
}
