using System.Diagnostics.CodeAnalysis;

namespace StreamFormatDecryptor{
    public class fMetadata{

        public string? ArtistName {set;get;}
        public string? BeatmapSetID {set;get;}
        public string? Mapper {set;get;}
        public string? SongTitle {set;get;}

        public Dictionary<fEnum.MapMetaType, string> MetaRead;

        /// <summary>
        /// Fetch metadata from a file stream from a .osz/.osk file.
        ///
        /// This method reads the header of the file stream, then reads the
        /// number of metadata items, and then reads each metadata item in a for-loop iteration.
        /// </summary>
        /// <param name="stream">File stream containing the .osz/.osk file.</param>
        /// <returns>Metadata as a tuple of strings: (SongTitle, ArtistName, Mapper, BeatmapSetID)</returns>
        public string []? Fetcher(FileStream stream)
        {

            /// <summary>
            /// Binary reader to read from.
            /// </summary>
            using var reader = new BinaryReader(stream);

            ReadHeader(stream);

            // 2. Read the number of metadata items.
            // This is a single byte containing the number of
            // metadata items in the file.
            var metadataCount = ReadMetadataCount(reader);

            // 3. Read the metadata items.
            // This reads a number of metadata items, each of
            // which is a short integer (the type of the metadata),
            // followed by a string (the value of the metadata).
            MetaRead = ReadMetadata(reader, metadataCount);

            // 4. Extract the metadata values.
            // This takes the metadata items and extracts the
            // values for the SongTitle, ArtistName, Mapper, and
            // BeatmapSetID fields.
            ExtractMetadataValues();

            // 5. Return the metadata values.
            // This returns the SongTitle, ArtistName, Mapper, and
            // BeatmapSetID fields as a tuple of strings.
            return [SongTitle, ArtistName, Mapper, BeatmapSetID];
        }

        /// <summary>
        /// Reads the header of the given stream.
        /// </summary>
        /// <param name="stream">The stream to read the header from.</param>
        private static void ReadHeader(FileStream stream)
        {
            // Read header
            var version = stream.ReadByte();
            // Read the 16 byte header identifier
            var headerIdentifier = new byte[16];
            stream.Read(headerIdentifier, 0, 16);
            // Read the first 16 byte hash
            var hash1 = new byte[16];
            stream.Read(hash1, 0, 16);
            // Read the second 16 byte hash
            var hash2 = new byte[16];
            stream.Read(hash2, 0, 16);
            // These hashes are used to verify the integrity of the file
            // The first hash is the MD5 hash of the file data up to the
            // video data offset (if the file contains video data), or
            // up to the end of the file if there is no video data
            // The second hash is the MD5 hash of the video data, if the
            // file contains video data
        }

        private static int ReadMetadataCount(BinaryReader reader)
        {
            // Read metadata count
            return reader.ReadInt32();
        }

        /// <summary>
        /// Reads metadata from the given binary reader.
        /// </summary>
        /// <param name="reader">Binary reader to read from.</param>
        /// <param name="metadataCount">Number of metadata items to read.</param>
        /// <returns>A dictionary of metadata items, where the key is the metadata type.</returns>
        private static Dictionary<fEnum.MapMetaType, string> ReadMetadata(BinaryReader reader, int metadataCount)
        {
            // Create an empty dictionary to store the metadata items in.
            var metaRead = new Dictionary<fEnum.MapMetaType, string>();

            // Loop through each metadata item.
            for (var i = 0; i < metadataCount; i++)
            {
                // Read the metadata type (a short integer) and the value (a string).
                var key = reader.ReadInt16();
                var value = reader.ReadString();

                // Check if the metadata type is valid.
                if (Enum.IsDefined(typeof(fEnum.MapMetaType), key))
                {
                    // Add the metadata item to the dictionary.
                    metaRead.Add((fEnum.MapMetaType)key, value);
                }
            }

            // Return the dictionary of metadata items.
            return metaRead;
        }

        private void ExtractMetadataValues()
        {
            // Extract metadata values
            if (MetaRead.TryGetValue(fEnum.MapMetaType.Title, out var songTitle))
                SongTitle = songTitle;
            else
                return;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.Artist, out var artistName))
                ArtistName = artistName;
            else
                return;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.Creator, out var mapper))
                Mapper = mapper;
            else
                return;

            if (MetaRead.TryGetValue(fEnum.MapMetaType.BeatmapSetID, out var beatmapSetId))
                BeatmapSetID = beatmapSetId;
            else
                return;
        }
        
    
        

    }
}