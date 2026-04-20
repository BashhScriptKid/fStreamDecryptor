using System.Collections;
using System.Security.Cryptography;
using StreamFormatDecryptor;

// ReSharper disable InconsistentNaming

namespace fStreamDecryptor
{
	public class Program
	{
		public static bool isRunning = true;

		public static string? filePath = String.Empty;

		public static byte[] keyRaw;
		
		private static int fOffsetData;
		
		public static int fOffsetFileinfo;
		
		private static long brOffset;

		static byte[] fileHash_iv;
		static byte[] fileHash_meta;
		static byte[] fileHash_info;
		static byte[] fileHash_body;
		
		
		private static Dictionary<string, FileInfoStruct.FileInfos> fFiles = new Dictionary<string, FileInfoStruct.FileInfos>(StringComparer.OrdinalIgnoreCase);

		public static FileStream fileStream;

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

			filePath = "/home/bashh/Documents/! Codes/fStreamDecryptor/Cranky - Dee Dee Cee (Deed).osz2"; // This is for debugging convenience purposes ONLY; Revert to L81 when done.

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
				fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
				var fileData = new byte[fileStream.Length];
				var read = fileStream.Read(fileData, 0, fileData.Length);

				string[]? fileMeta = new fMetadata().Fetcher(fileStream);
				
					string Title = fileMeta![0];
					string TitleUnicode = fileMeta[1];
					string Artist = fileMeta[2];
					string ArtistUnicode = fileMeta[3];
					string ArtistFullName = fileMeta[4];
					string ArtistURL = fileMeta[5];
					string ArtistTwitter = fileMeta[6];
					string Mapper = fileMeta[7];
					string Version = fileMeta[8];
					string BeatmapSetID = fileMeta[9];
					string Source = fileMeta[10];
					string Tags = fileMeta[11];
					string VideoOffset = fileMeta[12];
					string VideoLength = fileMeta[13];
					string VideoHash = fileMeta[14];
					string Genre = fileMeta[15];
					string Language = fileMeta[16];
					string UnknownMetadata = fileMeta[17];
					string PackID = fileMeta[18];
					string Revision = fileMeta[19];
				
				
				byte[][] fileHash = new fMetadata().ReadHeader(fileStream, true);
					fileHash_iv = fileHash[0];
					fileHash_meta = fileHash[1];
					fileHash_info = fileHash[2];
					fileHash_body = fileHash[3];

				if (fileMeta == null)
				{
					Console.WriteLine("One or more metadatas are missing!");
					ContinueOnPress();
					return;
				}
				
				Console.WriteLine($"\nFile size: {fileStream.Length} bytes");

				Console.WriteLine("File metadata:");
				Console.WriteLine($"    Title: {Title ?? "-"}");
				Console.WriteLine($"    Title (Unicode): {TitleUnicode ?? "-"}");
				Console.WriteLine($"    Artist: {Artist ?? "-"}");
				Console.WriteLine($"    Artist (Unicode): {ArtistUnicode ?? "-"}");
				Console.WriteLine($"    Artist (Full Name): {ArtistFullName ?? "-"}");
				Console.WriteLine($"    Artist URL: {ArtistURL ?? "-"}");
				Console.WriteLine($"    Artist Twitter: {ArtistTwitter ?? "-"}");
				Console.WriteLine($"    Mapper: {Mapper ?? "-"}");
				Console.WriteLine($"    Version: {Version ?? "-"}");
				Console.WriteLine($"    Beatmap ID: {BeatmapSetID ?? "-"}");
				
				// Additional metadata if available
				Console.WriteLine($"    Source: {Source ?? "-"}");
				Console.WriteLine($"    Tags: {Tags ?? "-"}");
				Console.WriteLine($"    Video Data Offset: {VideoOffset ?? "-"}");
				Console.WriteLine($"    Video Data Length: {VideoLength ?? "-"}");
				Console.WriteLine($"    Video Hash: {VideoHash ?? "-"}");
				Console.WriteLine($"    Genre: {Genre ?? "-"}");
				Console.WriteLine($"    Language: {Language ?? "-"}");
				Console.WriteLine($"    Unknown Metadata: {UnknownMetadata ?? "-"}");
				Console.WriteLine($"    Pack ID: {PackID ?? "-"}");
				Console.WriteLine();
				
				
				keyRaw = new Hasher().AESDecryptKey(fileMeta[2], fileMeta[9], fileMeta[7], fileMeta[0], isOsz2);
				string? keyOut = Convert.ToHexString(keyRaw);
				
				Console.WriteLine($"Decryption key: {keyOut}");

				string? outputFormatPrompt = null;
				//Console.Write("\n\nDecrypted. Extract to folder or osz file? [folder/osz/none]: ");
				//outputFormatPrompt = Console.ReadLine();

				#region Decryption

				Console.WriteLine($"\noffsetPostMetadata: {fMetadata.offsetPostMetadata}");

				using (BinaryReader br = new BinaryReader(fileStream))
				{ // Reset position to after metadata
					br.BaseStream.Position = (int)fMetadata.offsetPostMetadata;
					Console.WriteLine($"br position after seek to offsetPostMetadata: {br.BaseStream.Position}");

					// Read map count and map entries (matches original MapPackage.cs)
					{
						int mapCount = br.ReadInt32();
						Console.WriteLine($"mapCount: {mapCount}");

						for (int i = 0; i < mapCount; i++)
						{
							string mapName = br.ReadString();
							int mapId = br.ReadInt32();
							Console.WriteLine($"  map[{i}]: name={mapName} id={mapId}");
						}

						Console.WriteLine($"br position before doPostProcessing: {br.BaseStream.Position}");
						doPostProcessing(br);
					}
				}
				
