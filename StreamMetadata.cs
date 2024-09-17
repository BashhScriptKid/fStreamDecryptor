using System.Diagnostics.CodeAnalysis;

namespace StreamFormatDecryptor{
    public class fMetaDdata{

        public string? ArtistName {set;get;}
        public string? BeatmapSetsID {set;get;}
        public string? Mapper {set;get;}
        public string? SongTitle {set;get;}

        public Dictionary<fEnum.MapMetaType, string> MetaRead;

        public string[]? Fetcher(FileStream fsData){
            using (var br = new BinaryReader(fsData)) {
                // read data and perform integrity checks
                /*
                byte[] magic = br.ReadBytes(3);
                if (magic.Length < 3 || magic[0] != 0xec || magic[1] != 'H' || magic[2] != 'O')
                    throw new IOException("Invalid file.");
                */ //I dont think this is even necessary lol

                // version
                int version = fsData.ReadByte();

                // xor'd iv - 'decoded' once file data is read
                fsData.Read(new byte[16], 0, 16);

                // read hashes
                byte[] hash1 = new byte[16];
                byte[] hash2 = new byte[16];
                fsData.Read(hash1, 0, 16);
                fsData.Read(hash2, 0, 16);
                // read metadata
                int count = br.ReadInt32();

                MetaRead = new Dictionary<fEnum.MapMetaType, string>();

                for (int i = 0; i < count; i++) {
                    short key = br.ReadInt16();
                    string value = br.ReadString();
                    if (Enum.IsDefined(typeof(fEnum.MapMetaType), (int)key))
                        MetaRead.Add ((fEnum.MapMetaType)key, value);
                    // istg if the if-else nestfuck does the job better than switches

                    /*
                    switch (MetaRead.Keys){
                        case MetaRead.ContainsKey(fEnum.MapMetaType.Title):
                            SongTitle = MetaRead.TryGetValue(fEnum.MapMetaType, out string Title);
                            break;

                        case MetaRead.ContainsKey(fEnum.MapMetaType.Artist):
                            ArtistName = MetaRead.TryGetValue (key, out string Artist);
                            break;

                        case MetaRead.ContainsKey(fEnum.MapMetaType.Creator):
                            Mapper = MetaRead.TryGetValue (key, out string Creator);
                            break;

                        case MetaRead.ContainsKey(fEnum.MapMetaType.BeatmapSetID):
                            BeatmapSetID = MetaRead.TryGetValue (key, out string BeatmapSetID);
                            break;

                        default:
                            break;
                    */

                    if (MetaRead.TryGetValue(fEnum.MapMetaType.Title, out string? Title))
                        SongTitle = Title;
                    else
                    {
                        Console.WriteLine("Title not found!");  
                        return null;
                    }

                    if (MetaRead.TryGetValue(fEnum.MapMetaType.Artist, out string? Artist))
                        ArtistName = Artist;
                    else
                    {
                        Console.WriteLine("Artist not found!");
                        return null;
                    }

                    if (MetaRead.TryGetValue(fEnum.MapMetaType.Creator, out string? Creator))
                        Mapper = Creator;
                    else
                    {
                        Console.WriteLine("Mapper not found!");
                        return null;
                    }

                    if (MetaRead.TryGetValue(fEnum.MapMetaType.BeatmapSetID, out string? BeatmapSetID))
                        BeatmapSetsID = BeatmapSetID;
                    else
                    {
                        Console.WriteLine("BeatmapSetID not found!");
                        return null;
                    }
                }
  
            }

            return new string[4] {SongTitle, ArtistName, Mapper, BeatmapSetsID};
        }
        
        

    }
}