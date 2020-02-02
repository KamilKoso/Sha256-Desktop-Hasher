using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Text;

namespace Projekt_algorytm_hashujący_Sha_256__Kamil_Kosobudzki__Michał_Świerkot
{
    public class Sha256
    {

       readonly byte[] pendingBlock = new byte[64];
        uint pendingBlockOff = 0;
       readonly UInt32[] uint_buffer = new UInt32[16];

        UInt64 bitsProcessed = 0;

        bool closed = false;

        private static readonly UInt32[] K = new UInt32[64] {
            0x428A2F98, 0x71374491, 0xB5C0FBCF, 0xE9B5DBA5, 0x3956C25B, 0x59F111F1, 0x923F82A4, 0xAB1C5ED5,
            0xD807AA98, 0x12835B01, 0x243185BE, 0x550C7DC3, 0x72BE5D74, 0x80DEB1FE, 0x9BDC06A7, 0xC19BF174,
            0xE49B69C1, 0xEFBE4786, 0x0FC19DC6, 0x240CA1CC, 0x2DE92C6F, 0x4A7484AA, 0x5CB0A9DC, 0x76F988DA,
            0x983E5152, 0xA831C66D, 0xB00327C8, 0xBF597FC7, 0xC6E00BF3, 0xD5A79147, 0x06CA6351, 0x14292967,
            0x27B70A85, 0x2E1B2138, 0x4D2C6DFC, 0x53380D13, 0x650A7354, 0x766A0ABB, 0x81C2C92E, 0x92722C85,
            0xA2BFE8A1, 0xA81A664B, 0xC24B8B70, 0xC76C51A3, 0xD192E819, 0xD6990624, 0xF40E3585, 0x106AA070,
            0x19A4C116, 0x1E376C08, 0x2748774C, 0x34B0BCB5, 0x391C0CB3, 0x4ED8AA4A, 0x5B9CCA4F, 0x682E6FF3,
            0x748F82EE, 0x78A5636F, 0x84C87814, 0x8CC70208, 0x90BEFFFA, 0xA4506CEB, 0xBEF9A3F7, 0xC67178F2
        };


        private static UInt32 ROTR(UInt32 x, byte n)
        {
            Debug.Assert(n < 32);
            return (x >> n) | (x << (32 - n));
        }

        private static UInt32 Ch(UInt32 x, UInt32 y, UInt32 z)
        {
            return (x & y) ^ ((~x) & z);
        }

        private static UInt32 Maj(UInt32 x, UInt32 y, UInt32 z)
        {
            return (x & y) ^ (x & z) ^ (y & z);
        }

        //Sigma używana do pętli głównej

        private static UInt32 S0Main(UInt32 x)
        {
            return ROTR(x, 2) ^ ROTR(x, 13) ^ ROTR(x, 22);
        }

        private static UInt32 S1Main(UInt32 x)
        {
            return ROTR(x, 6) ^ ROTR(x, 11) ^ ROTR(x, 25);
        }

        //Sigma używana do rozszerzania z 16 el. do 64 elementowej tablicy

        private static UInt32 s0Extend(UInt32 x)
        {
            return ROTR(x, 7) ^ ROTR(x, 18) ^ (x >> 3);
        }

        private static UInt32 s1Extend(UInt32 x)
        {
            return ROTR(x, 17) ^ ROTR(x, 19) ^ (x >> 10);
        }


        private readonly UInt32[] H = new UInt32[8] {
            0x6A09E667, 0xBB67AE85, 0x3C6EF372, 0xA54FF53A, 0x510E527F, 0x9B05688C, 0x1F83D9AB, 0x5BE0CD19
        };


        private void ProcessBlock(UInt32[] M)
        {
            Debug.Assert(M.Length == 16);

            //Rozszerzanie 16 elementowej tablicy na 64 elementowa tablice
            UInt32[] W = new UInt32[64];
            for (int t = 0; t < 16; ++t)
            {
                W[t] = M[t];
            }

            for (int i = 16; i < 64; ++i)
            {
                W[i] = s1Extend(W[i - 2]) + W[i - 7] + s0Extend(W[i - 15]) + W[i - 16];
            }

            // Inicjalizacja wartosci skrótu dla kawałka
            UInt32 a = H[0],
                   b = H[1],
                   c = H[2],
                   d = H[3],
                   e = H[4],
                   f = H[5],
                   g = H[6],
                   h = H[7];

            // Pętla główna
            for (int i = 0; i < 64; ++i)
            {
                UInt32 T1 = h + S1Main(e) + Ch(e, f, g) + K[i] + W[i];
                UInt32 T2 = S0Main(a) + Maj(a, b, c);
                h = g;
                g = f;
                f = e;
                e = d + T1;
                d = c;
                c = b;
                b = a;
                a = T1 + T2;
            }

            // Dodawanie ten kawałek hasha do bieżącego rezulatu
            H[0] += a ;
            H[1] += b ;
            H[2] += c ;
            H[3] += d ;
            H[4] += e ;
            H[5] += f ;
            H[6] += g ;
            H[7] += h ;
        }

