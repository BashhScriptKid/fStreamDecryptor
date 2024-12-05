using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Transactions;

namespace StreamFormatDecryptor
{
	public class Program
	{
		public static bool isRunning = true;

		public static string? filePath = String.Empty;
		
		private static int fOffsetData;
		
		private static Dictionary<string, FileInfoStruct.FileInfos> fFiles;

		public static void ContinueOnPress()
		{
			Console.ReadLine();
		}


		private static (string? theIssue, bool isInvalid) CheckPathValidity(string? filePath)
		{
			bool isInvalid = false;
			string checkerResult = "Looks fine here but somehow we still got triggered?";

			if (string.IsNullOrWhiteSpace(filePath))
			{
				checkerResult = "Are you even putting anything there? It's as simple as dragging it to the console.";
				ContinueOnPress();
				isInvalid = true;
			}

			if (!File.Exists(filePath)){
				checkerResult = "The file does not exist. Check your file path and try again...unless it's not even a file path to begin with.";
				ContinueOnPress();
				isInvalid = true;
			}

			if (isInvalid)
				return (checkerResult, isInvalid);
				Console.Clear(); //return to the input

			return (null, false);

		}

		public static string? RequestPath (){
			Console.Write("Insert osu!stream beatmap file (osz2/osf2) to decrypt: ");

			string? filePath = Convert.ToString(Console.ReadLine()?.Replace("\"", string.Empty));

			(string? result, bool isWrong) = CheckPathValidity(filePath);
			if (isWrong == true) {Console.WriteLine(result);} // We already did a null check in another function so it can be safely ignored
			
			return filePath;
		}

		public static bool[] CheckFileFormat(string? filePath)
		{
			bool isInvalidFormat = false;
			bool isOsz2 = Path.GetExtension(filePath) switch
			{
				".osz2" => true,
				".osf2" => false,
				_ => isInvalidFormat = true
			};
			return [isInvalidFormat, isOsz2];
		}

