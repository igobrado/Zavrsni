using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Buffers;
using System.IO;
using Force.Crc32;
using DamienG.Security.Cryptography;

namespace FileHashBackend
{
    public enum HasherType
    {
        SHA256,
        MD5,
        SHA1,
        CRC32,
        CRC64,
        Invalid

    }
    /// <summary>
    /// Custom event arguments which provides a progress of the current operation.
    /// </summary>
    public class IncreasedPercentage : EventArgs
    {
        public IncreasedPercentage(float percentage)
        {
            Percentage = percentage;
        }
        public float Percentage { get; set; }
    }

    /// <summary>
    /// Provides an abstraction around hashing functions.
    /// </summary>
    public class Hasher : IDisposable
    {
        public EventHandler<IncreasedPercentage> HashProgress;
        public Hasher(HasherType wantedHasherType)
        {
            switch (wantedHasherType)
            {
                case HasherType.SHA256:
                    _hashAlgorithm = SHA256.Create();
                    break;
                case HasherType.MD5:
                    _hashAlgorithm = MD5.Create();
                    break;
                case HasherType.SHA1:
                    _hashAlgorithm = SHA1.Create();
                    break;
                case HasherType.CRC32:
                    _hashAlgorithm = new Force.Crc32.Crc32Algorithm();
                    break;
                case HasherType.CRC64:
                    _hashAlgorithm = new Crc64Iso();
                    break;
                default:
                    Debug.Assert(false, "Invalid hasher type received");
                    break;
            }
        }

        public string GetHash(List<System.IO.Stream> streams, ulong streamSize)
        {
            var block = ArrayPool<byte>.Shared.Rent(0);
            float progress = 0;
            foreach (var stream in streams)
            {
                block = ArrayPool<byte>.Shared.Rent(8192);

                int length;
                while ((length = stream.Read(block, 0, block.Length)) > 0)
                {
                    _hashAlgorithm.TransformBlock(block, 0, length, null, 0);

                    progress += length;
                    float completedPercentage = (progress / streamSize) * 100;

                    OnUserUpdate(new IncreasedPercentage(completedPercentage));
                }
            }
            _hashAlgorithm.TransformFinalBlock(block, 0, 0);

            return BitConverter.ToString(_hashAlgorithm.Hash).Replace("-", "");
        }

        /// <summary>
        /// Gets the hash of the providen files.
        /// </summary>
        /// <param name="files"></param>
        /// 
        /// <returns> Tuple which contains pair - <HASH, Size of files combined in MB> </returns>
        public Tuple<string, float> GetHash(List<string> files)
        {
            if (files.Count == 0)
            {
                return new Tuple<string, float>("", 0);
            }

            long streamSize = 0;

                var streams = new List<System.IO.Stream>();
            foreach (var item in files)
            {
                var file = System.IO.File.OpenRead(item);
                streamSize += file.Length;
                streams.Add(file);
            }

            var result = new Tuple<string, float>(GetHash(streams, (ulong) streamSize), (streamSize / 1048576F));

            // files are needing to be disposed, as noone can use it until garbage
            // collector comes into play
            foreach (var item in streams)
            {
                try
                {
                    item.Dispose();
                }
                catch (Exception)
                {
                }
            }

            return result;
        }

        public void Dispose()
        {
            _hashAlgorithm.Dispose();
        }

        protected virtual void OnUserUpdate(IncreasedPercentage e)
        {
            EventHandler<IncreasedPercentage> raiseEvent = HashProgress;
            if (raiseEvent != null)
            {
                raiseEvent(this, e);
            }
        }

        private HashAlgorithm _hashAlgorithm;
    }
}
 