        public void AddData(byte[] data, uint offset, uint length)
        {
            if (closed)
                throw new InvalidOperationException("Nie można skorzystać z tej samej instancji hashera ponownie.");

            if (length == 0)
                return;

            bitsProcessed += length * 8;

            while (length > 0)
            {
                uint amountToCopy;

                if (length < 64)
                {
                    if (pendingBlockOff + length > 64)
                        amountToCopy = 64 - pendingBlockOff;
                    else
                        amountToCopy = length;
                }
                else
                {
                    amountToCopy = 64 - pendingBlockOff;
                }

                Array.Copy(data, offset, pendingBlock, pendingBlockOff, amountToCopy);
                length -= amountToCopy;
                offset += amountToCopy;
                pendingBlockOff += amountToCopy;

                if (pendingBlockOff == 64)
                {
                    ToUintArray(pendingBlock, uint_buffer);
                    ProcessBlock(uint_buffer);
                    pendingBlockOff = 0;
                }
            }
        }

        public ReadOnlyCollection<byte> GetHash()
        {
            return ToByteArray(GetHashUInt32());
        }

        public ReadOnlyCollection<UInt32> GetHashUInt32()
        {
            if (!closed)
            {
                UInt64 sizeTemp = bitsProcessed;

                AddData(new byte[1] { 128 }, 0, 1); // Bit 1 (2^7)

                uint availableSpace = 64 - pendingBlockOff;

                if (availableSpace < 8)
                    availableSpace += 64;

                
                byte[] padding = new byte[availableSpace];
                
                for (uint i = 1; i <= 8; ++i)
                {
                    padding[padding.Length - i] = (byte)sizeTemp;
                    sizeTemp >>= 8;
                }

                AddData(padding, 0u, (uint)padding.Length);

                Debug.Assert(pendingBlockOff == 0); 

                closed = true;
            }

            return Array.AsReadOnly(H);
        }

        private static void ToUintArray(byte[] src, UInt32[] dest)
        {
            for (uint i = 0, j = 0; i < dest.Length; ++i, j += 4)
            {
                dest[i] = ((UInt32)src[j + 0] << 24) | ((UInt32)src[j + 1] << 16) | ((UInt32)src[j + 2] << 8) | ((UInt32)src[j + 3]);
            }
        }

        private static ReadOnlyCollection<byte> ToByteArray(ReadOnlyCollection<UInt32> src)
        {
            byte[] dest = new byte[src.Count * 4];
            int position = 0;

            for (int i = 0; i < src.Count; ++i)
            {
                dest[position++] = (byte)(src[i] >> 24);
                dest[position++] = (byte)(src[i] >> 16);
                dest[position++] = (byte)(src[i] >> 8);
                dest[position++] = (byte)(src[i]);
            }

            return Array.AsReadOnly(dest);
        }

        public static ReadOnlyCollection<byte> HashFile(Stream fs, Action<double> progressCallBack)
        {
            Sha256 sha = new Sha256();
            byte[] buf = new byte[8196];
            long totalRead = 0;

            uint bytes_read;
            do
            {
                bytes_read = (uint)fs.Read(buf, 0, buf.Length);
                if (bytes_read == 0)
                    break;
                totalRead += bytes_read;
                progressCallBack(((double)totalRead/(double)fs.Length)*1000);
                sha.AddData(buf, 0, bytes_read);
            }while (bytes_read == 8196);

            return sha.GetHash();
        }

        public static ReadOnlyCollection<byte> HashString(string toHash)
        {
            Sha256 sha = new Sha256();
            sha.AddData(Encoding.ASCII.GetBytes(toHash), 0, (uint)toHash.Length);
            return sha.GetHash();
        }


        public static string ByteArrayToString(ReadOnlyCollection<byte> arr)
        {
            StringBuilder s = new StringBuilder(arr.Count * 2);
            for (int i = 0; i < arr.Count; ++i)
            {
                s.AppendFormat("{0:x2}", arr[i]);
            }

            return s.ToString();
        }
    }
}