		public static void Main(string[] args)
		{
			if (Environment.IsPrivilegedProcess)
			{
				Console.WriteLine("This program is being run as administrator. This is not recommended under normal circumstances.");
				Console.WriteLine("If it needs to, under specific reasons; note that file dragging won't work. Alternatively, right click on the file while holding SHIFT and click \"Copy as Path\".");
			}

			// filePath = RequestPath();

			filePath = "C:\\Users\\Windows\\Documents\\! Codes\\fStreamDecryptor\\Cranky - Dee Dee Cee (Deed).osz2"; // This is for debugging convenience purposes ONLY; Revert to L81 when done.

			bool isOsz2 = CheckFileFormat(filePath)[1];

			if (CheckFileFormat(filePath)[0] == true)
			{
				Console.WriteLine("Invalid file format. Please try again.");
				ContinueOnPress();
				filePath = RequestPath();
			}

			Console.WriteLine($"File name: {Path.GetFileName(filePath)}");
			Console.WriteLine($"File format: {Path.GetExtension(filePath)}");

			if (filePath != null)
			{
				using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
				var fileData = new byte[fileStream.Length];
				var read = fileStream.Read(fileData, 0, fileData.Length);

				var fileMeta = new fMetadata().Fetcher(fileStream);
				var fileHash = new fMetadata().ReadHeader(fileStream);

				if (fileMeta == null)
				{
					Console.WriteLine("One or more metadatas are missing!");
					ContinueOnPress();
					return;
				}
				
				Console.WriteLine($"\nFile size: {fileStream.Length} bytes");

				Console.WriteLine($"File metadata: \n	Title: {fileMeta[0]} \n		Artist: {fileMeta[1]} \n	Mapper: {fileMeta[2]} \n	Beatmap ID: {fileMeta[3]} \n");


				byte[] keyRaw = new Hasher().AESDecryptKey(fileMeta[1], fileMeta[3], fileMeta[2], fileMeta[0], isOsz2);
				string keyOut = Encoding.ASCII.GetString(keyRaw);
				
				Console.WriteLine($"Decryption key ({Path.GetExtension(filePath)}): {keyOut}");
				string key = keyOut.ToLower().Replace("-", string.Empty);
				
				DecryptFile(fileStream, fEnum.fDecryptMode.OSUM);
				BinaryReader br = new BinaryReader(fileStream); // Put a reader on decrypted fileStream
				
				
				Console.Write("\n\nDecrypted. Extract to folder or osz file? [folder/osz/none]: ");
				string? outputFormatPrompt = Console.ReadLine();

				#region Decryption
				// Read length
				int fileLen = br.ReadInt32();
				for (int i = 0; i < 16; i += 2)
					fileLen -= fileHash[2][i] | (fileHash[2][i + 1] << 17);

		
		
				// Read file to mem
				byte[] fileInfo = br.ReadBytes(fileLen);
				
				// Set offset
				int fDataOffset = (int)br.BaseStream.Position;
				// Decode IV
				{
					for (int i = 0; i < fileHash[0].Length; i++) 
						fileHash[0][i] ^= fileHash[3][i % 16];
				}
				using (Aes aes = new AesManaged())
				{
					aes.IV = fileHash[0];
					aes.Key = keyRaw;
					uint[] keyRawUInt = SafeEncryptionProvider.ConvertByteArrayToUIntArray(keyRaw);
					
					using (MemoryStream fileBuffer = new MemoryStream(fileHash[2]))
					using (Stream cstream = new FastEncryptorStream(fileStream, fEnum.EncryptionMethod.Two, keyRawUInt))
					using (BinaryReader reader = new BinaryReader(fileBuffer))
					{
						// Read encrypted count
						int count = reader.ReadInt32();
						
						//Check Hash
						byte[] hash = GetOszHash(fileHash[2], count * 4, 0xd1);
						if (Comparer.Default.Compare(hash, fileHash[2]) != 0)
							throw new IOException("File failed integrity check.");
						
						// Add file and offset to dict
						int offset_cur = reader.ReadInt32();
						for (int i = 0; i < count; i++)
						{
							string name = reader.ReadString();
							byte[] fileHashes = reader.ReadBytes(16);
							DateTime fileDateCreated = DateTime.FromBinary(reader.ReadInt64());
							DateTime fileDateModified = DateTime.FromBinary(reader.ReadInt64());

							// get next offset in order to calculate length of file
							int offset_next;
							if (i + 1 < count)
								offset_next = reader.ReadInt32();
							else
								offset_next = (int)br.BaseStream.Length - fOffsetData;

							int fileLength = offset_next - offset_cur;

							fFiles.Add(name, new FileInfoStruct.FileInfos(name, offset_cur, fileLength, fileHashes, fileDateCreated, fileDateModified));

							offset_cur = offset_next;
						}
						reader.Close();
					}
					aes.Clear();
				}
				fileStream.Seek(0, SeekOrigin.Begin);
				
				#endregion
				
				switch (outputFormatPrompt)
				{
					case "folder":
						break;
					case "osz":
						break;
					default:
						return;
				}


				fileData = null;
				fileStream.Dispose();
			}

			GC.Collect();
			Console.WriteLine("File unloaded."); ;
			// ContinueOnPress(); filePath = RequestPath(); //repeat process
		}



		private static byte[] DecryptFile(Stream fileStream, fEnum.fDecryptMode mode)
		{
			byte[] fileBuffer = ConvertToByteArray(fileStream); 
			
			var algorithmProvider = new SafeEncryptionProvider();
			
			// BRO HARDCODED BEATMAPS TO ENCRYPTION.TWO
			algorithmProvider.Decrypt(fileBuffer, 0, fileBuffer.Length);

			return fileBuffer;
		}
		
		
		#region FileStream <-> Byte[] conversion
		public static byte[] ConvertToByteArray(Stream fileStream)
		{
			using (MemoryStream memoryStream = new MemoryStream())
			{
				fileStream.CopyTo(memoryStream);
				return memoryStream.ToArray();
			}
		}
		#endregion

		#region OSZ Hash check
		private static byte[] GetOszHash(byte[] buffer, int pos, byte swap)
		{
			try
			{
				buffer[pos] ^= swap;
				byte[] hash = Hasher.CreateMD5(buffer);
				buffer[pos] ^= swap;

				for (int i = 0; i < 8; i++)
				{
					byte a = hash[i];
					hash[i] = hash[i + 8];
					hash[i + 8] = a;
				}

				hash[5] ^= 0x2d;

				//return fHasher.ComputeHash(hash);
				return hash;
			}
			catch (Exception)
			{
				throw new IOException("File failed integrity check.");
			}
		}
		#endregion



	}
}