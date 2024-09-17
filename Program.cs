using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace StreamFormatDecryptor
{
	public class Program
	{
		public static void ContinueOnPress(){
			Console.ReadLine();
		}

		public static void Main(string[] args)
		{


			if (Environment.IsPrivilegedProcess){
				Console.WriteLine("This program is being run as administrator. This is not recommended under normal circumstances");
				Console.WriteLine("If it needs to, under specific reasons; note that file dragging won't work. Alternatively, right click on the file while holding SHIFT and click \"Copy as Path\".");
				Console.WriteLine();
			}


			Console.Write("Insert osu!stream beatmap file (osz2/osf2) to decrypt: ");
			string? filePath = Console.ReadLine();
			if (filePath.Contains("\""))
				filePath = filePath.Replace("\"", ""); // It hates quotes
			var mapFormat = fEnum.eMapFormat.undefined;

			

			if (!File.Exists(filePath))
			{
				if (filePath == null)
					Console.WriteLine("Are you even putting anything there? It's as simple as dragging it to the console.");
					
				Console.WriteLine("The file does not exist. Check your file path and try again...unless it's not even a file path to begin with.");
				ContinueOnPress();
				throw new IOException(filePath + " does not exist.");
				
			}

			if (Path.GetExtension(filePath) == ".osz2")
			{
				mapFormat = fEnum.eMapFormat.osz2;
			}
			else if (Path.GetExtension(filePath) == ".osf2")
			{
				mapFormat = fEnum.eMapFormat.osf2;
			}
			else
			{
				Console.WriteLine("The provided file format is invalid! Check your filename before dragging (we check format by filename extension)");
				ContinueOnPress();
				throw new IOException("Object points to invalid file format	.");
			}

				Console.WriteLine($"File name: {Path.GetFileName(filePath)}");
				Console.WriteLine($"File format: {mapFormat} \n");
				Thread.Sleep(500);

				// Allocate file to memory temporarily
				Console.WriteLine("Loading file...");

				
				var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
				var fileData = new byte[fs.Length];
				fs.Read(fileData, 0, fileData.Length);
				Console.SetCursorPosition(0, (Console.GetCursorPosition().Top - 1));
				Console.WriteLine($"File loaded!({fileData.Length} bytes) \n"); 
				Thread.Sleep(500);

				// Display all metadata
				fMetaDdata info = new();
				info.Fetcher(fs);

				string[] fileMeta = info.Fetcher(fs);

				if (fileMeta == null){
					Console.WriteLine("One or more metadatas are missing! \n");
					ContinueOnPress();
					return;
				}

				Console.WriteLine($"File metadata: \n Title: {fileMeta[0]} \n Artist: {fileMeta[1]} \n Mapper: {fileMeta[2]} \n Beatmap ID: {fileMeta[3]} \n");

				Hasher fileHash = new();

				string key =fileHash.AESDecryptKey(fileMeta[1], fileMeta[3], fileMeta[2], fileMeta[0], mapFormat); //todo: check if my assistant actually arranged the array correcctly
				Console.WriteLine($"Hash generated: {key} \n");










				//Unload at end just in case
				fileData = null;
				GC.Collect();
				fs.Dispose();
				Console.WriteLine("File unloaded.");
				ContinueOnPress() ;
		}



		private static void DecryptFile(string filePath, fEnum.fDecryptMode mode)
		{
			return;
			// todo implement decryption logic
		}
	}
}