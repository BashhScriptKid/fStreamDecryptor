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
      public static string CreateMD5(string input)
	    {
	    // Use input string to calculate MD5 hash
	        using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
	        {
	            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
	            byte[] hashBytes = md5.ComputeHash(inputBytes);
			    return BitConverter.ToString(hashBytes);
	        }
	    }

	    public string AESDecryptKey(string ArtistName, string BeatmapSetID, string Mapper, string SongTitle, bool is_osz2){

		    string KeyAlg = "";

	   		switch (is_osz2)
	    	{
		    	case true:
			    	KeyAlg = (char)0x08 + Mapper + "yhxyfjo5" + BeatmapSetID;
				    break;

			    case false:
				    KeyAlg = (char)0x08 + SongTitle + "4390gn8931i" + ArtistName;
    			    break;
    		}

			string Key = Convert.ToString(CreateMD5(KeyAlg))!;

			return Key;
		}
    }
}