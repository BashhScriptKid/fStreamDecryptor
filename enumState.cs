namespace StreamFormatDecryptor{
    public class fEnum{
        public enum eMapFormat{
			osz2,
			osf2,
			undefined
		}

		public enum fDecryptMode{
			DIST,
			OSUM,
			Unknown
		}
		
		 public enum MapMetaType
    	{
        Title,
        Artist,
        Creator,
        Version,
        Source,
        Tags,
        VideoDataOffset,
        VideoDataLength,
        VideoHash,
        BeatmapSetID,
        Genre,
        Language,
        TitleUnicode,
        ArtistUnicode,
        Unknown = 9999,
        Difficulty,
        PreviewTime,
        ArtistFullName,
        ArtistTwitter,
        SourceUnicode,
        ArtistUrl,
        Revision,
        PackId
    }
		
		
		
		
		
		 // enum is more efficient than string
    }
}