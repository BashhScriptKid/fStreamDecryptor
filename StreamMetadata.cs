using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace StreamFormatDecryptor
{
    public class fMetadata
    {

        public string? ArtistName { set; get; }
        public string? ArtistNameUnicode { set; get; }
        public string? ArtistFullName { set; get; }
        public string? ArtistTwitter { set; get; }
        public string? ArtistUrl { set; get; }
        public string? BeatmapSetID { set; get; }
        public string? Mapper { set; get; }
        public string? SongTitle { set; get; }
        public string? SongTitleUnicode { set; get; }
        public string? Source { set; get; }
        public string? SourceUnicode { set; get; }
        public string? Tags { set; get; }
        public string? Genre { set; get; }
        public string? Language { set; get; }
        public string? Difficulty { set; get; }
        public string? PreviewTime { set; get; }
        public string? VideoDataOffset { set; get; }
        public string? VideoDataLength { set; get; }
        public string? VideoHash { set; get; }
        public string? Revision { set; get; }
        public string? PackId { set; get; }
        public string? Version { set; get; }
        public string? Unknown { set; get; }

        public string? packId { set; get; }

        public static long offsetPostMetadata = 0;

    public Dictionary<fEnum.MapMetaType, string> MetaRead;

        /// <summary>
        /// Fetch metadata from a file stream from a .osz2/.osf2 file.
        ///
        /// This method reads the header of the file stream, then reads the
        /// number of metadata items, and then reads each metadata item in a for-loop iteration.
        /// </summary>
        /// <param name="stream">File stream containing the .osz/.osk file.</param>
        /// <returns>Metadata as array of strings containing all available metadata fields</returns>
        public string[]? Fetcher(FileStream stream)
        {
            if (stream == null || !stream.CanRead)
                throw new ArgumentException("Invalid or unreadable stream provided");

            try
            {
                stream.Position = 0;

                // Use ONE BinaryReader with bufferSize:1 to disable read-ahead entirely.
                // This ensures BaseStream.Position always reflects exactly what has been
                // logically consumed, so offsetPostMetadata is accurate.
                using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);

                ReadHeaderFromReader(reader, false);

                // Read metadata count as plain Int32 (matches reference MapPackage.cs)
                var metadataCount = reader.ReadInt32();
                if (metadataCount < 0 || metadataCount > 1000)
                    throw new InvalidDataException($"Invalid metadata count: {metadataCount}");

                Console.WriteLine($"[Fetcher] metadataCount: {metadataCount}");

                MetaRead = ReadMetadata(reader, metadataCount);
                ExtractMetadataValues();

                // BinaryReader buffers ahead internally — BaseStream.Position is ahead of
                // what the reader logically consumed. Force the stream back by doing a
                // zero-byte seek relative to current position using the reader's own buffer
                // drain: calling PeekChar() forces a refill from exactly where we are,
                // but the simplest fix is to re-derive the position from what we know we read.
                // Header: 68 bytes. Count int: 4 bytes. Metadata: sum of (Int16 + ReadString).
                // Rather than recomputing, seek stream to position reader logically consumed by
                // saving position before/after each read in ReadMetadata instead.
                // For now: compute manually.
                long metadataBlockSize = 4; // the metadataCount Int32
                foreach (var kvp in MetaRead)
                {
                    metadataBlockSize += 2; // Int16 key
                    var strBytes = System.Text.Encoding.UTF8.GetByteCount(kvp.Value);
                    // BinaryReader.ReadString prefix: 7-bit encoded length
                    int strLen = strBytes;
                    do { metadataBlockSize++; strLen >>= 7; } while (strLen > 0);
                    metadataBlockSize += strBytes;
                }
                offsetPostMetadata = 68 + metadataBlockSize;
                Console.WriteLine($"[Fetcher] offsetPostMetadata: {offsetPostMetadata} (computed manually)");

                return new[] { 
                    SongTitle ?? "",
                    SongTitleUnicode ?? "",
                    ArtistName ?? "", 
                    ArtistNameUnicode ?? "",
                    ArtistFullName ?? "",
                    ArtistUrl ?? "",
                    ArtistTwitter ?? "",
                    Mapper ?? "", 
                    Version ?? "",
                    BeatmapSetID ?? "",
                    Source ?? "",
                    Tags ?? "",
                    VideoDataOffset ?? "",
                    VideoDataLength ?? "",
                    VideoHash ?? "",
                    Genre ?? "",
                    Language ?? "",
                    Unknown ?? "",
                    PackId ?? "",
                    Revision ?? ""
                };
            }
            catch (EndOfStreamException)
            {
                throw new InvalidDataException("Unexpected end of file while reading metadata");
            }
            catch (Exception ex) when (ex is not InvalidDataException)
            {
                throw new InvalidDataException($"Error reading metadata: {ex.Message}", ex);
            }
        }

        public byte[][] ReadHeader(FileStream stream, bool verbose)
        {
            stream.Position = 0;
            using var br = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
            return ReadHeaderFromReader(br, verbose);
        }

        public byte[][] ReadHeaderFromReader(BinaryReader reader, bool verbose)
        {
            const int HeaderSize = 68; // 3 (magic) + 1 (version) + 16 (iv) + 16 (hashMeta) + 16 (hashInfo) + 16 (hashBody)

            if (reader.BaseStream.Length < HeaderSize)
                throw new InvalidDataException($"File is too small to contain a valid header. File size: {reader.BaseStream.Length} bytes, required: {HeaderSize} bytes.");

            byte[] header = reader.ReadBytes(HeaderSize);

            if (verbose)
            {
                Console.WriteLine($"Stream length: {reader.BaseStream.Length}");
                Console.WriteLine($"Bytes read: {header.Length}");
                Console.WriteLine($"Stream position after read: {reader.BaseStream.Position}");

                if (header.Length != HeaderSize)
                    throw new InvalidDataException($"Invalid header size. Expected {HeaderSize} bytes, but read {header.Length} bytes.");

                if (header[0] != 0xEC || header[1] != 0x48 || header[2] != 0x4F)
                    throw new InvalidDataException("Invalid magic number in file header");

                Console.WriteLine("Valid .osz2/.osf2 header found");

                var version = header[3];
                Console.WriteLine($"Version byte: {version}");
                if (version > 1)
                    throw new InvalidDataException($"Unsupported version: {version}");
            }

            byte[] iv = new byte[16];
            byte[] hashMeta = new byte[16];
            byte[] hashInfo = new byte[16];
            byte[] hashBody = new byte[16];

            Buffer.BlockCopy(header, 4,  iv,       0, 16);
            Buffer.BlockCopy(header, 20, hashMeta, 0, 16);
            Buffer.BlockCopy(header, 36, hashInfo, 0, 16);
            Buffer.BlockCopy(header, 52, hashBody, 0, 16);

            if (verbose)
            {
                Console.WriteLine($"IV: {BitConverter.ToString(iv)}");
                Console.WriteLine($"Hash Meta: {BitConverter.ToString(hashMeta)}");
                Console.WriteLine($"Hash Info: {BitConverter.ToString(hashInfo)}");
                Console.WriteLine($"Hash Body: {BitConverter.ToString(hashBody)}");
            }

            return new[] { iv, hashMeta, hashInfo, hashBody };
        }

        private static int Read7BitEncodedInt(BinaryReader reader)
        {
            int count = 0;
            int shift = 0;
            byte b;
            do
            {
                if (shift == 35)
                    throw new InvalidDataException("Invalid 7-bit encoded int");
                b = reader.ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            }
            while ((b & 0x80) != 0);
            return count;
        }

        private static Dictionary<fEnum.MapMetaType, string> ReadMetadata(BinaryReader reader, int metadataCount)
        {
            var metaRead = new Dictionary<fEnum.MapMetaType, string>(metadataCount);

            int count = 0;
            
            for (var i = 0; i < metadataCount; i++)
            {
                var key = (fEnum.MapMetaType)reader.ReadInt16();
                var value = reader.ReadString();
               // count = Read7BitEncodedInt(reader); //this breaks the metadata fetcher?

                metaRead[key] = value;
            }
            //Console.WriteLine($"Found {count} metadata entries");
            
            return metaRead;
        }


        private void ExtractMetadataValues()
        {
            // Extract all metadata values
            if (MetaRead.TryGetValue(fEnum.MapMetaType.Title, out var title))
                SongTitle = title;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.TitleUnicode, out var titleUnicode))
                SongTitleUnicode = titleUnicode;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.Artist, out var artist))
                ArtistName = artist;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.ArtistUnicode, out var artistUnicode))
                ArtistNameUnicode = artistUnicode;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.ArtistFullName, out var artistFullName))
                ArtistFullName = artistFullName;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.ArtistTwitter, out var artistTwitter))
                ArtistTwitter = artistTwitter;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.ArtistUrl, out var artistUrl))
                ArtistUrl = artistUrl;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.Creator, out var creator))
                Mapper = creator;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.BeatmapSetID, out var beatmapSetId))
                BeatmapSetID = beatmapSetId;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.Source, out var source))
                Source = source;
            
            if (MetaRead.TryGetValue(fEnum.MapMetaType.Version, out var version))
                Version = version;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.SourceUnicode, out var sourceUnicode))
                SourceUnicode = sourceUnicode;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.Tags, out var tags))
                Tags = tags;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.Genre, out var genre))
                Genre = genre;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.Language, out var language))
                Language = language;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.Difficulty, out var difficulty))
                Difficulty = difficulty;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.PreviewTime, out var previewTime))
                PreviewTime = previewTime;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.VideoDataOffset, out var videoOffset))
                VideoDataOffset = videoOffset;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.VideoDataLength, out var videoLength))
                VideoDataLength = videoLength;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.VideoHash, out var videoHash))
                VideoHash = videoHash;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.Revision, out var revision))
                Revision = revision;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.PackId, out var packId))
                PackId = packId;
        }
    }
}