using System;
using System.Security.Cryptography;


namespace WzComparerR2.Network
{
    public abstract class RC4 : SymmetricAlgorithm
    {
        protected RC4()
        {
        }

        public static new RC4 Create()
        {
            return new RC4CryptoServiceProvider();
        }
    }

    public sealed class RC4CryptoServiceProvider : RC4
    {
        public RC4CryptoServiceProvider()
        {
            this.LegalKeySizesValue = new[] { new KeySizes(1, 256, 1) };
            this.KeySize = DefaultKeyLength;
        }

        public const Int32 DefaultKeyLength = 8;

        public override ICryptoTransform CreateEncryptor(Byte[] rgbKey, Byte[] rgbIV)
        {
            this.Key = rgbKey;
            return new RC4CryptoTransform(this.Key);
        }

        public override ICryptoTransform CreateDecryptor(Byte[] rgbKey, Byte[] rgbIV)
        {
            return CreateEncryptor(rgbKey, rgbIV);
        }

        public override ICryptoTransform CreateEncryptor()
        {
            return new RC4CryptoTransform(this.Key);
        }

        public override ICryptoTransform CreateDecryptor()
        {
            return CreateEncryptor();
        }

        public override void GenerateKey()
        {
            var rnd = RandomNumberGenerator.Create();
            this.KeyValue = new Byte[this.KeySize];
            rnd.GetBytes(this.KeyValue);
        }

        public override void GenerateIV()
        {
            throw new CryptographicException("RC4 cipher do not support IV generation");
        }
    }

    public sealed class RC4CryptoTransform : ICryptoTransform
    {
        private Byte[] _rgbKey;

        // Bits, encrypted for one iteration
        public const Int32 BlockSizeInBits = 8;

        public const Int32 BlockSizeInBytes = BlockSizeInBits / 8;

        public const Int32 SBlockSize = 256;

        private Byte[] _sBlock;

        private Int32 _rndI = 0;
        private Int32 _rndJ = 0;

        public RC4CryptoTransform(byte[] rgbKey)
        {
            _rgbKey = rgbKey;

            _sBlock = new Byte[SBlockSize];

            Initialize();
        }

        private void Initialize()
        {
            var blockSize = SBlockSize;

            int keyLength = _rgbKey.Length;

            for (int i = 0; i < blockSize; i++)
            {
                _sBlock[i] = (byte)i;
            }

            int j = 0;

            for (int i = 0; i < blockSize; i++)
            {
                j = (j + _sBlock[i] + _rgbKey[i % keyLength]) % blockSize;

                _sBlock.Swap(i, j);
            }
        }

        private Byte GetNextPseudoRandomItem()
        {
            var blockSize = SBlockSize;

            _rndI = (_rndI + 1) % blockSize;
            _rndJ = (_rndJ + _sBlock[_rndI]) % blockSize;

            _sBlock.Swap(_rndI, _rndJ);

            return _sBlock[(_sBlock[_rndI] + _sBlock[_rndJ]) % blockSize];
        }

        public void Dispose()
        {
            Array.Clear(_rgbKey, 0, _rgbKey.Length);
            Array.Clear(_sBlock, 0, _sBlock.Length);

            _rgbKey = null;
            _sBlock = null;
        }

        public Int32 TransformBlock(Byte[] inputBuffer, Int32 inputOffset, Int32 inputCount, Byte[] outputBuffer, Int32 outputOffset)
        {
            for (long i = 0; i < inputCount; i++)
            {
                outputBuffer[outputOffset + i] = (byte)(inputBuffer[inputOffset + i] ^ GetNextPseudoRandomItem());
            }

            return inputCount;
        }

        public Byte[] TransformFinalBlock(Byte[] inputBuffer, Int32 inputOffset, Int32 inputCount)
        {
            var encryptedData = new Byte[inputCount];

            TransformBlock(inputBuffer, inputOffset, inputCount, encryptedData, 0);

            return encryptedData;
        }


        public Int32 InputBlockSize { get { return BlockSizeInBytes; } }

        public Int32 OutputBlockSize { get { return BlockSizeInBytes; } }

        public Boolean CanTransformMultipleBlocks { get { return false; } }

        public Boolean CanReuseTransform { get { return false; } }
    }

    internal static class ByteArrayExtensions
    {
        public static void Swap(this Byte[] array, int index1, int index2)
        {
            Byte temp = array[index1];
            array[index1] = array[index2];
            array[index2] = temp;
        }
    }
}
