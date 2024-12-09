using StreamFormatDecryptor;

namespace StreamFormatDecryptor;

public class SafeEncryptionProvider
{
    private uint[] k;
    private byte[] kB;
    private const uint d = 0x9e3779b9;
    private const uint r = 32;
    public fEnum.EncryptionMethod m = fEnum.EncryptionMethod.Four;

    #region Local array converter (Byte <-> Unsigned Integer)

    public static byte[] ConvertUIntArrayToByteArray(uint[] input)
    {
        byte[] output = new byte[input.Length * 4];
        Buffer.BlockCopy(input, 0, output, 0, output.Length);
        return output;
    }

    public static uint[] ConvertByteArrayToUIntArray(byte[] input)
    {
        if (input.Length % 4 != 0)
            throw new ArgumentException("Byte array length must be multiple of 4");

        uint[] output = new uint[input.Length / 4];
        Buffer.BlockCopy(input, 0, output, 0, input.Length);
        return output;
    }

    #endregion

    // Key has to be 4 words long and can't be null
    public void Init(uint[] pkey, fEnum.EncryptionMethod EM)
    {
        if (EM == fEnum.EncryptionMethod.Four)
            throw new ArgumentException("1"); //Encryption method can't be none
        if (pkey.Length != 4)
            throw new ArgumentException("2"); //Encryption key has to be 4 words long

        k = pkey;
        m = EM;
        kB = ConvertUIntArrayToByteArray(pkey);
    }

    private void CheckKey()
    {
        if (m == fEnum.EncryptionMethod.Four)
            throw new ArgumentException("Encryption method has to be set first");
    }

    #region Simple Bytes Encrypt/Decrypt

    private void SimpleEncryptBytesSafe(byte[] buf, int offset, int count)
    {
        byte prevE = 0; // previous encrypted
        for (int i = offset; i < count; i++)
        {
            buf[i] = unchecked((byte)((buf[i] + (kB[i % 16] >> 2)) % 256));
            buf[i] ^= RotateLeft(kB[15 - (i - offset) % 16], (byte)((prevE + count - i - offset) % 7));
            buf[i] = RotateRight(buf[i], (byte)((~(uint)(prevE)) % 7));

            prevE = buf[i];
        }
    }

    private void SimpleDecryptBytesSafe(byte[] buf, int offset, int count)
    {
        byte prevE = 0; // previous encrypted
        for (int i = offset; i < count; i++)
        {
            byte tmpE = buf[i];
            buf[i] = RotateLeft(buf[i], (byte)((~(uint)(prevE)) % 7));
            buf[i] ^= RotateLeft(kB[15 - (i - offset) % 16], (byte)((prevE + count - i - offset) % 7));
            buf[i] = unchecked((byte)((buf[i] - (kB[i % 16] >> 2) + 256) % 256));

            prevE = tmpE;
        }
    }

    #endregion

    #region Rotation

    private static byte RotateLeft(byte val, byte n)
    {
        return (byte)((val << n) | (val >> (8 - n)));
    }

    private static byte RotateRight(byte val, byte n)
    {
        return (byte)((val >> n) | (val << (8 - n)));
    }

    #endregion

    
    #region Encryption ONE
    
    private void EncryptDecryptOneSafe(byte[] bufferArr, byte[] resultArr, int bufferLen, bool isEncrypted, 
        int bufferPtr = 0, int resultPtr = 0) // Pointers always start at 0
    {
        uint fullWordCount = unchecked((uint)bufferLen / 8), 
            leftover = unchecked((uint)bufferLen) % 8;

        uint[] intWordArrB = ConvertByteArrayToUIntArray(bufferArr), 
            intWordArrO = ConvertByteArrayToUIntArray(resultArr);
        int intWordPtrB = 0;
        int intWordPtrO = 0;
        
        // Back up pointers by 2
        intWordPtrB -= 2;
        intWordPtrO -= 2;

        if (isEncrypted)
        {
            for (int wordCount = 0; wordCount < fullWordCount; wordCount++)
                EncryptWordOneSafe(intWordArrB, intWordArrO, intWordPtrB += 2, intWordPtrO += 2);
        }
        else
        {
            for (int wordCount = 0; wordCount < fullWordCount; wordCount++)
                DecryptWordOneSafe(intWordArrB, intWordArrO, intWordPtrB += 2, intWordPtrO += 2);
        }

        if (leftover == 0) return; // Where's my leftover :c

        byte[] bufferEnd = BitConverter.GetBytes(bufferArr[bufferPtr] + bufferLen);
        int bufferEndPtr = 0;
        byte[] byteWordArrB2 = BitConverter.GetBytes(bufferEnd[bufferEndPtr] - leftover);
        int byteWordPtrB2 = 0;
        byte[] byteWordArrO2 = BitConverter.GetBytes(resultArr[resultPtr] + bufferLen - leftover);
        int byteWordPtrO2 = 0;
        
        //copy leftover buffer array to result array
        Array.Copy(byteWordArrB2, byteWordPtrB2, byteWordArrO2, byteWordPtrO2, bufferEnd[bufferEndPtr]);
        
        // Deal with the leftover
        if (isEncrypted)
            SimpleEncryptBytesSafe(byteWordArrO2, (int)(byteWordPtrO2 - leftover), unchecked((int)leftover));
        else
            SimpleDecryptBytesSafe(byteWordArrO2, (int)(byteWordPtrO2 - leftover), unchecked((int)leftover));
    }
    