				static void doPostProcessing(BinaryReader br)
				{
					// Set offset on FileInfo
					fOffsetFileinfo = (int)br.BaseStream.Position;

					// Read and decrypt 64 bytes of known plaintext (matches original MapPackage.cs)
					// This advances the stream position by 64 bytes
					Console.WriteLine($"Before 64-byte read, position: {br.BaseStream.Position}");
					
					byte[] knownPlain = new byte[64];
					new FastRandom(1990).NextBytes(knownPlain);
					Console.WriteLine($"[DEBUG] FastRandom(1990) 64 bytes: {BitConverter.ToString(knownPlain)}");

					try
					{
						uint[] keyRawUIntForCheck = SafeEncryptionProvider.ConvertByteArrayToUIntArray(keyRaw);
						using (Stream cstream = new FastEncryptorStream(br.BaseStream, fEnum.EncryptionMethod.One, keyRawUIntForCheck))
						{
							byte[] decryptedPlain = new byte[64];
							int bytesRead = cstream.Read(decryptedPlain, 0, 64);
							Console.WriteLine($"Read {bytesRead} bytes of encrypted data (position now: {br.BaseStream.Position})");
							
							if (!decryptedPlain.SequenceEqual(knownPlain))
							{
								Console.WriteLine("Decrypted plain does not match known plain!");
								Console.WriteLine($"Decrypted: {BitConverter.ToString(decryptedPlain)}");
								Console.WriteLine($"Expected:  {BitConverter.ToString(knownPlain)}");
								throw new Exception("Invalid key");
							}
							Console.WriteLine("Key verification successful (Known Plain matches).");
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Error during key verification: {ex.Message}");
						throw;
					}

					// Read length
					Console.WriteLine("fileStream position:" + br.BaseStream.Position);
					int encodedLength = br.ReadInt32();
					Console.WriteLine($"Encoded Length (Initial): {encodedLength}");
					

					for (int i = 0; i < 16; i += 2)
					{
						encodedLength -= fileHash_info[i] | (fileHash_info[i + 1] << 17);
						Console.WriteLine(
							$"Encoded Length (Iteration {i}): {encodedLength} (Reduction by {fileHash_info[i]} OR {fileHash_info[i + 1] << 17} = {fileHash_info[i] | (fileHash_info[i + 1] << 17)})");
					}

					Console.WriteLine("Encoded Length (Final):" + encodedLength);




					// Read file to mem
					byte[] fileInfo = br.ReadBytes(encodedLength);

					// Set global offset to file data (needed for last-file length calculation)
					fOffsetData = (int)br.BaseStream.Position;
					// Decode IV
					{
						for (int i = 0; i < fileHash_iv.Length; i++)
							fileHash_iv[i] ^= fileHash_body[i % 16];
					}
					// fileInfo is the encrypted file listing; decrypt with XXTEA (SafeEncryptionProvider)
					uint[] keyRawUInt = SafeEncryptionProvider.ConvertByteArrayToUIntArray(keyRaw);
					SafeEncryptionProvider algorithmProvider = new SafeEncryptionProvider();
					algorithmProvider.Init(keyRawUInt, fEnum.EncryptionMethod.Two);
					algorithmProvider.Decrypt(fileInfo);

					using (MemoryStream fileBuffer = new MemoryStream(fileInfo))
					using (BinaryReader reader = new BinaryReader(fileBuffer))
						{
							// Read encrypted count
							int count = reader.ReadInt32();
							Console.WriteLine($"file count (from decrypted fileInfo): {count}");


						//Check Hash: compute over content (fileInfo), compare against header hash (fileHash_info)
						byte[] hash = GetOszHash(fileInfo, count * 4, 0xd1);
						Console.WriteLine($"computed hash: {BitConverter.ToString(hash)}");
						Console.WriteLine($"expected hash: {BitConverter.ToString(fileHash_info)}");
						if (!hash.SequenceEqual(fileHash_info))
							throw new IOException("File failed integrity check.");

						Console.WriteLine($"Files found ({count}):");
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

							fFiles.Add(name,
								new FileInfoStruct.FileInfos(name, offset_cur, fileLength, fileHashes,
									fileDateCreated, fileDateModified));
							Console.WriteLine($"	{i + 1}: {fFiles.Keys}: {fFiles.Values}");

							offset_cur = offset_next;
						}

						reader.Close();
					}

					// aes.Clear(); // aes was not defined in scope
							}


							#endregion

							switch (outputFormatPrompt?.ToLower())
							{
							case "folder":
							break;
							case "osz":
							break;
							default:
							return;
							}

							fileStream.Dispose();
							}

							GC.Collect();
							Console.WriteLine("File unloaded.");
							// ContinueOnPress(); filePath = RequestPath(); //repeat process
							}


		private static byte[] DecryptFile(Stream fileStream, fEnum.fDecryptMode mode)
		{
			byte[] fileBuffer = ConvertToByteArray(fileStream);
			Console.WriteLine("Converted to byte array.\n");
			
			var algorithmProvider = new SafeEncryptionProvider();
			
			Console.WriteLine("Initialising encryption provider.");
			algorithmProvider.Init(SafeEncryptionProvider.ConvertByteArrayToUIntArray(keyRaw), fEnum.EncryptionMethod.Two);
			Console.WriteLine("Initialised encryption provider.\n");
			
			
			Console.WriteLine("Decrypting file.");
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