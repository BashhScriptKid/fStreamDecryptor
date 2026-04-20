using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace StreamFormatDecryptor
{

    public class Hasher{
	    public const bool UseLegacyOsz2Key = false;
	    public const bool UseOsf2KeyForOsz2 = true;

      public static byte[] CreateMD5(byte[] input)
	    {
	    // Use input string to calculate MD5 hash
	        using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
	        {
	            byte[] hashBytes = md5.ComputeHash(input);
			    return hashBytes;
	        }
	    }

	    public byte[] AESDecryptKey(string ArtistName, string BeatmapSetID, string Mapper, string SongTitle, bool is_osz2){

		    string KeyAlg = "";

	   		if (is_osz2 && !UseOsf2KeyForOsz2)
	    	{
			    KeyAlg = (UseLegacyOsz2Key ? (char)0x08 : string.Empty) + Mapper + "yhxyfjo5" + BeatmapSetID;
				Console.WriteLine("Using key seed from osz2: " + KeyAlg);
    		}
            else
            {
				KeyAlg = (char)0x08 + SongTitle + "4390gn8931i" + ArtistName;
				Console.WriteLine("Using key seed from osf2/mixed: " + KeyAlg);
            }

			byte[] Key = CreateMD5(Encoding.ASCII.GetBytes(KeyAlg))!;

			return Key;
		}
    }
}