    #region Sub-Functions (Encrypt/Decrypt)

    private void EncryptWordOneSafe(uint[] v, uint[] o, int vPtr, int oPtr)
    {
        uint i;
        uint v0 = v[vPtr];
        uint v1 = v[vPtr + 1];
        uint sum = 0;
        for (i = 0; i < r; i++)
        {
            v0 += (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + k[sum & 3]);
            sum += d;
            v1 += (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + k[(sum >> 11) & 3]);
        }
        o[oPtr] = v0;
        o[oPtr + 1] = v1;
    }
    
    private void DecryptWordOneSafe(uint[] v, uint[] o, int vPtr, int oPtr)
    {
        uint i;
        uint v0 = v[vPtr];
        uint v1 = v[vPtr + 1];
        uint sum = unchecked(d * r);
        for (i = 0; i < r; i++)
        {
            v1 -= (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + k[(sum >> 11) & 3]);
            sum -= d;
            v0 -= (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + k[sum & 3]);
        }

        o[oPtr] = v0;
        o[oPtr + 1] = v1;
    }

    #endregion

    #endregion 
    
    #region Encryption TWO

    private uint _n;
    public const uint NMax = 16;
    public const uint NMaxBytes = NMax * 4;

    private void EncryptDecryptTwoSafe(byte[] bufferArr, bool isEncrypted, int count, int offset)
    {
        uint fullWordCount = unchecked((uint)count / NMaxBytes);
        uint leftover = unchecked((uint)count) % NMaxBytes;

        _n = NMax;
        uint rounds = 6 + 52 / _n;

        byte[] bufferCut = new byte[fullWordCount * NMaxBytes];
        Buffer.BlockCopy(bufferArr, offset, bufferCut, 0, (int)(fullWordCount * NMaxBytes));
        uint[] bufferCutWords = ConvertByteArrayToUIntArray(bufferCut);

        if (isEncrypted)
            for (uint wordCount = 0; wordCount < fullWordCount; wordCount++)
            {
                EncryptWordsTwoSafe(bufferCutWords, (int)(wordCount * NMax));
            }
        else //copy pasta because we dont want to waste time on a cmp each iteration
            for (uint wordCount = 0; wordCount < fullWordCount; wordCount++)
            {
                DecryptWordsTwoSafe(bufferCutWords, (int)(wordCount * NMax));
            }

        byte[] bufferProcessed = ConvertUIntArrayToByteArray(bufferCutWords);
        Buffer.BlockCopy(bufferProcessed, 0, bufferArr, offset, (int)(fullWordCount * NMaxBytes));

        _n = leftover / 4;
        byte[] leftoverBuffer = new byte[_n * 4];
        Buffer.BlockCopy(bufferArr, (int)(offset + fullWordCount * NMaxBytes), leftoverBuffer, 0, (int)_n * 4);
        uint[] leftoverBufferWords = ConvertByteArrayToUIntArray(leftoverBuffer);

        if (_n > 1)
        {
            if (isEncrypted)
                EncryptWordsTwoSafe(leftoverBufferWords, 0);
            else
                DecryptWordsTwoSafe(leftoverBufferWords, 0);

            leftover -= _n * 4;
            if (leftover == 0)
                return;
        }

        byte[] leftoverBufferProcessed = ConvertUIntArrayToByteArray(leftoverBufferWords);
        Buffer.BlockCopy(leftoverBufferProcessed, 0, bufferArr, (int)(offset + fullWordCount * NMaxBytes), (int)_n * 4);

        if (isEncrypted)
            SimpleEncryptBytesSafe(bufferArr, (int)(count - leftover) + offset, count);
        else
            SimpleDecryptBytesSafe(bufferArr, (int)(count - leftover) + offset, count);
    }

    #region Sub-Functions (Encrypt/Decrypt)

    private void EncryptWordsTwoSafe(uint[] v, int offset)
    {
        uint y, z, sum;
        uint p, e;
        uint rounds = 6 + 52 / _n;
        sum = 0;
        z = v[_n - 1 + offset];
        do
        {
            sum += d;
            e = (sum >> 2) & 3;
            for (p = 0; p < _n - 1; p++)
            {
                y = v[p + 1 + offset];
                z = v[p + offset] += ((z >> 5 ^ y << 2) + (y >> 3 ^ z << 4)) ^ ((sum ^ y) + (k[(p & 3) ^ e] ^ z));
            }

            y = v[offset];
            z = v[_n - 1 + offset] += ((z >> 5 ^ y << 2) + (y >> 3 ^ z << 4)) ^ ((sum ^ y) + (k[(p & 3) ^ e] ^ z));
        } while (--rounds > 0);
    }

