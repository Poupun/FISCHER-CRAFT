using System;

namespace WorldGeneration.Chunks
{
    [Serializable]
    public class ChangedCell
    {
        public int x;
        public int y;
        public int z;
        public int t; // BlockType as int
    }

    [Serializable]
    public class ChunkSaveData
    {
        public int cx;
        public int cz;
        public int sizeX;
        public int sizeY;
        public int sizeZ;
        public ChangedCell[] changes;
    }
}
