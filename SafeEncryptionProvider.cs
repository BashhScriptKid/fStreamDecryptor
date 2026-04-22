using StreamFormatDecryptor;

namespace StreamFormatDecryptor;

public class SafeEncryptionProvider
{
    private static readonly bool TraceAlgorithmTwo = false;
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
        Console.WriteLine("Key: " + string.Join(" ", pkey));
        m = EM;
        Console.WriteLine("Encryption method: " + EM);
        kB = ConvertUIntArrayToByteArray(pkey);
        Console.WriteLine("Key bytes: " + string.Join(" ", kB));
    }

    private void CheckKey()
    {
        if (m == fEnum.EncryptionMethod.Four)
            throw new ArgumentException("Encryption method has to be set first");
    }

    #region Simple Bytes Encrypt/Decrypt

    private void SimpleEncryptBytesSafe(byte[] buf, int offset, int length)
    {
        byte prevE = 0; // previous encrypted
        for (int i = 0; i < length; i++)
        {
            int bufIdx = offset + i;
            buf[bufIdx] = unchecked((byte)((buf[bufIdx] + (kB[i % 16] >> 2)) % 256));
            buf[bufIdx] ^= RotateLeft(kB[15 - i % 16], (byte)((prevE + length - i) % 7));
            buf[bufIdx] = RotateRight(buf[bufIdx], (byte)((~(uint)(prevE)) % 7));

            prevE = buf[bufIdx];
        }
    }

    private void SimpleDecryptBytesSafe(byte[] buf, int offset, int length)
    {
        byte prevE = 0; // previous encrypted
        for (int i = 0; i < length; i++)
        {
            int bufIdx = offset + i;
            byte tmpE = buf[bufIdx];
            buf[bufIdx] = RotateLeft(buf[bufIdx], (byte)((~(uint)(prevE)) % 7));
            buf[bufIdx] ^= RotateLeft(kB[15 - i % 16], (byte)((prevE + length - i) % 7));
            buf[bufIdx] = unchecked((byte)((buf[bufIdx] - (kB[i % 16] >> 2) + 256) % 256));

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

    
    #region Encryption ONE (Disabled)
/**
    private void EncryptDecryptOneSafe(byte[] bufferArr, int bufferIdx, int bufferLen, bool isEncrypted)
    {
        uint fullWordCount = unchecked((uint)bufferLen / 8);
        uint leftover = (uint)(bufferLen % 8); //remaining of fullWordCount

        uint[] intWordArrB = ConvertByteArrayToUIntArray(bufferArr);

        intWordArrB -= 2;
        intWordArrO -= 2;

        if (isEncrypted)
            for (int wordCount = 0; wordCount < fullWordCount; wordCount++)
                EncryptWordOne(intWordArrB += 2, intWordArrO += 2);
        else
            for (int wordCount = 0; wordCount < fullWordCount; wordCount++)
                DecryptWordOne(intWordArrB += 2, intWordArrO += 2);

        if (leftover == 0) return; // no leftover for me? get lost :c

        byte[] bufferEnd = bufferArr + bufferLen;
        byte[] byteWordArrB2 = bufferEnd - leftover;
        byte[] byteWordArrO2 = ConvertUIntArrayToByteArray(resultArr + bufferLen - leftover); ;
        
        // copy leftoverBuffer[] -> result[]
        Array.Copy(byteWordArrB2, byteWordArrO2, leftover);

        // deal with leftover
        if (isEncrypted)
            SimpleEncryptBytesSafe(byteWordArrO2 - leftover, 0, unchecked((int)leftover));
        else
            SimpleDecryptBytesSafe(byteWordArrO2 - leftover, 0, unchecked((int)leftover));
    }
**/
    #region Sub-Functions (Encrypt/Decrypt)

/**
    private void EncryptWordOne(uint[] v  uint[] o)
    {
        uint i;
        uint v0 = v[0];
        uint v1 = v[1];
        uint sum = 0;
        for (i = 0; i < r; i++)
        {
            //todo: cache sum + k for better speed
            v0 += (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + k[sum & 3]);
            sum += d;
            v1 += (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + k[(sum >> 11) & 3]);
        }

        o[0] = v0;
        o[1] = v1;
    }

    private void DecryptWordOne(uint[] v , uint[] o )
    {
        uint i;
        uint v0 = v[0];
        uint v1 = v[1];
        uint sum = unchecked(d * r);
        for (i = 0; i < r; i++)
        {
            //todo: cache sum + k for better speed
            v1 -= (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + k[(sum >> 11) & 3]);
            sum -= d;
            v0 -= (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + k[sum & 3]);
        }

        o[0] = v0;
        o[1] = v1;
    }
**/

    #endregion

    #endregion 
    
    #region Encryption TWO

    public const uint NMax = 16;
    public const uint NMaxBytes = NMax * 4;

    private void EncryptDecryptTwoSafe(byte[] bufferArr, bool encrypt, int count, int offset)
    {
        uint fullWordCount = unchecked((uint)count / NMaxBytes);
        uint leftover = unchecked((uint)count) % NMaxBytes;

        uint rounds = 6 + 52 / NMax;

        if (TraceAlgorithmTwo)
        {
            Console.WriteLine("\n=== CRYPTO ALGORITHM TWO ===");
            Console.WriteLine("Full word count: " + fullWordCount);
            Console.WriteLine("Leftover: " + leftover);
            Console.WriteLine("Rounds: " + rounds);
            Console.WriteLine("Set max n to: " + NMax);
            Console.WriteLine($"Starting {(encrypt ? "encryption" : "decryption")}...");
        }

        byte[] bufferCut = new byte[fullWordCount * NMaxBytes];
        Buffer.BlockCopy(bufferArr, offset, bufferCut, 0, (int)(fullWordCount * NMaxBytes));
        uint[] bufferCutWords = ConvertByteArrayToUIntArray(bufferCut);

        if (encrypt)
            for (uint wordCount = 0; wordCount < fullWordCount; wordCount++)
            {
                EncryptWordsTwoSafe(bufferCutWords, (int)(wordCount * NMax), NMax);
            }
        else //copy pasta because we dont want to waste time on a cmp each iteration
            for (uint wordCount = 0; wordCount < fullWordCount; wordCount++)
            {
                DecryptWordsTwoSafe(bufferCutWords, (int)(wordCount * NMax), NMax);
            }
        
        byte[] bufferProcessed = ConvertUIntArrayToByteArray(bufferCutWords);
        Buffer.BlockCopy(bufferProcessed, 0, bufferArr, offset, (int)(fullWordCount * NMaxBytes));

        uint n_leftover = leftover / 4;
        byte[] leftoverBuffer = new byte[n_leftover * 4];
        Buffer.BlockCopy(bufferArr, (int)(offset + fullWordCount * NMaxBytes), leftoverBuffer, 0, (int)n_leftover * 4);
        uint[] leftoverBufferWords = ConvertByteArrayToUIntArray(leftoverBuffer);

        if (n_leftover > 1)
        {
            if (TraceAlgorithmTwo)
            {
                Console.WriteLine($"Starting leftover {(encrypt ? "encryption" : "decryption")}...");
            }

            if (encrypt)
                EncryptWordsTwoSafe(leftoverBufferWords, 0, n_leftover);
            else
                DecryptWordsTwoSafe(leftoverBufferWords, 0, n_leftover);
            
            leftover -= n_leftover * 4;

            if (leftover == 0)
            {
                if (TraceAlgorithmTwo)
                {
                    Console.WriteLine("Leftover is 0, no further byte pass required.");
                    Console.WriteLine("============================");
                }
                
                // Copy processed words back to main buffer before returning
                byte[] leftoverBufferProcessedWords = ConvertUIntArrayToByteArray(leftoverBufferWords);
                Buffer.BlockCopy(leftoverBufferProcessedWords, 0, bufferArr, (int)(offset + fullWordCount * NMaxBytes), (int)n_leftover * 4);
                return;
            }
        }

        byte[] leftoverBufferProcessed = ConvertUIntArrayToByteArray(leftoverBufferWords);
        Buffer.BlockCopy(leftoverBufferProcessed, 0, bufferArr, (int)(offset + fullWordCount * NMaxBytes), (int)n_leftover * 4);

        if (TraceAlgorithmTwo)
        {
            Console.WriteLine($"Starting final (simple) {(encrypt ? "encryption" : "decryption")}...");
        }

        if (encrypt)
            SimpleEncryptBytesSafe(bufferArr, (int)(count - leftover) + offset, (int)leftover);
        else
            SimpleDecryptBytesSafe(bufferArr, (int)(count - leftover) + offset, (int)leftover);

        if (TraceAlgorithmTwo)
        {
            Console.WriteLine($"Final {(encrypt ? "encryption" : "decryption")} done.");
        }
    }

    #region Sub-Functions (Encrypt/Decrypt)

    private void EncryptWordsTwoSafe(uint[] v, int offset, uint n)
    {
        uint y, z, sum;
        uint p, e;
        uint rounds = 6 + 52 / n;
        sum = 0;
        z = v[n - 1 + offset];
        do
        {
            sum += d;
            e = (sum >> 2) & 3;
            for (p = 0; p < n - 1; p++)
            {
                y = v[p + 1 + offset];
                z = v[p + offset] += ((z >> 5 ^ y << 2) + (y >> 3 ^ z << 4)) ^ ((sum ^ y) + (k[(p & 3) ^ e] ^ z));
            }

            y = v[offset];
            z = v[n - 1 + offset] += ((z >> 5 ^ y << 2) + (y >> 3 ^ z << 4)) ^ ((sum ^ y) + (k[(p & 3) ^ e] ^ z));
        } while (--rounds > 0);
    }

    private void DecryptWordsTwoSafe(uint[] v, int offset, uint n)
    {
        if (v == null)
            throw new ArgumentNullException(nameof(v));
        if (k == null)
            throw new InvalidOperationException("Encryption key not initialized. Call Init method first.");

        uint y, z, sum;
        uint p, e;
        uint rounds = 6 + 52 / n;
        sum = rounds * d;
        y = v[offset];
        do
        {
            e = (sum >> 2) & 3;
            for (p = n - 1; p > 0; p--)
            {
                z = v[p - 1 + offset];
                y = v[p + offset] -= ((z >> 5 ^ y << 2) + (y >> 3 ^ z << 4)) ^ ((sum ^ y) + (k[(p & 3) ^ e] ^ z));
            }

            z = v[n - 1 + offset];
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
    private void EncryptDecryptSafe(byte[] bufferArr, byte[] outputArr, int bufStart, int outputStart, int bufferLength, bool encrypt)
{
    switch (m)
    {
        case fEnum.EncryptionMethod.One:
            EncryptDecryptOneSafe(bufferArr, outputArr, bufStart, outputStart, bufferLength, encrypt);
            break;

        case fEnum.EncryptionMethod.Two:
            // EncryptDecryptTwoSafe handles its own word conversion and simple cipher steps
            EncryptDecryptTwoSafe(bufferArr, encrypt, bufferLength, bufStart); 
            break;
        case fEnum.EncryptionMethod.Three:
            EncryptDecryptHomebrew(bufferArr, bufStart, bufferLength, encrypt);
            break;

        case fEnum.EncryptionMethod.Four:
            //checkKey();
            break;
    }
}
private void EncryptDecryptOneSafe(byte[] bufferArr, byte[] outputArr, int bufStart, int outputStart, int bufferLength, bool encrypt)
{
    uint fullWordCount = unchecked((uint)bufferLength / 8);

    // We need to work with a segment of the array.
    // For simplicity, we can copy to a temporary array if not already aligned.
    byte[] inputSegment = new byte[fullWordCount * 8];
    Buffer.BlockCopy(bufferArr, bufStart, inputSegment, 0, inputSegment.Length);

    uint[] words = ConvertByteArrayToUIntArray(inputSegment);
    uint[] outputWords = new uint[words.Length];

    if (encrypt)
    {
        for (int i = 0; i < fullWordCount; i++)
            EncryptWordOneSafe(words, outputWords, i * 2);
    }
    else
    {
        for (int i = 0; i < fullWordCount; i++)
            DecryptWordOneSafe(words, outputWords, i * 2);
    }

    byte[] processedBytes = ConvertUIntArrayToByteArray(outputWords);
    Buffer.BlockCopy(processedBytes, 0, outputArr ?? bufferArr, outputArr != null ? outputStart : bufStart, (int)(fullWordCount * 8));
}
private void EncryptWordOneSafe(uint[] v, uint[] o, int offset)
{
    uint i;
    uint v0 = v[offset];
    uint v1 = v[offset + 1];
    uint sum = 0;
    for (i = 0; i < r; i++)
    {
        v0 += (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + k[sum & 3]);
        sum += d;
        v1 += (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + k[(sum >> 11) & 3]);
    }
    o[offset] = v0;
    o[offset + 1] = v1;
}

private void DecryptWordOneSafe(uint[] v, uint[] o, int offset)
{
    uint i;
    uint v0 = v[offset];
    uint v1 = v[offset + 1];
    uint sum = unchecked(d * r);
    for (i = 0; i < r; i++)
    {
        v1 -= (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + k[(sum >> 11) & 3]);
        sum -= d;
        v0 -= (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + k[sum & 3]);
    }
    o[offset] = v0;
    o[offset + 1] = v1;
}

    public void EncryptDecrypt(byte[] buffer, byte[] output, int bufStart, int outputStart, int count,
        bool encrypt)
    {
        EncryptDecryptSafe(buffer, output, bufStart, outputStart, count, encrypt);
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
        EncryptDecrypt(buffer, null, 0, 0, buffer.Length, false);
    }

    public void Decrypt(byte[] buffer, int start, int count)
    {
        EncryptDecrypt(buffer, null, start, 0, count, false);
    }

    public void Decrypt(byte[] buffer, byte[] output)
    {
        EncryptDecrypt(buffer, output, 0, 0, buffer.Length, false);
    }

    public void Decrypt(byte[] buffer, byte[] output, int bufStart, int outStart, int count)
    {
        EncryptDecrypt(buffer, output, bufStart, outStart, count, false);
    }
    
    #endregion

    #region Encrypt

    /**
    * Will be encrypted from and to the buffer.
    * Fastest if buffer size is a multiple of 8
    **/
    public void Encrypt(byte[] buffer)
    {
        EncryptDecrypt(buffer, null, 0, 0, buffer.Length, true);
    }

    public void Encrypt(byte[] buffer, int start, int count)
    {
        EncryptDecrypt(buffer, null, start, 0, count, true);
    }

    public void Encrypt(byte[] buffer, byte[] output)
    {
        EncryptDecrypt(buffer, output, 0, 0, buffer.Length, true);
    }

    public void Encrypt(byte[] buffer, byte[] output, int bufStart, int outStart, int count)
    {
        EncryptDecrypt(buffer, output, bufStart, outStart, count, true);
    }


    #endregion
    
    #endregion
}