    private void DecryptWordsTwoSafe(uint[] v, int offset)
    {
        if (v == null)
            throw new ArgumentNullException(nameof(v));
        if (k == null)
            throw new InvalidOperationException("Encryption key not initialized. Call Init method first.");

        uint y, z, sum;
        uint p, e;
        uint rounds = 6 + 52 / _n;
        sum = rounds * d;
        y = v[offset];
        do
        {
            e = (sum >> 2) & 3;
            for (p = _n - 1; p > 0; p--)
            {
                z = v[p - 1 + offset];
                y = v[p + offset] -= ((z >> 5 ^ y << 2) + (y >> 3 ^ z << 4)) ^ ((sum ^ y) + (k[(p & 3) ^ e] ^ z));
            }

            z = v[_n - 1 + offset];
            y = v[offset] -= ((z >> 5 ^ y << 2) + (y >> 3 ^ z << 4)) ^ ((sum ^ y) + (k[(p & 3) ^ e] ^ z));
        } while ((sum -= d) != 0);
    }

    #endregion

    #endregion
    
    #region Encryption HOMEBREW
    private void EncryptDecryptHomebrew(byte[] bufferArr, int offset,int bufferLen, bool isEncrypted)
    {
        if (isEncrypted)
            SimpleEncryptBytesSafe(bufferArr,offset, bufferLen);
        else
            SimpleDecryptBytesSafe(bufferArr,offset, bufferLen);
    }
    #endregion

    #region Encrypt/Decrypt Main

    private void EncryptDecryptSafe(byte[] bufferArr, int bufferLen, bool isEncrypted, 
        int bufferPtr = 0)
    {
        switch (m)
        {
            case fEnum.EncryptionMethod.One:
                EncryptDecryptOneSafe(bufferArr, bufferArr, bufferLen, isEncrypted, bufferPtr, bufferPtr);
                break;
            case fEnum.EncryptionMethod.Two:
                EncryptDecryptTwoSafe(bufferArr, isEncrypted, bufferLen, bufferPtr);
                break;
            case fEnum.EncryptionMethod.Three:
                EncryptDecryptHomebrew(bufferArr, bufferPtr, bufferLen, isEncrypted);
                break;
            case fEnum.EncryptionMethod.Four:
                CheckKey();
                break;
        }
    }

    private void EncryptDecryptSafe(byte[] bufferArr, byte[] outputArr, int bufferLen, bool isEncrypted,
        int bufferPtr = 0, int resultPtr = 0)
    {
        switch (m)
        {
            case fEnum.EncryptionMethod.One:
                EncryptDecryptOneSafe(bufferArr, outputArr, bufferLen, isEncrypted, bufferPtr, resultPtr);
                break;
            case fEnum.EncryptionMethod.Three:
            case fEnum.EncryptionMethod.Two:
                throw new NotSupportedException();
            case fEnum.EncryptionMethod.Four:
                CheckKey();
                break;
        }
    }
    

    #endregion
    
    #region Encrypt/Decrypt Methods

    #region Decrypt

    /**
    * Will be decrypted from and to the buffer.
    * Fastest if buffer size is a multiple of 8
    **/
    public void Decrypt(byte[] buffer)
    {
        EncryptDecryptSafe(buffer, buffer.Length, false, 0);
    }

    public void Decrypt(byte[] buffer, int start, int count)
    {
        EncryptDecryptSafe(buffer, count, false, start);
    }

    public void Decrypt(byte[] buffer, byte[] output)
    {
        EncryptDecryptSafe(buffer, output, buffer.Length, false, 0, 0);
    }

    public void Decrypt(byte[] buffer, byte[] output, int bufStart, int outStart, int count)
    {
        EncryptDecryptSafe(buffer, output, count, false, bufStart, outStart);
    }
    
    #endregion

    #region Encrypt

    /**
    * Will be encrypted from and to the buffer.
    * Fastest if buffer size is a multiple of 8
    **/
    public void Encrypt(byte[] buffer)
    {
        EncryptDecryptSafe(buffer, buffer.Length, true, 0);
    }

    public void Encrypt(byte[] buffer, int start, int count)
    {
        EncryptDecryptSafe(buffer, count, true, start);
    }

    public void Encrypt(byte[] buffer, byte[] output)
    {
        EncryptDecryptSafe(buffer, output, buffer.Length, true, 0, 0);
    }

    public void Encrypt(byte[] buffer, byte[] output, int bufStart, int outStart, int count)
    {
        EncryptDecryptSafe(buffer, output, count, true, bufStart, outStart);
    }


    #endregion
    
    #endregion
}