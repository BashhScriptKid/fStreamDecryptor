namespace StreamFormatDecryptor{
    public class fEnum{

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
		
		public enum EncryptionMethod
		{
			One,
			Two,
			Three,
			Four
			
		//**TRUE ENUM**//	
			//One,
			//Two,
			//Homebrew,
			//None
		}
		
		
		 // enum is more efficient than string
    }
}